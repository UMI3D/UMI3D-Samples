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

using umi3d.common;
using umi3d.edk;

using UnityEngine;

namespace Assembly_CSharp // TODO: Complete and lowercase the namespace
{
    [RequireComponent(typeof(UMI3DNode))]
    public class SimpleOscilation : MonoBehaviour
    {
        public enum Axes
        {
            X, Y, Z
        }

        public Axes Axe;

        public float Speed;
        public float Amplitude;

        [Header("Transaction settings")]
        public bool selfManaged = false;

        public float updateFrequency = 5;

        private Vector3 startPosition;
        private float startTime;

        private UMI3DNode node;
        private float lastTime;

        private void Start()
        {
            startPosition = transform.position;
            startTime = Time.time;
            node = GetComponent<UMI3DNode>();

            UMI3DServer.Instance.OnUserJoin.AddListener((user) =>
            {
                var t = new Transaction() { reliable = true };
                t.AddIfNotNull(new StartInterpolationProperty() { users = new() { user }, entityId = node.Id(), property = UMI3DPropertyKeys.Position, startValue = transform.localPosition });
                t.Dispatch();
            });
        }

        private void Update()
        {
            switch (Axe)
            {
                case Axes.X:
                    transform.position = startPosition + new Vector3(Amplitude * Mathf.Cos(Speed * (Time.time - startTime)), 0, 0);
                    break;

                case Axes.Y:
                    transform.position = startPosition + new Vector3(0, Amplitude * Mathf.Cos(Speed * (Time.time - startTime)), 0);
                    break;

                case Axes.Z:
                    transform.position = startPosition + new Vector3(0, 0, Amplitude * Mathf.Cos(Speed * (Time.time - startTime)));
                    break;

                default:
                    break;
            }

            if (selfManaged)
            {
                if ((Time.time - lastTime) > 1 / updateFrequency)
                {
                    var t = new Transaction() { reliable = false };
                    t.AddIfNotNull(node.objectPosition.SetValue(transform.localPosition));
                    t.Dispatch();
                    lastTime = Time.time;
                }
            }
        }
    }
}