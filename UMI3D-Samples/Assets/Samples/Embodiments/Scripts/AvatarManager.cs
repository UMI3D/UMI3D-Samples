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

using inetum.unityUtils;

using System.Collections.Generic;
using System.Linq;

using umi3d.common.userCapture;
using umi3d.edk;
using umi3d.edk.binding;
using umi3d.edk.collaboration;
using umi3d.edk.userCapture.binding;
using umi3d.edk.userCapture.tracking;

using UnityEngine;

public class AvatarManager : MonoBehaviour, IAvatarManager
{
    #region Fields

    [HideInInspector]
    public IReadOnlyDictionary<UMI3DUser, UMI3DModel> HandledAvatars => handledAvatars;

    private readonly Dictionary<UMI3DUser, UMI3DModel> handledAvatars = new();

    public event System.Action<UMI3DCollaborationUser> UserHandled;

    public event System.Action<UMI3DCollaborationUser> UserUnhandled;

    #region Avatar Model

    [Header("Avatar")]
    [SerializeField, EditorReadOnly, Tooltip("Scene where to instantiate avatar.")]
    private UMI3DScene AvatarScene;

    [SerializeField, EditorReadOnly, Tooltip("Avatar model to load.")]
    private UMI3DResource AvatarModel;

    [System.Serializable]
    public class RigBindingData
    {
        [ConstEnum(typeof(BoneType), typeof(uint))]
        public uint boneType;

        public string rigName;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
    }

    // List all the required binding to make with their offsets
    [System.Serializable]
    public class RigList
    {
        public List<RigBindingData> binds;
    }

    [Header("Rigging")]
    [SerializeField, EditorReadOnly, Tooltip("If true, the rigs of the avatar are bound to the user's skeleton bones.")]
    private bool shouldBindAvatarRigs;

    [SerializeField, EditorReadOnly, Tooltip("List all the required binding to make with their offsets.")]
    private RigList Rigs;

    #endregion Avatar Model

    #endregion Fields

    private IBindingService bindingHelperService;
    private IUMI3DServer UMI3DServerService;

    private void Start()
    {
        bindingHelperService = BindingManager.Instance;
        UMI3DServerService = UMI3DServer.Instance;
        UMI3DServerService.OnUserActive.AddListener((user) => Handle(user as UMI3DCollaborationUser));
        UMI3DServerService.OnUserLeave.AddListener((user) => Unhandle(user as UMI3DCollaborationUser));
        UMI3DServerService.OnUserMissing.AddListener((user) => Unhandle(user as UMI3DCollaborationUser));
    }

    private void Handle(UMI3DCollaborationUser user)
    {
        if (user == null || handledAvatars.ContainsKey(user))
            return;

        SendAvatar(user);
    }

    private void Unhandle(UMI3DCollaborationUser user)
    {
        if (user == null || !handledAvatars.ContainsKey(user))
            return;

        Transaction t = new() { reliable = true };
        if (shouldBindAvatarRigs)
            t.AddIfNotNull(bindingHelperService.RemoveAllBindings(handledAvatars[user].Id()));
        t.AddIfNotNull(handledAvatars[user].GetDeleteEntity());
        t.Dispatch();

        UnityEngine.Object.Destroy(handledAvatars[user].gameObject);

        handledAvatars.Remove(user);

        UserUnhandled?.Invoke(user);
    }

    private void SendAvatar(UMI3DCollaborationUser user)
    {
        Transaction t = new() { reliable = true };
        t.AddIfNotNull(LoadAvatarModel(user));

        if (shouldBindAvatarRigs)
            t.AddIfNotNull(BindAvatar(user, handledAvatars[user]));

        t.Dispatch();

        UserHandled?.Invoke(user);
    }

    private Operation LoadAvatarModel(UMI3DCollaborationUser user)
    {
        GameObject avatarModelNode = new($"Avatar Model - user {user.Id()}");

        avatarModelNode.transform.SetParent(AvatarScene.transform);
        avatarModelNode.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        UMI3DModel avatarModel = avatarModelNode.AddComponent<UMI3DModel>();

        avatarModel.objectModel.SetValue(AvatarModel);
        avatarModel.objectScale.SetValue(user.userSize.GetValue(user).Struct());

        handledAvatars.Add(user, avatarModel);

        return avatarModel.GetLoadEntity();
    }

    private IEnumerable<Operation> BindAvatar(UMI3DTrackedUser user, UMI3DModel avatarModel)
    {
        List<Operation> ops = new();

        // hide head for own user avatar (and legs in VR)
        var bindingsForUser = Rigs.binds.Select(bind =>
        {
            if (bind.boneType == BoneType.Neck || (user.HasHeadMountedDisplay && bonesToHideInVR.Contains(bind.boneType)))
            {
                return new RigBoneBinding(avatarModel.Id(), bind.rigName, user.Id(), bind.boneType)
                {
                    syncScale = true,
                    offsetScale = Vector3.zero,
                };
            }
            else
            {
                return new RigBoneBinding(avatarModel.Id(), bind.rigName, user.Id(), bind.boneType)
                {
                    syncPosition = true,
                    offsetPosition = bind.positionOffset,
                    syncRotation = true,
                    offsetRotation = Quaternion.Euler(bind.rotationOffset),
                };
            }
        }).Cast<AbstractSingleBinding>();

        MultiBinding multiBindingForUser = new(avatarModel.Id())
        {
            partialFit = false,
            priority = 100,
            bindings = bindingsForUser.ToList()
        };

        // others receive normal full avatar
        var bindingsForOthers = Rigs.binds.Select(bind =>
                new RigBoneBinding(avatarModel.Id(), bind.rigName, user.Id(), bind.boneType)
                {
                    syncPosition = true,
                    offsetPosition = bind.positionOffset,
                    syncRotation = true,
                    offsetRotation = Quaternion.Euler(bind.rotationOffset),
                }).Cast<AbstractSingleBinding>();

        MultiBinding multiBindingForOthers = new(avatarModel.Id())
        {
            partialFit = false,
            priority = 100,
            bindings = bindingsForOthers.ToList()
        };

        ops.AddRange(bindingHelperService.AddBinding(multiBindingForOthers)); //set value as synchronized one
        ops.AddRange(bindingHelperService.RemoveBinding(multiBindingForOthers, new UMI3DUser[1] { user }));
        ops.AddRange(bindingHelperService.AddBinding(multiBindingForUser, new UMI3DUser[1] { user }));

        return ops;
    }


    private static HashSet<uint> bonesToHideInVR = new()
    {
        BoneType.Head,
        BoneType.LeftHip,
        BoneType.RightHip
    };
}