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

using inetum.unityUtils;
using System.Collections.Generic;
using umi3d.common;
using umi3d.edk;
using UnityEngine.Rendering;
using UnityEngine;

public class EnvironmentSO
{
    public bool useDto { get; set; }
    public string name { get; set; }
    public Vector3Dto defaultStartPosition { get; set; }
    public Vector3Dto defaultStartOrientation { get; set; }
    public List<AssetLibrary> globalLibraries { get; set; }
    public List<UMI3DResource> preloadedScenes { get; set; }
    public int mode { get; set; }
    public ColorDto skyColor { get; set; }
    public ColorDto horizontalColor { get; set; }
    public ColorDto groundColor { get; set; }
    public float ambientIntensity { get; set; }
    public int skyboxType { get; set; }
    public UMI3DResource skyboxImage { get; set; }
    public float skyboxRotation { get; set; }
    public UMI3DResource defaultMaterial { get; set; }

}
