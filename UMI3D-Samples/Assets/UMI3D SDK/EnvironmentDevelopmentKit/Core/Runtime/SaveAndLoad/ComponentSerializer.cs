using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using umi3d.edk;
using Unity.Collections;

public class ComponentConverter : JsonConverter
{
    SaveReference references;

    public ComponentConverter(SaveReference references)
    {
        this.references = references;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Component).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);
        if (obj["_Type"] != null)
        {
            if(obj["_Type"].ToString() == "_")
            {
                return null;
            }
            if (obj["Id"] != null)
            {
                return references.GetEntitySync(obj["Id"].ToObject<long>());
            }
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        JObject obj = new JObject();
        obj["_Type"] = value?.GetType().ToString() ?? "_";
        obj["Id"] = value != null ? references.GetId(value) : -1;
        obj.WriteTo(writer);
    }
}
