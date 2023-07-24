using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using umi3d.edk;
using System.Reflection;
using umi3d.edk.save;

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
            && !(objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(List<>));
    }

    bool IsRefProperty(Type type)
    {
        return (typeof(ScriptableObject).IsAssignableFrom(type) || typeof(Component).IsAssignableFrom(type) || typeof(GameObject).IsAssignableFrom(type));
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
        else
        {
            try
            {
                if (objectType.ToString().Contains("UnityEngine"))
                    return null; // jobj.ToObject(objectType);

                object obj = Activator.CreateInstance(objectType);

                if (jobj.ToString() == "{}")
                    return obj;

                JsonConvert.PopulateObject(jobj.ToString(), obj, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    Converters = new[] { (JsonConverter)this, new VectorConverter() },
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
        obj["_Type"] = v?.GetType().ToString() ?? type.ToString();
        obj["Id"] = v != null ? references.GetId(v) : -1;
        return obj;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        JObject jObject = new JObject();
        if (value != null)
        {
            Type type = value.GetType();

            foreach (var prop in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (prop != null && IsRefProperty(prop.FieldType))
                {
                    try
                    {
                        var v = prop.GetValue(value);
                        var obj = FromValue(v, prop.FieldType);
                        jObject[prop.Name] = obj;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(type.ToString() + " - " + prop.Name + " - " + e);
                        jObject[prop?.Name ?? "ERROR"] = "Error 1 " + e.Message;
                    }
                }
                else
                {
                    try
                    {
                        var v = prop.GetValue(value);

                        if (v == null)
                        {
                            //jObject.Add(prop.Name, null);
                        }
                        else if ((v.GetType() ?? prop.FieldType).IsArray && IsRefProperty(v.GetType().GetElementType()))
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
                                jObject.Add(prop.Name, JToken.FromObject(objs.ToArray()));
                            }
                            //else
                             //   jObject.Add(prop.Name, null);
                        }
                        else if ((v.GetType() ?? prop.FieldType).IsGenericType && 
                            ((v.GetType() ?? prop.FieldType).GetGenericTypeDefinition() == typeof(Dictionary<,>)
                            || (v.GetType() ?? prop.FieldType).GetGenericTypeDefinition() == typeof(HashSet<>)
                            ))
                        {
                            // Nothing
                        }
                        else if (typeof(IEnumerable).IsAssignableFrom(v.GetType() ?? prop.FieldType) && (v.GetType() ?? prop.FieldType) != typeof(string))
                        {
                            if (v is IEnumerable enumerable)
                            {
                                IEnumerator enumerator = enumerable.GetEnumerator();
                                List<JObject> objs = new();

                                bool atLeastOneValue = enumerator.MoveNext();

                                if (!atLeastOneValue)
                                    jObject.Add(prop.Name, JToken.FromObject(objs.ToArray()));
                                else if (!IsRefProperty(enumerator.Current.GetType()))
                                    jObject.Add(prop.Name, JToken.FromObject(v, serializer));
                                else
                                {
                                    do
                                    {
                                        object item = enumerator.Current;

                                        var obj = FromValue(enumerator.Current, enumerator.Current.GetType());
                                        objs.Add(obj);
                                    } while (enumerator.MoveNext());

                                    jObject.Add(prop.Name, JToken.FromObject(objs));
                                }
                            }
                            //else
                            //    jObject.Add(prop.Name, null);
                        }
                        else
                        {
                            jObject.Add(prop.Name, JToken.FromObject(v, serializer));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(type.ToString() + " - " + prop.Name + " - " + e);
                        jObject[prop?.Name ?? "ERROR"] = "Error 2 " + e.Message;
                    }
                }
            }

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