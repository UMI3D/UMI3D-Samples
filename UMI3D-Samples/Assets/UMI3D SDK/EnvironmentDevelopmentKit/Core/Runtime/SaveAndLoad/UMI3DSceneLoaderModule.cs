/*
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
using Newtonsoft.Json.Serialization;
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
            modules = new() { new LastLoader() }; //UMI3DSceneLoaderModuleUtils.GetModules().ToList();
        }

        static public void Restart()
        {
            instance.modules = new() { new LastLoader() };
        }

        public static object Save<T>(T obj, SaveReference references)
        {
            foreach(var module in Instance.modules)
            {
               
                if(module.Save(obj,out object data, references))
                {
                    //UnityEngine.Debug.Log($"{obj} => {module.ToString()} {(module as UMI3DSceneLoaderContainer)?.type}");
                    return data;
                }
            }
            return default;
        }

        public static async Task<bool> Load<T, Data>(T obj, Data data, SaveReference references)
        {
            foreach (var module in Instance.modules)
            {
                if (await module.Load(obj, data, references))
                {
                    return true;
                }
            }
            return false;
        }

        public static object Load(GameObject gameObject, string json, SaveReference references)
        {
            var cp = FromJson(json);

            return Load(gameObject, cp, references);
        }

        public static async Task<object> LoadOrUpdate(GameObject gameObject, ComponentExtensionSO extension, SaveReference references)
        {
            var type = extension.Type();

            if (type == null)
                return null;

            /*var component = gameObject.GetOrAddComponent(type);
            references.GetId(component, extension.id);*/
            
            Component component = null;

            var components = gameObject.GetComponents(type);
            foreach (var cp in components)
            {
                var id = references.GetId(cp, extension.id);

                if (id == extension.id)
                {
                    component = cp;
                    break;
                }
            }

            if (component == null)
            {
                component = gameObject.AddComponent(type);
                references.GetId(component, extension.id);
            }
            
            while (!references.ready)
                await Task.Yield();

            //Debug.Log("Load "+gameObject.name);

            await Load(component, extension.data, references);

            return component;
        }

        public static async Task<object> LoadOrUpdate(GameObject gameObject, ScriptableExtensionSO extension, SaveReference references)
        {
            var type = extension.Type();

            if (type == null)
                return null;

            ScriptableObject scriptableObject = ScriptableObject.CreateInstance(type);
            scriptableObject.name = extension.name;

            var id = references.GetId(scriptableObject, extension.id);

            while (!references.ready)
                await Task.Yield();

            //Debug.Log("Load Scriptable " + extension.name);

            await Load(scriptableObject, extension.data, references);

            return scriptableObject;
        }

        public static async Task<object> Load(GameObject gameObject, ComponentExtensionSO extension, SaveReference references)
        {
            var cp = gameObject.AddComponent(extension.Type());

            await Load(cp, extension.data, references);

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
            return gameObject.GetComponents<Component>()
                .OrderBy(OrderComponent)
                .Select(s => new ComponentExtensionSO() { type = s.GetType().FullName, data = Save(s, references), id = references.GetId(s) });
        }

        public static IEnumerable<ScriptableExtensionSO> GetScriptables(SaveReference references)
        {
            return references.GetAllEntitiesAssignedFrom<ScriptableObject>()
                .Select(s => new ScriptableExtensionSO() { type = s.GetType().FullName, data = Save((ScriptableObject)s, references), id = references.GetId(s), name = ((ScriptableObject)s).name });
        }

        static int OrderComponent(Component component)
        {
            return OrderComponent(component.GetType());
        }

        static int OrderComponent(Type component)
        {
            var attributes = component.GetCustomAttributes(typeof(RequireComponent), true);
            if (attributes.Length > 0)
            {
                var max = 0;
                foreach(var required in attributes.Select(a => a as RequireComponent))
                {
                    if (required.m_Type0 != null)
                    {
                        var m = OrderComponent(required.m_Type0);
                        if (max < m)
                            max = m;
                    }
                    if (required.m_Type1 != null)
                    {
                        var m = OrderComponent(required.m_Type0);
                        if (max < m)
                            max = m;
                    }
                    if (required.m_Type2 != null)
                    {
                        var m = OrderComponent(required.m_Type0);
                        if (max < m)
                            max = m;
                    }
                }

                return max + 1;
            }
            return 0;
        }

        static void OrderComponent(Type component, List<Type> antiCircularLoop, ref int max)
        {
            var m = OrderComponent(component, antiCircularLoop);
            if(m > max)
                max = m;
        }

        static int OrderComponent(Type component, List<Type> antiCircularLoop)
        {
            if (component == null)
                return 0;

            if (antiCircularLoop.Contains(component))
                return int.MaxValue;

            antiCircularLoop.Add(component);

            var attributes = component.GetCustomAttributes(typeof(RequireComponent), true);
            if (attributes.Length > 0)
            {
                var max = 0;
                foreach (var required in attributes.Select(a => a as RequireComponent))
                {
                    OrderComponent(required.m_Type0, antiCircularLoop.ToList(), ref max);
                    OrderComponent(required.m_Type1, antiCircularLoop.ToList(), ref max);
                    OrderComponent(required.m_Type2, antiCircularLoop.ToList(), ref max);
                }
                return max + 1;
            }
            return 0;
        }



    }

    public class AllPropertiesContractResolver : DefaultContractResolver
    {
        protected static AllPropertiesContractResolver singleton = null;
        public static AllPropertiesContractResolver Singleton { get
            {
                if (singleton == null)
                    singleton = new AllPropertiesContractResolver();
                return singleton;
            } }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Select(p => base.CreateProperty(p, memberSerialization))
                        .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                   .Select(f => base.CreateProperty(f, memberSerialization)))
                        .ToList();

            props.ForEach(p => { p.Writable = true; p.Readable = true; });

            return props;
        }
    }

    // [UMI3DSceneLoaderOrder(int.MinValue)]
    [UMI3DSceneLoaderIgnore]
    public class LastLoader : UMI3DSceneLoaderModule
    {
        Task<bool> UMI3DSceneLoaderModule.Load<T>(T obj, object data, SaveReference references)
        {
            if (data != null && !(obj is Transform))
                try
                {
                    //UnityEngine.Debug.Log(obj +" "+ typeof(T).IsSubclassOf(typeof(Transform)).ToString());
                    var c = new ComponentConverter(references);

                    JsonConvert.PopulateObject((string)data, obj, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All,
                        Converters = new[] { (JsonConverter)c, new VectorConverter() },
                        ContractResolver = AllPropertiesContractResolver.Singleton
                    });
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                }

            return Task.FromResult(true);
        }

        bool UMI3DSceneLoaderModule.Save<T>(T obj, out object data, SaveReference references)
        {
            //UnityEngine.Debug.Log(obj+ " "+ (obj is Transform).ToString()+ " "+ typeof(T).Name);

            if (!(obj is Transform))
                try
                {
                    var c = new ComponentConverter(references);

                    data = JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All,
                        Converters = new[] { (JsonConverter)c, new VectorConverter() },
                        ContractResolver = AllPropertiesContractResolver.Singleton
                    });
                }
                catch (Exception e)
                {
                    //UnityEngine.Debug.Log(obj);
                    UnityEngine.Debug.LogError(e);
                    data = null;
                }
            else
                data = null;

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

        public static Type Type(this ExtensionSO extension)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(extension.type))
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
        Task<bool> Load<T>(T obj, object data, SaveReference references);
    }

    /// <summary>
    /// Helper class to serialize objects.
    /// </summary>
    /// Typically used to serialize objects that are not defined in the UMI3D core.
    public interface UMI3DSceneLoaderModule<T,Data> where Data : class, new()
    {
        Data Save(T obj,Data data, SaveReference references);
        Task<bool> Load(T obj, Data data, SaveReference references);
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

        public static IEnumerable<Type> GetModulesType(Assembly assembly = null)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => assembly == null || a == assembly)
                .SelectMany(a => a.GetTypes())
                .Where(type => type.IsValidType() && !type.IsIgnoreAttribute())
                .OrderByDescending(GetTypeOrder);
        }

        static int GetTypeOrder(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(UMI3DSceneLoaderOrderAttribute), true);
            if (type.GetCustomAttributes(typeof(UMI3DSceneLoaderOrderAttribute), true).Length > 0)
            {
                return attributes.Select(a => a as UMI3DSceneLoaderOrderAttribute).First().priotity;
            }

            if (type == typeof(object))
                return -1;

            return GetTypeOrder(type.BaseType) +1;
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

        Task<bool> UMI3DSceneLoaderModule.Load<T>(T obj, object data, SaveReference references)
        {
            if (type.IsAssignableFrom(typeof(T)) && dataType.IsAssignableFrom(data.GetType()))
                return (Task<bool>)methodLoad.Invoke(module, new object[] { obj, data, references });
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