/*
Copyright 2019 - 2023 Inetum

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection;
using System.Linq;
using umi3d.common;
using Newtonsoft.Json;
using inetum.unityUtils;

public interface ISavable
{
    ComponentExtensionSO Save();
    Task<bool> Load(object data);
}

public interface ISavable<Data> : ISavable where Data : class, new()
{
    ComponentExtensionSO ISavable.Save() => new ComponentExtensionSO() { name = this.GetType().FullName, data = Save(new Data()) };
    Task<bool> ISavable.Load(object data) => Load(data as Data);

    Data Save(Data data);
    Task<bool> Load(Data data);
}

public class ComponentExtensionSO
{
    public string name { get; set; }
    public object data { get; set; }
}

public static class ComponentExtensionSOLoader
{

    public static object Load(GameObject gameObject, string json)
    {
        var cp = ComponentExtensionSOLoader.FromJson(json);
        return ComponentExtensionSOLoader.Load(gameObject, cp);
    }

    public static object LoadOrUpdate(GameObject gameObject, ComponentExtensionSO extension)
    {
        var cp = gameObject.GetOrAddComponent(extension.Type());
        (cp as ISavable).Load(extension.data);
        return cp;
    }

    public static object Load(GameObject gameObject, ComponentExtensionSO extension)
    {
        var cp = gameObject.AddComponent(extension.Type());
        (cp as ISavable).Load(extension.data);
        return cp;
    }

    static Type Type(this ComponentExtensionSO extension)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(extension.name))
            .FirstOrDefault(t => t != null);
    }

    public static string ToJson(this ComponentExtensionSO dto, TypeNameHandling typeNameHandling = TypeNameHandling.All)
    {
        return JsonConvert.SerializeObject(dto, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = typeNameHandling
        });
    }

    public static ComponentExtensionSO FromJson(string json)
    {
        return JsonConvert.DeserializeObject<ComponentExtensionSO>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        });
    }
}
