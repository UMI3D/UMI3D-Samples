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

using umi3d.edk;

using UnityEngine;
using UnityEngine.Assertions;

public class KeepInBounds : MonoBehaviour
{
    UMI3DModel model;
    public BoxCollider boxCollider;

    private void Start()
    {
        model = GetComponent<UMI3DModel>();
        Assert.IsNotNull(boxCollider);
    }

    // Update is called once per frame
    private void Update()
    {
        if (Contains(boxCollider, transform.position))
            return;

        Transaction t = new(true);
        transform.position = boxCollider.ClosestPoint(this.transform.position);
        t.AddIfNotNull(model.objectPosition.SetValue(transform.localPosition, true));
        t.Dispatch();
    }

    private bool Contains(BoxCollider collider, Vector3 pointWorldSpace)
    {
        Vector3 pointLocalSpace = boxCollider.transform.InverseTransformPoint(pointWorldSpace) - collider.center;

        float halfX = (collider.size.x * 0.5f);
        float halfY = (collider.size.y * 0.5f);
        float halfZ = (collider.size.z * 0.5f);

        bool X = IsInRange(-halfX, halfX, pointLocalSpace.x);
        bool Y = IsInRange(-halfY, halfY, pointLocalSpace.y);
        bool Z = IsInRange(-halfZ, halfZ, pointLocalSpace.z);
        return X && Y && Z;
    }

    const float EPSILON = 1e-4f; // Mathf.Approximately is too generous

    private static bool IsInRange(float a, float b, float value)
    {
        return Mathf.Abs(a - value) < EPSILON
            || Mathf.Abs(b - value) < EPSILON
            || (a <= value && value <= b); 
    }
}