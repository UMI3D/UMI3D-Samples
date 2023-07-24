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

using umi3d.edk;

using UnityEngine;

public class SimpleRotation : MonoBehaviour
{
    public enum Axes
    {
        X, Y, Z
    }

    public Axes Axe;

    public float Speed;

    [Header("Transaction settings")]
    public bool selfManaged = false;

    public float updateFrequency = 5;

    private UMI3DNode node;
    private float lastTime;

    private void Start()
    {
        node = GetComponent<UMI3DNode>();
    }

    // Update is called once per frame
    private void Update()
    {
        switch (Axe)
        {
            case Axes.X:
                transform.Rotate(Speed * Time.deltaTime, 0, 0);
                break;

            case Axes.Y:
                transform.Rotate(0, Speed * Time.deltaTime, 0);
                break;

            case Axes.Z:
                transform.Rotate(0, 0, Speed * Time.deltaTime);
                break;

            default:
                break;
        }
        if (selfManaged)
        {
            if ((Time.time - lastTime) > 1 / updateFrequency)
            {
                var t = new Transaction() { reliable = true };
                t.AddIfNotNull(node.objectRotation.SetValue(transform.rotation));
                t.Dispatch();
                lastTime = Time.time;
            }
        }
    }
}