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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace umi3d.common
{
    /// <summary>
    /// Helper class for byte serialization.
    /// </summary>
    public static class UMI3DSerializer
    {
        private const DebugScope scope = DebugScope.Common | DebugScope.Core | DebugScope.Bytes;

        /// <summary>
        /// Networking helpers from other modules.
        /// </summary>
        private static readonly List<UMI3DSerializerModule> modules = new List<UMI3DSerializerModule>();

        /// <summary>
        /// Add a networking module.
        /// </summary>
        /// <param name="module"></param>
        public static void AddModule(UMI3DSerializerModule module)
        {
            modules.Add(module);
        }

        /// <summary>
        /// Remove a networking module
        /// </summary>
        /// <param name="module"></param>
        public static void RemoveModule(UMI3DSerializerModule module)
        {
            modules.Remove(module);
        }

        /// <summary>
        /// Add a list of module
        /// </summary>
        /// <param name="moduleList"></param>
        public static void AddModule(List<UMI3DSerializerModule> moduleList)
        {
            foreach (UMI3DSerializerModule module in moduleList)
                modules.Add(module);
        }

        /// <summary>
        /// Remove a list of module
        /// </summary>
        /// <param name="moduleList"></param>
        public static void RemoveModule(List<UMI3DSerializerModule> moduleList)
        {
            foreach (UMI3DSerializerModule module in moduleList)
                modules.Remove(module);
        }

        /// <summary>
        /// Read a value from a ByteContainer and update it
        /// </summary>
        /// <typeparam name="T">Type to read</typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        public static T Read<T>(ByteContainer container)
        {
            TryRead<T>(container, out T result);
            return result;
        }

        /// <summary>
        /// Try to read a value from a ByteContainer and update it.
        /// </summary>
        /// <typeparam name="T">Type to read</typeparam>
        /// <param name="container"></param>
        /// <param name="result">result if readable</param>
        /// <returns>state if the value is readable from this byte container</returns>
        public static bool TryRead<T>(ByteContainer container, out T result)
        {
            bool read;
            foreach (UMI3DSerializerModule module in modules)
            {
                if (module.Read<T>(container, out read, out result))
                    return read;
            }

            switch (true)
            {
                case true when typeof(T).IsSubclassOf(typeof(TypedDictionaryEntry)):
                    result = default(T);
                    var entry = (TypedDictionaryEntry)Activator.CreateInstance(typeof(T));
                    if (entry.Read(container))
                    {
                        result = (T)(object)entry;
                        return true;
                    }
                    return false;
                default:
                    throw new Exception($"Missing case [{typeof(T)} was not catched]");
            }
            result = default(T);
            return false;
        }

        /// <summary>
        /// Read a byteContainer known to be a collection of elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="container"></param>
        /// <returns>A deserialized array of the elements.</returns>
        public static T[] ReadArray<T>(ByteContainer container)
        {
            return ReadList<T>(container).ToArray();
        }

        /// <summary>
        /// Read a byteContainer known to be a collection of elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="container"></param>
        /// <returns>A deserialized list of the elements.</returns>
        /// <exception cref="Exception">Not a known collection type.</exception>
        public static List<T> ReadList<T>(ByteContainer container)
        {
            byte listType = UMI3DSerializer.Read<byte>(container);

            switch (listType)
            {
                case UMI3DObjectKeys.CountArray:
                    return ReadCountList<T>(container);
                case UMI3DObjectKeys.IndexesArray:
                    return ReadIndexesList<T>(container);
                default:
                    throw new Exception($"Not a known collection type {container}");
            }
        }

        /// <summary>
        /// Applies an operation to a <paramref name="list"/> based on the value in a byte <paramref name="container"/>.
        /// </summary>
        /// Passing an invalid or no operation id will clear the list and fill it again with the container values.
        /// <typeparam name="T"></typeparam>
        /// <param name="operationId">Operation to apply UMI3D key</param>
        /// <param name="container">Byte container with the interesting value (index or index+value)</param>
        /// <param name="list">List to apply the changes</param>
        public static void ReadList<T>(uint operationId, ByteContainer container, List<T> list)
        {
            switch (operationId)
            {
                case UMI3DOperationKeys.SetEntityListAddProperty:
                    int ind = UMI3DSerializer.Read<int>(container);
                    T value = UMI3DSerializer.Read<T>(container);
                    if (ind == list.Count)
                        list.Add(value);
                    else if (ind < list.Count && ind >= 0)
                        list.Insert(ind, value);
                    else
                        UMI3DLogger.LogWarning($"Add value ignore for {ind} in collection of size {list.Count}", scope);
                    break;
                case UMI3DOperationKeys.SetEntityListRemoveProperty:
                    list.RemoveAt(UMI3DSerializer.Read<int>(container));
                    break;
                case UMI3DOperationKeys.SetEntityListProperty:
                    int index = UMI3DSerializer.Read<int>(container);
                    T v = UMI3DSerializer.Read<T>(container);
                    list[index] = v;
                    break;
                default:
                    list.Clear();
                    list.AddRange(UMI3DSerializer.ReadList<T>(container));
                    break;
            }
        }

        /// <summary>
        /// Generic class to describe a Dictionary entry that can be read from a ByteContainer
        /// </summary>
        private abstract class TypedDictionaryEntry
        {
            /// <summary>
            /// Read the entry.
            /// </summary>
            /// <param name="container"></param>
            /// <returns></returns>
            public abstract bool Read(ByteContainer container);
        }

        /// <summary>
        /// Utility class to describe a Dictionary<K,V> entry that can be read from a ByteContainer
        /// </summary>
        /// <typeparam name="K">Key Type</typeparam>
        /// <typeparam name="V">Value Type</typeparam>
        private class TypedDictionaryEntry<K, V> : TypedDictionaryEntry
        {
            public V value;
            public K key;

            public KeyValuePair<K, V> keyValuePair => new KeyValuePair<K, V>(key, value);

            /// <summary>
            /// Read both the key and value stored in the container.
            /// </summary>
            /// <param name="container"></param>
            /// <returns></returns>
            public override bool Read(ByteContainer container)
            {
                return TryRead(container, out key) && TryRead(container, out value);
            }
        }

        /// <summary>
        /// Read a Dictionary<K,V> From a ByteContainer
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        public static Dictionary<K, V> ReadDictionary<K, V>(ByteContainer container)
        {
            return ReadList<TypedDictionaryEntry<K, V>>(container).Select(k => k.keyValuePair).ToDictionary();
        }

        /// <summary>
        /// Read a List from a container where starting indexes are given for each values at the begining of the list. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        private static List<T> ReadIndexesList<T>(ByteContainer container)
        {
            var result = new List<T>();
            int indexMaxPos = -1;
            int maxLength = container.bytes.Length;
            int valueIndex = -1;
            for (; container.position < indexMaxPos || indexMaxPos == -1;)
            {
                int nopIndex = UMI3DSerializer.Read<int>(container);

                if (indexMaxPos == -1)
                {
                    indexMaxPos = valueIndex = nopIndex;
                    continue;
                }
                var SubContainer = new ByteContainer(container.timeStep, container.bytes) { position = valueIndex, length = nopIndex - valueIndex };
                if (!TryRead(SubContainer, out T v)) break;
                result.Add(v);
                valueIndex = nopIndex;
            }
            {
                var SubContainer = new ByteContainer(container.timeStep, container.bytes) { position = valueIndex, length = maxLength - valueIndex };
                if (TryRead(SubContainer, out T v))
                    result.Add(v);
            }
            return result;
        }

        /// <summary>
        /// Read a collection in way that cannot exceed the indicated collection size in the <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        private static List<T> ReadCountList<T>(ByteContainer container)
        {
            int count = UMI3DSerializer.Read<int>(container);
            var res = new List<T>();
            int Length = container.bytes.Length;
            for (int i = 0; container.position < Length && container.length > 0 && i < count; i++)
            {
                if (TryRead<T>(container, out T result))
                    res.Add(result);
                else
                    break;
            }
            return res;
        }

        /// <summary>
        /// Read an array of bytes from a <paramref name="container"/>.
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public static byte[] ReadByteArray(ByteContainer container)
        {
            byte type = UMI3DSerializer.Read<byte>(container);
            int count = UMI3DSerializer.Read<int>(container);
            byte[] res = new byte[count];
            container.bytes.CopyRangeTo(res, 0, container.position, container.position + count - 1);
            return res;
        }

        /// <summary>
        /// Get an enumerable collection of <see cref="ByteContainer"/> 
        /// from a <paramref name="container"/> that is an indexed array.
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IEnumerable<ByteContainer> ReadIndexesList(ByteContainer container)
        {
            byte listType = UMI3DSerializer.Read<byte>(container);
            if (listType != UMI3DObjectKeys.IndexesArray)
                yield break;
            int indexMaxPos = -1;
            int maxLength = container.bytes.Length;
            int valueIndex = -1;
            for (; container.position < indexMaxPos || indexMaxPos == -1;)
            {
                int nopIndex = UMI3DSerializer.Read<int>(container);

                if (indexMaxPos == -1)
                {
                    indexMaxPos = valueIndex = nopIndex;
                    continue;
                }
                var SubContainer = new ByteContainer(container.timeStep, container.bytes) { position = valueIndex, length = nopIndex - valueIndex };
                yield return SubContainer;
                valueIndex = nopIndex;
            }
            {
                var SubContainer = new ByteContainer(container.timeStep, container.bytes) { position = valueIndex, length = maxLength - valueIndex };
                yield return SubContainer;
            }
            yield break;
        }

        /// <summary>
        /// Get the right <see cref="UMI3DObjectKeys"/> for a value of a given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Bytable GetType<T>(T value)
        {
            switch (value)
            {
                case bool b:
                    return Write(UMI3DObjectKeys.Bool);
                case double b:
                    return Write(UMI3DObjectKeys.Double);
                case float b:
                    return Write(UMI3DObjectKeys.Float);
                case int b:
                    return Write(UMI3DObjectKeys.Int);
                case SerializableVector2 v:
                case Vector2 b:
                    return Write(UMI3DObjectKeys.Vector2);
                case SerializableVector3 v:
                case Vector3 b:
                    return Write(UMI3DObjectKeys.Vector3);
                case Quaternion q:
                case SerializableVector4 v:
                case Vector4 b:
                    return Write(UMI3DObjectKeys.Vector4);
                case Color b:
                    return Write(UMI3DObjectKeys.Color);
                case TextureDto b:
                    return Write(UMI3DObjectKeys.TextureDto);
                default:
                    return new Bytable();
            }
        }

        /// <summary>
        /// Get a bytable from a enumerable set of values of unknown type.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Bytable Write(IEnumerable value)
        {
            return WriteCollection(value.Cast<object>());
        }

        /// <summary>
        /// Get a bytable from a key-value set of values of unknown type.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Bytable Write(IDictionary value)
        {
            return WriteCollection(value);
        }

        /// <summary>
        /// Get a bytable from any <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Bytable Write<T>(T value)
        {
            foreach (UMI3DSerializerModule module in modules)
            {
                if (module.Write<T>(value, out Bytable bc))
                    return bc;
            }

            switch (value)
            {
                case IBytable b:
                    return b.ToBytableArray();
                case IDictionary Id:
                    return Write(Id);
                case IEnumerable Ie:
                    return Write(Ie);
                case DictionaryEntry De:
                    return Write(De.Key) + Write(De.Value);
                default:
                    break;
            }

            throw new Exception($"Missing case [{typeof(T)}:{value} was not catched]");
        }

        /// <summary>
        /// Get a bytable from a enumerable set of values of unknown type.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Bytable WriteCollection<T>(IEnumerable<T> value)
        {
            if (typeof(IBytable).IsAssignableFrom(typeof(T)) || (value.Count() > 0 && !value.Any(e => !typeof(IBytable).IsAssignableFrom(e.GetType()))))
            {
                return WriteIBytableCollection(value.Select((e) => e as IBytable));
            }
            Bytable b = Write(UMI3DObjectKeys.CountArray) + Write(value.Count());
            foreach (T v in value)
                b += Write(v);
            return b;
        }

        /// <summary>
        /// Get a bytable from a enumerable set of key-value pairs of unknown types.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Bytable WriteCollection(IDictionary value)
        {
            if (value.Count > 0 && !value.Entries().Any(e => !typeof(IBytable).IsAssignableFrom(e.Value.GetType())))
            {
                return WriteIBytableCollection(value.Entries().Select((e) => new DictionaryEntryBytable(e)));
            }
            Bytable b = Write(UMI3DObjectKeys.CountArray) + Write(value.Count);
            foreach (object v in value)
                b += Write(v);
            return b;
        }

        /// <summary>
        /// Utility class for dictionnary serialization of key-value pairs.
        /// </summary>
        private class DictionaryEntryBytable : IBytable
        {
            private readonly object key;
            private readonly IBytable value;

            public DictionaryEntryBytable(DictionaryEntry entry)
            {
                this.key = entry.Key;
                this.value = entry.Value as IBytable;
            }

            /// <inheritdoc/>
            public bool IsCountable()
            {
                return value.IsCountable();
            }

            /// <inheritdoc/>
            public Bytable ToBytableArray(params object[] parameters)
            {
                return Write(key) + Write(value);
            }
        }

        /// <summary>
        /// Get a <see cref="Bytable"/> from an enumerable set of bytes.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Bytable WriteCollection(IEnumerable<byte> value)
        {
            int count = value.Count();
            Bytable b = Write(UMI3DObjectKeys.CountArray) + Write(count);
            Func<byte[], int, int, (int, int)> f = (by, i, bs) =>
            {
                value.ToArray().CopyTo(by, i);
                return (i + count, bs + count);
            };
            return b + new Bytable(count, f);
        }

        /// <summary>
        /// Get a <see cref="Bytable"/> from an enumerable set of adequate values and <paramref name="parameters"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Bytable WriteIBytableCollection(IEnumerable<IBytable> ibytes, params object[] parameters)
        {
            if (ibytes.Count() > 0)
            {
                if (ibytes.First().IsCountable()) return ListToCountBytable(ibytes, parameters);
                else return ListToIndexesBytable(ibytes, parameters);
            }
            return Write(UMI3DObjectKeys.CountArray)
                + Write(0);
        }

        /// <summary>
        /// Get a <see cref="Bytable"/> from an enumerable set of <paramref name="operations"/> to apply and their <paramref name="parameters"/>.
        /// </summary>
        /// <param name="operations"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static Bytable ListToIndexesBytable(IEnumerable<IBytable> operations, params object[] parameters)
        {
            Bytable ret = Write(UMI3DObjectKeys.IndexesArray);

            Func<byte[], int, int, (int, int, int)> f3 = (byte[] by, int i, int j) =>
            {
                return (0, i, j);
            };
            if (operations.Count() > 0)
            {
                int size = operations.Count() * sizeof(int);
                (int, Func<byte[], int, int, (int, int, int)> f3) func = operations
                    .Select(o => o.ToBytableArray(parameters))
                    .Select(c =>
                    {
                        Func<byte[], int, int, (int, int, int)> f1 = (byte[] by, int i, int j) => { (int, int) cr = c.function(by, i, 0); return (cr.Item1, i, j); };
                        return (c.size, f1);
                    })
                    .Aggregate((0, f3)
                    , (a, b) =>
                    {
                        Func<byte[], int, int, (int, int, int)> f2 = (byte[] by, int i, int j) =>
                        {
                            int i2, sj;
                            (i2, i, j) = a.Item2(by, i, j);
                            (i2, i, j) = b.Item2(by, i, j);
                            (j, sj) = UMI3DSerializer.Write(i).function(by, j, 0);
                            i = i2;
                            return (i2, i, j);
                        };
                        return (a.Item1 + b.Item1, f2);
                    });
                int length = size + func.Item1;

                Func<byte[], int, int, (int, int)> f5 = (byte[] by, int i, int bs) =>
                {
                    (int, int, int) couple = func.Item2(by, i + size, i);
                    return (couple.Item1, couple.Item2);
                };
                return ret + new Bytable(length, f5);
            }
            return ret;
        }

        /// <summary>
        /// Get a bytable with a known size from a list of <paramref name="operations"/> to apply and their <paramref name="parameters"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Bytable ListToCountBytable(IEnumerable<IBytable> operations, params object[] parameters)
        {
            return Write(UMI3DObjectKeys.CountArray)
                + Write(operations.Count())
                + operations.Select(op => op.ToBytableArray(parameters));
        }
    }
}