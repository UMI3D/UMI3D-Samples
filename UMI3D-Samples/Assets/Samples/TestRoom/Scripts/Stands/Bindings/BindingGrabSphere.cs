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
using umi3d.edk.userCapture;
using umi3d.common.userCapture;
using UnityEngine;
using umi3d.common;
using System.Collections.Generic;
using umi3d.edk.binding;
using umi3d.edk.userCapture.binding;

[RequireComponent(typeof(UMI3DNode))]
public class BindingGrabSphere : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// Orignal local position of <see cref="node"/>.
    /// </summary>
    Vector3 originalPos;

    /// <summary>
    /// Original local rotation of <see cref="node"/>.
    /// </summary>
    Quaternion originalRot;

    /// <summary>
    /// Current binding when <see cref="node"/> is grabbed.
    /// </summary>
    BoneBinding binding;

    /// <summary>
    /// Id of the current <see cref="UMI3DUser"/> who is grabbing <see cref="node"/>.
    /// </summary>
    ulong ownerUserID;

    /// <summary>
    /// Is the object grabbed ?
    /// </summary>
    private bool isObjectGrabbed = false;

    /// <summary>
    /// Reference to the node grabbed.
    /// </summary>
    UMI3DNode node;

    #endregion

    private IBindingService bindingHelperServer;

    #region Methods

    void Start()
    {
        bindingHelperServer = BindingManager.Instance;

        originalPos = transform.localPosition;
        originalRot = transform.localRotation;

        node = GetComponent<UMI3DNode>();
    }

    void Update()
    {
        if (isObjectGrabbed)
        {
            //node.objectPosition.SetValue(transform.parent.InverseTransformPoint(bindingAnchor.TransformPoint(localPosOffset)));
            //node.objectRotation.SetValue(Quaternion.Inverse(transform.parent.rotation) * bindingAnchor.rotation * localRotOffset);
        }
    }

    /// <summary>
    /// Grabs the object or releases it if it was already hold by a user.
    /// </summary>
    /// <param name="content"></param>
    public void UpdateBindingActivation(umi3d.edk.interaction.AbstractInteraction.InteractionEventContent content)
    {
        UMI3DTrackedUser user = content.user as UMI3DTrackedUser;
        uint bonetype = content.boneType;

        if (!isObjectGrabbed)
        {
            isObjectGrabbed = true;

            var localPosOffset = Vector3.forward * 1.5f; //: bindingAnchor.InverseTransformPoint(transform.position);
            var localRotOffset = transform.rotation; //Quaternion.Inverse(bindingAnchor.rotation) * transform.rotation;

            ownerUserID = user.Id();

            binding = new BoneBinding(node.Id(), ownerUserID, bonetype)
            {
                syncPosition = true,
                offsetPosition = localPosOffset,
                syncRotation = true,
                offsetRotation = localRotOffset,
            };

            var ops = bindingHelperServer.AddBinding(binding);

            Transaction transaction = new() { reliable = true };
            transaction.AddIfNotNull(ops);
            UMI3DServer.Dispatch(transaction);
        }
        else
        {
            if (!user.Id().Equals(ownerUserID))
                return;

            isObjectGrabbed = false;

            Transaction transaction = new() { reliable = true };

            transaction.AddIfNotNull(bindingHelperServer.RemoveBinding(binding));
            transaction.AddIfNotNull(node.objectPosition.SetValue(originalPos));
            transaction.AddIfNotNull(node.objectRotation.SetValue(originalRot));

            UMI3DServer.Dispatch(transaction);
        }
    }

    #endregion
}
