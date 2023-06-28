using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using umi3d.edk;
using Unity.Collections;
using Codice.CM.Common;
using System.Reflection;
using Mono.Cecil.Cil;
using System.Linq;

public class ComponentConverter : JsonConverter
{
    SaveReference references;

    public ComponentConverter(SaveReference references)
    {
        this.references = references;
    }

    public override bool CanConvert(Type objectType)
    {
        //Debug.Log($"{objectType} {typeof(Component).IsAssignableFrom(objectType)}");
        return objectType.IsClass;
    }

    bool IsRefProperty(Type type)
    {
        return (typeof(Component).IsAssignableFrom(type) || typeof(GameObject).IsAssignableFrom(type));
    }


    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        Debug.Log($"Read " + objectType);

        if (objectType.IsArray)
        {
            JArray jsonArray = JArray.Load(reader);
            Array array = Array.CreateInstance(objectType.GetElementType(), jsonArray.Count);

            //if (IsRefProperty(objectType.GetElementType()))
            //{
            int i = 0;
            foreach (var item in jsonArray.ToList())
            {
                if (IsRefProperty(objectType.GetElementType()))
                {
                    if (item["_Type"] != null)
                    {
                        if (item["_Type"].ToString() == "_")
                        {
                            //Debug.Log($"type : {prop.Name} {prop.MemberType} | res : NULL");
                            array.SetValue(null, i++);
                        }
                    }
                    if (item["Id"] != null)
                    {
                        var res = references.GetEntitySync(item["Id"].ToObject<long>());
                        //Debug.Log($"type : {prop.Name} {prop.MemberType} | res : {res?.GetType().Name}");

                        array.SetValue(res, i++);
                    }
                }
                else
                    array.SetValue(item.ToObject(objectType.GetElementType(), serializer), i++);
            }
            //}
            //else
            //{
            //    for (int i = 0; i < jsonArray.Count; i++)
            //    {
            //        var val = jsonArray[i];


            //        array.SetValue(jsonArray[i].ToObject(objectType.GetElementType(), serializer), i);
            //    }
            //}

            return array;
        }
        else
        {
            Debug.Log(reader.TokenType);
            JObject obj = JObject.Load(reader);
            Debug.Log($"Read");

            if (IsRefProperty(objectType))
            {
                Debug.Log($"Read ref");
                if (obj["_Type"] != null)
                {
                    if (obj["_Type"].ToString() == "_")
                    {
                        //Debug.Log($"type : {prop.Name} {prop.MemberType} | res : NULL");
                        return null;
                    }
                    if (obj["Id"] != null)
                    {
                        var res = references.GetEntitySync(obj["Id"].ToObject<long>());
                        //Debug.Log($"type : {prop.Name} {prop.MemberType} | res : {res?.GetType().Name}");

                        return res;
                    }
                }

            }
            else
            {
                Debug.Log($"Read not ref");
                return obj.ToObject(objectType);
            }

            //foreach (var prop in objectType.GetFields())
            //{
            //    if (obj.ContainsKey(prop.Name))
            //    {
            //        if (IsRefProperty(prop.FieldType))
            //        {
            //            Debug.Log($"Read ref");
            //            object value = null;

            //            var p = obj[prop.Name];
            //            if (p["_Type"] != null)
            //            {
            //                if (p["_Type"].ToString() == "_")
            //                {
            //                    Debug.Log($"type : {prop.Name} {prop.MemberType} | res : NULL");
            //                    return null;
            //                }
            //                if (obj["Id"] != null)
            //                {
            //                    var res = references.GetEntitySync(p["Id"].ToObject<long>());
            //                    Debug.Log($"type : {prop.Name} {prop.MemberType} | res : {res?.GetType().Name}");

            //                    return res;
            //                }
            //            }

            //            prop.SetValue(existingValue, value);
            //        }
            //        else
            //        {

            //            Debug.Log($"Read not ref");
            //            prop.SetValue(existingValue, obj[prop.Name]);
            //        }
            //    }
            //}
        }

        return null;
    }

    //public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //{
    //    JObject obj = new JObject();
    //    obj["_Type"] = value?.GetType().ToString() ?? "_";
    //    obj["Id"] = value != null ? references.GetId(value) : -1;
    //    obj.WriteTo(writer);
    //}
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        JObject jObject = new JObject();
        Debug.Log($"Writte value " + value.ToString());
        if (value != null)
        {

            Type type = value.GetType();

            foreach (var prop in type.GetFields())
            {

                if (prop != null && IsRefProperty(prop.FieldType))
                {
                    Debug.Log($"Writte property " + prop.Name);
                    try
                    {
                        JObject obj = new JObject();
                        var v = prop.GetValue(value);
                        obj["_Type"] = v?.GetType().ToString() ?? prop.FieldType.ToString();
                        obj["Id"] = v != null ? references.GetId(v) : -1;
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
                    Debug.Log($"Writte Default property " + prop.Name);
                    try
                    {
                        var v = prop.GetValue(value);
                        if (v != null)
                            jObject.Add(prop.Name, JToken.FromObject(v));
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