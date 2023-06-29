using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using umi3d.edk;

public class ComponentConverter : JsonConverter
{
    SaveReference references;

    public ComponentConverter(SaveReference references)
    {
        this.references = references;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType.IsClass && !objectType.IsArray;
    }

    bool IsRefProperty(Type type)
    {
        return (typeof(Component).IsAssignableFrom(type) || typeof(GameObject).IsAssignableFrom(type));
    }


    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);

        if (IsRefProperty(objectType))
        {
            if (obj["_Type"] != null)
            {
                if (obj["_Type"].ToString() == "_")
                {
                    return null;
                }
                if (obj["Id"] != null)
                {
                    var res = references.GetEntitySync(obj["Id"].ToObject<long>());
                    return res;
                }
            }
        }
        else
        {
            return obj.ToObject(objectType);
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

            foreach (var prop in type.GetFields())
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
                        Debug.LogError(e);
                        jObject[prop?.Name ?? "ERROR"] = "Error 1 " + e.Message;
                    }
                }
                else
                {
                    try
                    {
                        var v = prop.GetValue(value);

                        if ((v?.GetType() ?? prop.FieldType).IsArray && IsRefProperty(v.GetType().GetElementType()))
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
                            else
                                jObject.Add(prop.Name, null);
                        }
                        else
                        {
                            if (v != null)
                                jObject.Add(prop.Name, JToken.FromObject(v, serializer));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
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
        }
        jObject.WriteTo(writer);
    }
}