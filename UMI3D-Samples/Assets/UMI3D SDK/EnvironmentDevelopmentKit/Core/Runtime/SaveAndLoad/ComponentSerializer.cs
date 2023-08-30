using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using umi3d.edk;
using System.Reflection;
using umi3d.edk.save;
using UnityEngine.Events;
using System.Linq;
using UnityEditor.Events;

public class ComponentConverter : JsonConverter
{
    SaveReference references;

    public ComponentConverter(SaveReference references)
    {
        this.references = references;
    }

    public override bool CanConvert(Type objectType)
    {
        return 
            objectType.IsClass 
            && !objectType.IsArray 
            && objectType != typeof(string) 
            && !(objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(List<>))
            && objectType != typeof(AnimationCurve);
    }

    bool IsRefProperty(Type type)
    {
        return (typeof(ScriptableObject).IsAssignableFrom(type) || typeof(Component).IsAssignableFrom(type) || typeof(GameObject).IsAssignableFrom(type));
    }

    bool IsEventProperty(Type type)
    {
        return (typeof(UnityEventBase).IsAssignableFrom(type));
    }


    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        //Debug.Log(objectType + " " + reader.TokenType);
        JObject jobj = JObject.Load(reader);

        if (IsRefProperty(objectType))
        {
            if (jobj["_Type"] != null)
            {
                if (jobj["_Type"].ToString() == "_")
                {
                    return null;
                }
                if (jobj["Id"] != null)
                {
                    var id = jobj["Id"].ToObject<long>();
                    if (id != -1)
                    {
                        var res = references.GetEntitySync(id);
                        Debug.Assert(res != null, $"no entity[{objectType}] for id {id} in {references.Count} {references.debug}");

                        return res;
                    }
                    return null;
                }
            }
        }
        else if (IsEventProperty(objectType))
        {
            if (jobj["Callbacks"] != null)
            {
                List<JObject> lst = jobj["Callbacks"].ToObject<List<JObject>>();

                object obj = Activator.CreateInstance(objectType);

                if (obj is UnityEventBase evnt)
                {
                    foreach (JObject jobjMethod in lst)
                    {
                        if (jobjMethod["_Type"] != null)
                        {
                            if (jobjMethod["_Type"].ToString() == "_")
                            {
                                continue;
                            }

                            if (jobjMethod["Id"] != null)
                            {
                                var id = jobjMethod["Id"].ToObject<long>();
                                if (id != -1)
                                {
                                    var res = references.GetEntitySync(id);
                                    Debug.Assert(res != null, $"no entity[{objectType}] for id {id} in {references.Count} {references.debug}");

                                    if (res != null)
                                    {
                                        string methodName = jobjMethod["Method"].ToString();

                                        var methodToLink = res.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(m => m.Name == methodName);

                                        var methods = obj.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                        var addPersistentListenerMethod = methods.FirstOrDefault(m => m.Name == "AddPersistentListener" && m.GetParameters().Count() == 0);
                                        var setPersistentListenerStateMethod = methods.FirstOrDefault(m => m.Name == "SetPersistentListenerState" && m.GetParameters().Count() == 2);

                                        int index = evnt.GetPersistentEventCount();

                                        addPersistentListenerMethod.Invoke(obj, new object[] { });
                                        if (methodToLink.GetParameters().Count() == 0)
                                        {
                                            var registerVoidPersistentListenerMethod = methods.FirstOrDefault(m => m.Name == "RegisterVoidPersistentListener" && m.GetParameters().Count() == 2);
                                            UnityAction action = Delegate.CreateDelegate(typeof(UnityAction), res, methodName) as UnityAction;

                                            registerVoidPersistentListenerMethod.Invoke(obj, new object[] { index, action });
                                        }
                                        else
                                        {
                                            var registerPersistentListenerMethod = methods.FirstOrDefault(m => m.Name == "RegisterPersistentListener" && m.GetParameters().Count() == 3);

                                            registerPersistentListenerMethod.Invoke(obj, new object[] { index, res, methodToLink });
                                        }
                                        setPersistentListenerStateMethod.Invoke(obj, new object[] { index, UnityEventCallState.RuntimeOnly });
                                    }
                                }
                            }
                        }
                    }
                }

                return obj;
            }
        }
        else
        {
            try
            {
                if (objectType.ToString().Contains("UnityEngine.Font"))
                {
                    var res = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    return res;
                }
                if (objectType.ToString().Contains("UnityEngine") && !objectType.ToString().Contains("UnityEngine.UI.FontData")
                    && !objectType.ToString().Contains("UnityEngine.Events.UnityEvent")
                    && !objectType.ToString().Contains("UnityEngine.AnimationCurve")) 
                {
                    Debug.Log(objectType.ToString());
                    return null; // jobj.ToObject(objectType);
                }

                object obj = Activator.CreateInstance(objectType);

                if (jobj.ToString() == "{}")
                    return obj;

                JsonConvert.PopulateObject(jobj.ToString(), obj, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    Converters = new[] { (JsonConverter)this, new VectorConverter(), new CurveConverter() },
                    ContractResolver = AllPropertiesContractResolver.Singleton
                });

                return obj;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                Debug.LogError(objectType + " " + reader.TokenType+"\n"+jobj.ToString());

                return null;
            }
        }

        return null;
    }

    JObject FromValue(object v, Type type)
    {
        JObject obj = new JObject();
        obj["_Type"] = v?.GetType().ToString() ?? type?.ToString() ?? "_";
        obj["Id"] = v != null ? references.GetId(v) : -1;
        return obj;
    }

    JObject FromValue(object v, Type type, string methodName)
    {
        JObject obj = new JObject();
        obj["_Type"] = v?.GetType().ToString() ?? type?.ToString() ?? "_";
        obj["Id"] = v != null ? references.GetId(v) : -1;
        obj["Method"] = methodName;
        return obj;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        JObject jObject = new JObject();
        if (value != null)
        {
            Type type = value.GetType();

            List<string> fields = new List<string>();
            
            do
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (fields.Contains(field.Name) || field.FieldType.ToString().Contains("UMI3DAsyncProperty") ||
                        field.FieldType.ToString().Contains("UMI3DAsyncListProperty") ||
                        field.FieldType.ToString().Contains("TweenRunner"))
                        continue;

                    fields.Add(field.Name);

                    if (field != null && IsRefProperty(field.FieldType))
                    {
                        try
                        {
                            var v = field.GetValue(value);
                            var obj = FromValue(v, field.FieldType);
                            jObject[field.Name] = obj;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(type.ToString() + " - " + field.Name + " - " + e);
                            jObject[field.Name ?? "ERROR"] = "Error 1 " + e.Message;
                        }
                    }
                    else if (field != null && IsEventProperty(field.FieldType))
                    {
                        var v = field.GetValue(value);

                        if (v is UnityEventBase evnt)
                        {
                            int n = evnt.GetPersistentEventCount();

                            List<JObject> objs = new();
                            for (int i = 0; i < n; i++)
                            {
                                string methodName = evnt.GetPersistentMethodName(i);
                                object target = evnt.GetPersistentTarget(i);

                                objs.Add(FromValue(target, target.GetType(), methodName));
                            }

                            if (objs.Count > 0)
                            {
                                JObject obj = new JObject();
                                obj["Callbacks"] = JToken.FromObject(objs);

                                jObject[field.Name] = obj;
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            var v = field.GetValue(value);

                            if (v == null)
                            {
                                //jObject.Add(prop.Name, null);
                            }
                            else if ((v.GetType() ?? field.FieldType).IsArray && IsRefProperty(v.GetType().GetElementType()))
                            {
                                if (v is Array arr)
                                {
                                    List<JObject> objs = new();

                                    var t = v.GetType().GetElementType();
                                    foreach (var customClass in arr)
                                    {
                                        var obj = FromValue(customClass, t);
                                        objs.Add(obj);
                                    }
                                    jObject.Add(field.Name, JToken.FromObject(objs.ToArray()));
                                }
                                //else
                                //   jObject.Add(prop.Name, null);
                            }
                            else if ((v.GetType() ?? field.FieldType).IsGenericType &&
                                ((v.GetType() ?? field.FieldType).GetGenericTypeDefinition() == typeof(Dictionary<,>)
                                || (v.GetType() ?? field.FieldType).GetGenericTypeDefinition() == typeof(HashSet<>)
                                ))
                            {
                                // Nothing
                            }
                            else if (typeof(IEnumerable).IsAssignableFrom(v.GetType() ?? field.FieldType) && (v.GetType() ?? field.FieldType) != typeof(string))
                            {
                                if (v is IEnumerable enumerable)
                                {
                                    IEnumerator enumerator = enumerable.GetEnumerator();
                                    List<JObject> objs = new();

                                    bool atLeastOneValue = enumerator.MoveNext();

                                    if (!atLeastOneValue)
                                        jObject.Add(field.Name, JToken.FromObject(objs.ToArray()));
                                    else if (!IsRefProperty(enumerator.Current.GetType()))
                                        jObject.Add(field.Name, JToken.FromObject(v, serializer));
                                    else
                                    {
                                        do
                                        {
                                            object item = enumerator.Current;

                                            var obj = FromValue(item, item?.GetType());
                                            objs.Add(obj);
                                        } while (enumerator.MoveNext());

                                        jObject.Add(field.Name, JToken.FromObject(objs));
                                    }
                                }
                                //else
                                //    jObject.Add(prop.Name, null);
                            }
                            else
                            {
                                jObject.Add(field.Name, JToken.FromObject(v, serializer));
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(type.ToString() + " - " + field.Name + " - " + e);
                            jObject[field?.Name ?? "ERROR"] = "Error 2 " + e.Message;
                        }
                    }
                }

                type = type.BaseType;
            } while (type != null && type != typeof(MonoBehaviour) && type != typeof(Behaviour) && type != typeof(System.Object) &&
                        type != typeof(UnityEngine.Object) && type != typeof(UnityEngine.Component));
            
            jObject.WriteTo(writer);
        }
    }
}

public class VectorConverter : JsonConverter
{


    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector2)
            || objectType == typeof(Vector3)
            || objectType == typeof(Vector4)
            || objectType == typeof(Quaternion)
            || objectType == typeof(Vector2Int)
            || objectType == typeof(Vector3Int)
            || objectType == typeof(Color)
            || objectType == typeof(Color32)
            ;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);
        if(objectType == typeof(Vector2))
                return new Vector2(
                    obj.ContainsKey("x") ? (float)obj["x"] : 0,
                    obj.ContainsKey("y") ? (float)obj["y"] : 0
                    );
        if (objectType == typeof(Vector3))
            return new Vector3(
                obj.ContainsKey("x") ? (float)obj["x"] : 0,
                obj.ContainsKey("y") ? (float)obj["y"] : 0,
                obj.ContainsKey("z") ? (float)obj["z"] : 0
                );
        if (objectType == typeof(Vector4))
            return new Vector4(
                obj.ContainsKey("x") ? (float)obj["x"] : 0,
                obj.ContainsKey("y") ? (float)obj["y"] : 0,
                obj.ContainsKey("z") ? (float)obj["z"] : 0,
                obj.ContainsKey("w") ? (float)obj["w"] : 0
                );
        if (objectType == typeof(Quaternion))
            return new Quaternion(
                obj.ContainsKey("x") ? (float)obj["x"] : 0,
                obj.ContainsKey("y") ? (float)obj["y"] : 0,
                obj.ContainsKey("z") ? (float)obj["z"] : 0,
                obj.ContainsKey("w") ? (float)obj["w"] : 0
                );
        if (objectType == typeof(Vector2Int))
            return new Vector2Int(
                obj.ContainsKey("x") ? (int)obj["x"] : 0,
                obj.ContainsKey("y") ? (int)obj["y"] : 0
                );
        if (objectType == typeof(Vector3Int))
            return new Vector3Int(
                obj.ContainsKey("x") ? (int)obj["x"] : 0,
                obj.ContainsKey("y") ? (int)obj["y"] : 0,
                obj.ContainsKey("z") ? (int)obj["z"] : 0
                );
        if (objectType == typeof(Color))
            return new Color(
                obj.ContainsKey("r") ? (int)obj["r"] : 0,
                obj.ContainsKey("g") ? (int)obj["g"] : 0,
                obj.ContainsKey("b") ? (int)obj["b"] : 0,
                obj.ContainsKey("a") ? (int)obj["a"] : 0
                );
        if (objectType == typeof(Color32))
            return new Color32(
                obj.ContainsKey("r") ? (byte)obj["r"] : byte.MinValue,
                obj.ContainsKey("g") ? (byte)obj["g"] : byte.MinValue,
                obj.ContainsKey("b") ? (byte)obj["b"] : byte.MinValue,
                obj.ContainsKey("a") ? (byte)obj["a"] : byte.MinValue
                );
        return null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        JObject jObject = new JObject();
        switch (value)
        {
            case Vector2 v:
                jObject["x"] = v.x;
                jObject["y"] = v.y;
                break;
            case Vector3 v:
                jObject["x"] = v.x;
                jObject["y"] = v.y;
                jObject["z"] = v.z;
                break;
            case Vector4 v:
                jObject["x"] = v.x;
                jObject["y"] = v.y;
                jObject["Z"] = v.z;
                jObject["w"] = v.w;
                break;
            case Quaternion q:
                jObject["x"] = q.x;
                jObject["y"] = q.y;
                jObject["Z"] = q.z;
                jObject["w"] = q.w;
                break;
            case Vector2Int v:
                jObject["x"] = v.x;
                jObject["y"] = v.y;
                break;
            case Vector3Int v:
                jObject["x"] = v.x;
                jObject["y"] = v.y;
                jObject["z"] = v.z;
                break;
            case Color c:
                jObject["r"] = c.r;
                jObject["g"] = c.g;
                jObject["b"] = c.b;
                jObject["a"] = c.a;
                break;
            case Color32 c:
                jObject["r"] = c.r;
                jObject["g"] = c.g;
                jObject["b"] = c.b;
                jObject["a"] = c.a;
                break;
        }
        jObject.WriteTo(writer);
    }
}

public class CurveConverter : JsonConverter
{


    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(AnimationCurve)
            ;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);

        if (objectType == typeof(AnimationCurve))
        {
            AnimationCurve curve = new AnimationCurve(obj.ContainsKey("keys") ? obj["keys"].ToObject<Keyframe[]>() : new Keyframe[] { });
            curve.preWrapMode = Enum.Parse<WrapMode>(obj["preWrapMode"].ToString());
            curve.postWrapMode = Enum.Parse<WrapMode>(obj["postWrapMode"].ToString());

            return curve;
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        JObject jObject = new JObject();

        switch (value)
        {
            case AnimationCurve animationCurve:
                jObject["preWrapMode"] = animationCurve.preWrapMode.ToString();
                jObject["postWrapMode"] = animationCurve.postWrapMode.ToString();
                jObject["keys"] = JToken.FromObject(animationCurve.keys);
                break;
        }

        jObject.WriteTo(writer);
    }
}