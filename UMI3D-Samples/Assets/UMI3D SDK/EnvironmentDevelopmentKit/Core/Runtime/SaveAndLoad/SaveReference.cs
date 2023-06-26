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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace umi3d.edk
{
    public class SaveReference
    {
        public void Clear()
        {
            entities.Clear();
            lastId = 0;
        }

        Dictionary<long,object> entities = new Dictionary<long, object>();

        private long NewID()
        {
            long id = LongRandom();
            while (entities.ContainsValue(id)) id = LongRandom();
            return id;
        }

        private ulong lastId = 0;

        private long LongRandom()
        {
            ulong longRand = lastId++;
            if (longRand > long.MaxValue)
                return  (long)(longRand - long.MaxValue) * -1;
            return  (long)longRand;
        }

        public long GetId(object entity)
        {
            if(entities.ContainsValue(entity))
                return entities.First(p => p.Value == entity).Key;

            var id = NewID();
            entities.Add(id, entity);
            return id;
        }

        public long GetId(object entity, long id)
        {
            if (entities.ContainsValue(entity))
                return entities.First(p => p.Value == entity).Key;

            if (entities.ContainsKey(id))
            {
                UnityEngine.Debug.Log($"id:{id} {entities[id]} != {entity}");

                throw new Exception("Id already in collection");
            }

            entities.Add(id, entity);
            return id;
        }

        public E GetEntitySync<E>(long id) where E : class
        {
            return ( GetEntitySync(id)) as E;
        }

        public async Task<E> GetEntity<E> (long id, List<CancellationToken> tokens = null) where E : class 
        {
            return (await GetEntity(id,tokens)) as E;
        }

        public async Task<object> GetEntity(long id, List<CancellationToken> tokens = null)
        {
            if (tokens == null)
                tokens = new();

            while (!entities.ContainsKey(id) && !tokens.Any(token => token.IsCancellationRequested))
            {
                await Task.Yield();
            }
            if (tokens.Any(token => token.IsCancellationRequested))
                return default;
            return entities[id];
        }

        public object GetEntitySync(long id)
        {
            if (!entities.ContainsKey(id))
            {
                return default;
            }
            return entities[id];
        }
    }
}
