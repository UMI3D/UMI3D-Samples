﻿/*
Copyright 2019 - 2021 Inetum
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

using inetum.unityUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using umi3d.edk;
using UnityEngine;

namespace umi3d.edk.save
{
    public class UMI3DSceneLoader : Singleton<UMI3DSceneLoader>
    {
        List<UMI3DSceneLoaderModule> modules;

        public UMI3DSceneLoader():base()
        {
            modules = UMI3DSceneLoaderModuleUtils.GetModules().ToList();
        }

        static public void Restart()
        {
            instance.modules = UMI3DSceneLoaderModuleUtils.GetModules().ToList();
        }

        public static object Save<T>(T obj, SaveReference references)
        {
            foreach(var module in Instance.modules)
            {
                if(module.Save(obj,out object data, references))
                {
                    return data;
                }
            }
            return default;
        }


        public static async Task<bool> Load<T, Data>(T obj, Data data)
        {
            foreach (var module in Instance.modules)
            {
                if (await module.Load(obj, data))
                {
                    return true;
                }
            }
            return false;
        }

        public static object Load(GameObject gameObject, string json)
        {
            var cp = FromJson(json);
            return Load(gameObject, cp);
        }

        public static async Task<object> LoadOrUpdate(GameObject gameObject, ComponentExtensionSO extension)
        {
            var type = extension.Type();
            if (type == null)
                return null;
            var cp = gameObject.GetOrAddComponent(type);
            await Load(cp, extension.data);
            return cp;
        }

        public static async Task<object> Load(GameObject gameObject, ComponentExtensionSO extension)
        {
            var cp = gameObject.AddComponent(extension.Type());
            await Load(cp, extension.data);
            return cp;
        }

        public static ComponentExtensionSO FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ComponentExtensionSO>(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
        }

        public static IEnumerable<ComponentExtensionSO> GetComponents(GameObject gameObject, SaveReference references)
        {
            return gameObject.GetComponents<Component>().Select(s => new ComponentExtensionSO() { name = s.GetType().FullName, data = Save(s, references), id = references.GetId(s) }).Where(s => s!= null);
        }




    }

    [UMI3DSceneLoaderOrder(int.MinValue)]
    public class LastLoader : UMI3DSceneLoaderModule
    {
        Task<bool> UMI3DSceneLoaderModule.Load<T>(T obj, object data)
        {
            if (data != null)
                try
                {
                    JsonUtility.FromJsonOverwrite((string)data, obj);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                }
            return Task.FromResult(true);
        }

        bool UMI3DSceneLoaderModule.Save<T>(T obj, out object data, SaveReference references)
        {
            try
            {
                data = JsonUtility.ToJson(obj);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(obj);
                UnityEngine.Debug.LogError(e);
                data = null;
            }
            return true;
        }

    }


    public static class UMI3DSceneLoaderExtension
    {
        public static IEnumerable<ComponentExtensionSO> GetComponentExtensionSOs(this Transform transform, SaveReference references)
        {
            return UMI3DSceneLoader.GetComponents(transform.gameObject, references);
        }

        public static IEnumerable<ComponentExtensionSO> GetComponentExtensionSOs(this GameObject gameObject, SaveReference references)
        {
            return UMI3DSceneLoader.GetComponents(gameObject, references);
        }

        public static Type Type(this ComponentExtensionSO extension)
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
    }

    [UMI3DSceneLoaderIgnore]
    public interface UMI3DSceneLoaderModule
    {
        bool Save<T>(T obj,out object data, SaveReference references);
        Task<bool> Load<T>(T obj, object data);
    }

    /// <summary>
    /// Helper class to serialize objects.
    /// </summary>
    /// Typically used to serialize objects that are not defined in the UMI3D core.
    public interface UMI3DSceneLoaderModule<T,Data> where Data : class, new()
    {
        Data Save(T obj,Data data, SaveReference references);
        Task<bool> Load(T obj, Data data);
    }

    public static class UMI3DSceneLoaderModuleUtils
    {
        /// <summary>
        /// Return a collection of all UMI3DSerializerModule and UMI3DSerializerModule/<C/>
        /// </summary>
        /// <param name="assembly">Assembly to restrict the cherch. All assembly of the current domain if null</param>
        /// <returns></returns>
        public static IEnumerable<UMI3DSceneLoaderModule> GetModules(Assembly assembly = null)
        {
            return GetModulesType(assembly).SelectMany(Instanciate);
        }

        static IEnumerable<Type> GetModulesType(Assembly assembly = null)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => assembly == null || a == assembly)
                .SelectMany(a => a.GetTypes())
                .Where(type => type.IsValidType() && !type.IsIgnoreAttribute())
                .OrderByDescending(type =>
                {
                    var attributes = type.GetCustomAttributes(typeof(UMI3DSceneLoaderOrderAttribute), true);
                    if (type.GetCustomAttributes(typeof(UMI3DSceneLoaderOrderAttribute), true).Length > 0)
                    {
                        return attributes.Select(a => a as UMI3DSceneLoaderOrderAttribute).First().priotity;
                    }
                    return -1;
                });
        }

        /// <summary>
        /// State if the The type is a serializer.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static bool IsValidType(this Type type)
        {
            return !type.GetTypeInfo().IsAbstract
            && type.GetTypeInfo().IsClass
            && (
                type.GetInterfaces().Contains(typeof(UMI3DSceneLoaderModule))
                || type.GetInterfaces()
                        .Any(i =>
                            i.IsGenericType
                            && i.GetGenericTypeDefinition() == typeof(UMI3DSceneLoaderModule<,>
               )));
        }

        /// <summary>
        /// Look if the Serializer have the ignore attribut.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static bool IsIgnoreAttribute(this Type type)
        {
            var a = type.GetCustomAttributes(typeof(UMI3DSceneLoaderIgnoreAttribute), false);
            if (a.Length > 0)
            {
                return a.Any(a => (a as UMI3DSceneLoaderIgnoreAttribute).ignore);
            }
            return false;
        }

        /// <summary>
        /// Instanciate class if its inherit the UMI3DSerializerModule interface and put it in a container foreach UMI3DSerializerModule/</> interface its inheriting 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<UMI3DSceneLoaderModule> Instanciate(Type type)
        {
            var l = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(UMI3DSceneLoaderModule<,>))
                .Select(i => new UMI3DSceneLoaderContainer(type, i, i.GetGenericArguments().First(), i.GetGenericArguments().Skip(1).First())).Cast<UMI3DSceneLoaderModule>().ToList();

            if (type.GetInterfaces().Contains(typeof(UMI3DSceneLoaderModule)))
                l.Add(Activator.CreateInstance(type) as UMI3DSceneLoaderModule);

            return l;
        }
    }

    [UMI3DSceneLoaderIgnore]
    class UMI3DSceneLoaderContainer : UMI3DSceneLoaderModule
    {
        public Type type;
        public Type dataType;
        public object module;
        MethodInfo methodLoad;
        MethodInfo methodSave;

        public UMI3DSceneLoaderContainer(Type type, Type interfaceType, Type argumentType, Type dataType)
        {
            this.type = argumentType;
            module = Activator.CreateInstance(type);
            methodLoad = GetImplementedMethod(type, interfaceType.GetMethod("Load"));
            methodSave = GetImplementedMethod(type, interfaceType.GetMethod("Save"));
            this.dataType = dataType;
        }

        static MethodInfo GetImplementedMethod(Type targetType, MethodInfo interfaceMethod)
        {
            if (targetType is null) throw new ArgumentNullException(nameof(targetType));
            if (interfaceMethod is null) throw new ArgumentNullException(nameof(interfaceMethod));

            var map = targetType.GetInterfaceMap(interfaceMethod.DeclaringType);
            var index = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
            if (index < 0) return null;

            return map.TargetMethods[index];
        }

        bool UMI3DSceneLoaderModule.Save<T>(T obj,out object data, SaveReference references)
        {
            if (type.IsAssignableFrom(typeof(T)))
            {
                var param = new object[] { obj, null, references };
                if ((bool)methodSave.Invoke(module, param))
                {
                    data = param[1];
                }
            }
            data = default;
            return false;
        }

        Task<bool> UMI3DSceneLoaderModule.Load<T>(T obj, object data)
        {
            if (type.IsAssignableFrom(typeof(T)) && dataType.IsAssignableFrom(data.GetType()))
                return (Task<bool>)methodLoad.Invoke(module, new object[] { obj, data });
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Attribute to specify a test priority.
    /// Without this attribute the priotity is set to -1.
    /// Serializer will be called in reverse order sort by priority.
    /// </summary>
    public class UMI3DSceneLoaderOrderAttribute : Attribute
    {
        public readonly int priotity;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="priotity">Priority of the serializer. 0 by default</param>
        public UMI3DSceneLoaderOrderAttribute(int priotity = 0)
        {
            this.priotity = priotity;
        }
    }

    /// <summary>
    /// Serilizer with this attribute will not be called.
    /// The presence of the attribute is checked on the class itself and not its inheritance.
    /// </summary>
    public class UMI3DSceneLoaderIgnoreAttribute : Attribute
    {
        public readonly bool ignore;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ignore">Should the Serializer should be ignored. True by default.</param>
        public UMI3DSceneLoaderIgnoreAttribute(bool ignore = true)
        {
            this.ignore = ignore;
        }
    }
}