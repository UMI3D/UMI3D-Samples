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

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class InterpolationManager : MonoBehaviour
{
    public bool AutoStartSendingTransaction;

    public bool updateTransform = false;
    public bool isInterpolating = false;

    public UnityEvent UpdateEvent;
    public UnityEvent InterpolationEvent;

    private async void Start()
    {
        if (AutoStartSendingTransaction)
        {
            await Task.Delay(10000);
            UpdateStatus();
        }
    }

    public void UpdateStatus()
    {
        updateTransform = !updateTransform;
        UpdateEvent.Invoke();
    }

    public void InterpolationStatus()
    {
        isInterpolating = !isInterpolating;
        InterpolationEvent.Invoke();
    }
}
