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

using System.Linq;
using umi3d.edk;
using umi3d.edk.collaboration;
using umi3d.edk.userCapture;
using UnityEngine;

public class BindingPlanets : MonoBehaviour
{
    private bool isOrbiting = false;

    public UMI3DModel parent;
    public UMI3DModel child;

    public bool syncPosition;
    public bool syncRotation;

    private IBindingService bindingHelperServer;

    private void Start()
    {
        bindingHelperServer = BindingManager.Instance;
    }

    public void TriggerOrbiting()
    {
        if (!isOrbiting)
            StartOrbiting();
        else
            EndOrbiting();
    }

    private void StartOrbiting()
    {
        NodeBinding binding = new(child.Id(), parent.Id())
        {
            syncPosition = syncPosition,
            offsetPosition = child.transform.position - parent.transform.position,
            syncRotation = syncRotation,
            offsetRotation = Quaternion.Inverse(parent.transform.rotation) * child.transform.rotation,
        };

        var op = bindingHelperServer.AddBinding(binding);

        Transaction t = new()
        {
            reliable = true
        };
        t.AddIfNotNull(op);
        t.Dispatch();
        
        isOrbiting = true;
    }

    private void EndOrbiting()
    {
        var op = bindingHelperServer.RemoveAllBindings(child.Id());

        Transaction t = new()
        {
            reliable = true
        };

        t.AddIfNotNull(op);
        t.AddIfNotNull(child.objectPosition.SetValue(child.objectPosition.GetValue(), forceOperation: true));
        t.Dispatch();
        
        isOrbiting = false;
    }
}