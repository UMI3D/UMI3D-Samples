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
using System.Collections.Generic;
using umi3d.common;
using umi3d.edk;
using UnityEngine;

public class AbstractNodeSO
{
    public bool isStatic { get; set; }
    public bool active { get; set; }
    public long AnchorId { get; set; }
    public bool immersiveOnly { get; set; }


}

public class NodeSO : AbstractNodeSO
{
    public string nodeName { get; set; }
    public bool xBillboard { get; set; }
    public bool yBillboard { get; set; }
    public bool hasCollider { get; set; }
    public ColliderType colliderType { get; set; }
    public bool convex { get; set; }
    public Vector3 colliderCenter { get; set; }
    public float colliderRadius { get; set; }
    public Vector3 colliderBoxSize { get; set; }
    public float colliderHeight { get; set; }
    public DirectionalType colliderDirection { get; set; }
    public bool isMeshCustom { get; set; }
    public UMI3DResource customMeshCollider { get; set; }
}

public class RenderedNodeSO : NodeSO
{
    public bool overrideModelMaterials { get; set; }
    public List<MaterialOverrider> materialsOverrider { get; set; }
    public bool castShadow { get; set; }
    public bool receiveShadow { get; set; }
}

public class ModelSO : RenderedNodeSO
{

}