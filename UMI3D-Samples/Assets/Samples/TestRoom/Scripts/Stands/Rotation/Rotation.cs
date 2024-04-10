/*
Copyright 2019 Gfi Informatique

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

public class Rotation : MonoBehaviour
{
    private UMI3DModel model;

    /// <summary>
    /// Last time a user used UMI3DManipualtion linked to this class.
    /// </summary>
    private float lastTimeUsed;

    private float sendingFrequency = 5f;

    private void Start()
    {
        model = GetComponent<UMI3DModel>();

        UMI3DServer.Instance.OnUserJoin.AddListener((user) =>
        {
            var t = new Transaction() { reliable = true };
            t.AddIfNotNull(new StartInterpolationProperty() { users = new() { user }, entityId = model.Id(), property = UMI3DPropertyKeys.Rotation, startValue = transform.localRotation });
            t.Dispatch();
        });
    }

    /// <summary>
    /// This have on purpose to be call by a OnManipulated Event.
    /// </summary>
    /// <param name="user">The who performed the manipulation</param>
    /// <param name="trans">The position delta of the manipulation</param>
    /// <param name="rot">The rotation delta of the manipulation</param>
    public void OnUserManipulation(umi3d.edk.interaction.UMI3DManipulation.ManipulationEventContent content)
    {
        if (Time.time - lastTimeUsed > 1 / sendingFrequency)
        {
            Transaction t = new(true);
            t.AddIfNotNull(model.objectRotation.SetValue(content.rotation));
            t.Dispatch();

            lastTimeUsed = Time.time;
        }
    }
}