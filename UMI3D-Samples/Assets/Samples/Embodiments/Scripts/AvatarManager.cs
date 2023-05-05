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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using umi3d.common;
using umi3d.common.collaboration;
using umi3d.common.interaction;
using umi3d.common.userCapture;
using umi3d.edk;
using umi3d.edk.collaboration;
using umi3d.edk.userCapture;
using UnityEngine;
using UnityEngine.XR;

public class AvatarManager : MonoBehaviour
{
    #region Fields
    HashSet<UMI3DUser> Handled = new HashSet<UMI3DUser>();

    #region Avatar Model
    [Header("Avatar")]
    [SerializeField, EditorReadOnly]
    UMI3DResource AvatarModel;

    [System.Serializable]
    public class RigBindingData
    {
        [ConstEnum(typeof(BoneType), typeof(uint))]
        public uint boneType;
        public string rigName;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
    }

    [System.Serializable]
    public class RigList
    {
        public List<RigBindingData> binds;
    }

    [SerializeField, EditorReadOnly]
    bool bindRig;

    [SerializeField, EditorReadOnly]
    RigList Rigs;

    #endregion Avatar Model

    #region Emotes

    [Header("Emotes")]
    [SerializeField]
    EmotesSubskeletonDescription emotesSubskeletonData = new();

    [Serializable]
    public class EmotesSubskeletonDescription
    {
        [SerializeField]
        public UMI3DResource emoteSubkeletonBundleResource;

        [SerializeField]
        public List<string> animatorStateNames;

        [SerializeField]
        public UMI3DEmotesConfig emoteConfig;
    }

    #endregion Emotes

    #endregion Fields

    // Start is called before the first frame update
    void Start()
    {
        //UMI3DEmbodimentManager.Instance.NewEmbodiment.AddListener(NewAvatar);
        //TODO: find a way to start newavatar
        UMI3DCollaborationServer.Instance.OnUserJoin.AddListener(Handle);
        UMI3DCollaborationServer.Instance.OnUserLeave.AddListener(Unhandle);
    }

    void Handle(UMI3DUser user)
    {
        if (user is not UMI3DTrackedUser trackedUser)
            return;

        if (!Handled.Contains(UMI3DCollaborationServer.Collaboration.GetUser(user.Id())))
        {
            UMI3DCollaborationServer.Instance.OnUserActive.AddListener(NewAvatar);
        }
    }

    void Unhandle(UMI3DUser user)
    {
        if (Handled.Contains(user))
            Handled.Remove(user);
    }

    void NewAvatar(UMI3DUser user)
    {
        var trackedUser = user as UMI3DTrackedUser;

       
        Transaction t = new() { reliable = true };
        t.AddIfNotNull(LoadAvatar(trackedUser, out UMI3DModel avatarModel));
        t.AddIfNotNull(BindAvatar(trackedUser, avatarModel));
        if (trackedUser is UMI3DCollaborationUser collabUser)
        {
            t.AddIfNotNull(LoadEmotes(collabUser, emotesSubskeletonData));
        }
        t.Dispatch();
        UMI3DCollaborationServer.Instance.OnUserActive.RemoveListener(NewAvatar);
    }

    private Operation LoadAvatar(UMI3DTrackedUser trackedUser, out UMI3DModel avatarModel)
    {
        Debug.Log("Load avatar");

        GameObject avatarModelnode = new("AvatarModel");
        UMI3DNode skeletonParentNode = UMI3DEnvironment.Instance.GetEntityInstance<UMI3DNode>(trackedUser.CurrentTrackingFrame.parentId);
        avatarModelnode.transform.SetParent(skeletonParentNode.transform);
        avatarModelnode.transform.localPosition = Vector3.zero;
        avatarModelnode.transform.localRotation = Quaternion.identity;

        avatarModel = avatarModelnode.AddComponent<UMI3DModel>();
        avatarModel.objectModel.SetValue(AvatarModel);
        // TODO : Re-enable size
        //avatarModel.objectScale.SetValue(UMI3DEmbodimentManager.Instance.embodimentSize[user.Id()]);

        return avatarModel.GetLoadEntity();
    }

    List<Operation> BindAvatar(UMI3DTrackedUser user, UMI3DModel avatarModel)
    {
        List<Operation> ops = new();

        if (bindRig)
        {
            var bindings = Rigs.binds.Select(bind => new RigBoneBinding(avatarModel.Id(), bind.boneType, user.Id())
            {
                users = new() { user },
                rigName = bind.rigName,
                offsetRotation = Quaternion.Euler(bind.rotationOffset),
                offsetPosition = bind.positionOffset,
                syncPosition = bind.boneType.Equals(BoneType.CenterFeet) || bind.boneType.Equals(BoneType.Hips),
            }).Cast<AbstractSingleBinding>();

            MultiBinding multiBinding = new(avatarModel.Id())
            {
                partialFit = false,
                priority = 100,
                bindings = bindings.ToList()
            };

            ops.AddRange(BindingHelper.Instance.AddBinding(multiBinding));
        }

        return ops;
    }

    private List<Operation> LoadEmotes(UMI3DCollaborationUser user, EmotesSubskeletonDescription emoteSubskeleton)
    {
        List<Operation> ops = new();

        GameObject skeletonNode = new("Emote subskeleton");
        UMI3DNode skeletonParentNode = UMI3DEnvironment.Instance.GetEntityInstance<UMI3DNode>(user.CurrentTrackingFrame.parentId);
        skeletonNode.transform.SetParent(skeletonParentNode.transform);
        skeletonNode.transform.localPosition = Vector3.zero;
        skeletonNode.transform.localRotation = Quaternion.identity;

        UMI3DSkeletonNode skeleton = skeletonNode.AddComponent<UMI3DSkeletonNode>();
        skeleton.objectModel.SetValue(emoteSubskeleton.emoteSubkeletonBundleResource);
        ops.Add(skeleton.GetLoadEntity());

        // Create animations
        List<UMI3DAbstractAnimation> animations = new();
        foreach (var animatorStateName in emoteSubskeleton.animatorStateNames.Take(emoteSubskeleton.emoteConfig.IncludedEmotes.Count))
        {
            UMI3DAnimatorAnimation animation = skeletonNode.AddComponent<UMI3DAnimatorAnimation>();
            animation.Register();
            animation.objectNode.SetValue(skeleton);
            animation.objectStateName.SetValue(animatorStateName);
            animations.Add(animation);
            ops.Add(animation.GetLoadEntity());
        }

        // Associate animation with emotes
        EmoteDispatcher.Instance.EmotesConfigs.Add(user.Id(), emoteSubskeleton.emoteConfig);

        int indexAnim = 0;
        foreach (var emote in emoteSubskeleton.emoteConfig.IncludedEmotes)
        {
            UMI3DAbstractAnimation animation = animations[indexAnim++];

            if (string.IsNullOrEmpty(emote.label) && animation is UMI3DAnimatorAnimation animatorAnimation)
                emote.label = animatorAnimation.objectStateName.GetValue();

            emote.AnimationId.SetValue(user, animation.Id());
            emote.Available.SetValue(user, emoteSubskeleton.emoteConfig.allAvailableAtStartByDefault || emote.availableAtStart);

            ops.Add(animation.GetLoadEntity());
        }
        ops.Add(emoteSubskeleton.emoteConfig.GetLoadEntity());

        return ops;
    }
}