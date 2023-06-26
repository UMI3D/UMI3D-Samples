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
using umi3d.edk;

//public interface ISavable
//{
//    ComponentExtensionSO Save(SaveReference references);
//    Task<bool> Load(object data);
//}

//public interface ISavable<Data> : ISavable where Data : class, new()
//{
//    ComponentExtensionSO ISavable.Save(SaveReference references) => new ComponentExtensionSO() { name = this.GetType().FullName, data = Save(new Data(),references), id = references.GetId(this) };
//    Task<bool> ISavable.Load(object data) => Load(data as Data);

//    Data Save(Data data, SaveReference references);
//    Task<bool> Load(Data data);
//}

public class ComponentExtensionSO
{
    public long id { get; set; }
    public string name { get; set; }
    public object data { get; set; }
    public object customData { get; set; }
}

public static class ComponentExtensionSOLoader
{


}
