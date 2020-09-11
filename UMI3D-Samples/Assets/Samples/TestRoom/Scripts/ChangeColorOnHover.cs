﻿/*
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

using System.Collections.Generic;
using umi3d.edk;
using UnityEngine;

public class ChangeColorOnHover : MonoBehaviour
{
    List<string> trackers = new List<string>();

    string ToName(UMI3DUser user, string boneType)
    {
        return $"{user.Id()}:{boneType}";
    }

    public void OnHoverEnter(UMI3DUser user, string boneId)
    {
        var name = ToName(user, boneId);
        if (!trackers.Contains(name))
            trackers.Add(name);
        updateColor();
    }
    public void OnHoverExit(UMI3DUser user, string boneId)
    {
        var name = ToName(user, boneId);
        if (trackers.Contains(name))
        {
            trackers.Remove(ToName(user, boneId));
        }
        updateColor();
    }

    public void updateColor()
    {
        Debug.Log($"hovered {trackers.Count > 0}");
    }

}
