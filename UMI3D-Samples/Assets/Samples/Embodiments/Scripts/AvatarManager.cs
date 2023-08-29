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
using System.Collections.Generic;
using System.Linq;

using umi3d.common.userCapture;
using umi3d.common.userCapture.animation;
using umi3d.edk;
using umi3d.edk.binding;
using umi3d.edk.collaboration;
using umi3d.edk.collaboration.emotes;
using umi3d.edk.userCapture.animation;
using umi3d.edk.userCapture.binding;
using umi3d.edk.userCapture.tracking;

using UnityEngine;

public class AvatarManager : MonoBehaviour
{
    #region Fields

    private Dictionary<UMI3DUser, HandledInfos> HandledAvatars = new();

    private class HandledInfos
    {
        public UMI3DModel avatar;
        public UMI3DSkeletonAnimationNode emotesSkeletonNode;
        public UMI3DSkeletonAnimationNode walkingSkeletonNode;
    }

    #region Avatar Model

    [Header("Avatar")]
    [SerializeField, EditorReadOnly]
    private UMI3DScene AvatarScene;

    [SerializeField, EditorReadOnly]
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

    [System.Serializable]
    public class RigList
    {
        public List<RigBindingData> binds;
    }

    [SerializeField, EditorReadOnly]
    private bool bindRig;

    [SerializeField, EditorReadOnly]
    private RigList Rigs;

    #endregion Avatar Model

    [Serializable]
    public class SubskeletonDescription
    {
        [SerializeField, EditorReadOnly]
        public UMI3DResource animatedSubkeletonBundleResource;

        [SerializeField, EditorReadOnly]
        public List<string> animatorStateNames;
    }

    #region MovementAnimation

    [Header("Movement animation")]
    [SerializeField, EditorReadOnly]
    private bool sendWalkingAnimator;

    [SerializeField, EditorReadOnly]
    private SubskeletonDescription movementSubskeletonData;

    #endregion MovementAnimation

    #region Emotes

    [Header("Emotes")]
    [SerializeField, EditorReadOnly]
    private bool sendEmotes;

    [SerializeField, EditorReadOnly]
    private EmotesSubskeletonDescription emotesSubskeletonData = new();

    [Serializable]
    public class EmotesSubskeletonDescription : SubskeletonDescription
    {
        [SerializeField, EditorReadOnly]
        public UMI3DEmotesConfig emoteConfig;
    }

    #endregion Emotes

    #endregion Fields

    private IBindingService bindingHelperServer;

    private void Start()
    {
        //TODO: find a way to start newavatar
        bindingHelperServer = BindingManager.Instance;
        UMI3DCollaborationServer.Instance.OnUserActive.AddListener(Handle);
        UMI3DCollaborationServer.Instance.OnUserLeave.AddListener(Unhandle);
        UMI3DCollaborationServer.Instance.OnUserMissing.AddListener(Unhandle);
    }

    private void Handle(UMI3DUser user)
    {
        if (user is not UMI3DTrackedUser trackedUser)
            return;

        if (!HandledAvatars.ContainsKey(UMI3DCollaborationServer.Collaboration.GetUser(user.Id())))
        {
            HandledAvatars.Add(user, new());
            SendAvatar(trackedUser);
        }
    }

    private void Unhandle(UMI3DUser user)
    {
        if (HandledAvatars.ContainsKey(user))
        {
            Transaction t = new() { reliable = true };

            // walking animation
            if (sendWalkingAnimator)
            {
                t.AddIfNotNull(HandledAvatars[user].walkingSkeletonNode.GetDeleteAnimations());
                t.AddIfNotNull(HandledAvatars[user].walkingSkeletonNode.GetDeleteEntity());
                UnityEngine.Object.Destroy(HandledAvatars[user].walkingSkeletonNode.gameObject);
            }

            if (sendEmotes)
            {
                // emotes
                t.AddIfNotNull(HandledAvatars[user].emotesSkeletonNode.GetDeleteAnimations());
                t.AddIfNotNull(HandledAvatars[user].emotesSkeletonNode.GetDeleteEntity());
                UnityEngine.Object.Destroy(HandledAvatars[user].emotesSkeletonNode.gameObject);
            }

            // bindings
            t.AddIfNotNull(bindingHelperServer.RemoveAllBindings(HandledAvatars[user].avatar.Id()));

            // avatar model
            t.AddIfNotNull(HandledAvatars[user].avatar.GetDeleteEntity());

            UnityEngine.Object.Destroy(HandledAvatars[user].avatar.gameObject);

            t.Dispatch();
            HandledAvatars.Remove(user);
        }
    }

    private void SendAvatar(UMI3DUser user)
    {
        var collabUser = user as UMI3DCollaborationUser;

        Transaction t = new() { reliable = true };
        t.AddIfNotNull(SendAvatarModel(collabUser, out UMI3DModel avatarModel));

        if (bindRig)
            t.AddIfNotNull(BindAvatar(collabUser, avatarModel));
        if (sendWalkingAnimator)
        {
            foreach (var avatar in HandledAvatars.Where(x => x.Key != user))
            {
                t.AddIfNotNull(avatar.Value.walkingSkeletonNode.GetLoadAnimations(user));
            }
            t.AddIfNotNull(SendWalkingAnimations(collabUser, avatarModel, movementSubskeletonData));
        }

        if (sendEmotes)
        {
            foreach (var avatar in HandledAvatars.Where(x => x.Key != user))
            {
                t.AddIfNotNull(avatar.Value.emotesSkeletonNode.GetLoadAnimations(user));
            }
            t.AddIfNotNull(SendEmotes(collabUser, avatarModel, emotesSubskeletonData));
        }

        t.Dispatch();
    }

    private Operation SendAvatarModel(UMI3DCollaborationUser user, out UMI3DModel avatarModel)
    {
        GameObject avatarModelnode = new($"AvatarModel_User-{user.Id()}");

        avatarModelnode.transform.SetParent(AvatarScene.transform);
        avatarModelnode.transform.localPosition = Vector3.zero;
        avatarModelnode.transform.localRotation = Quaternion.identity;

        avatarModel = avatarModelnode.AddComponent<UMI3DModel>();
        SimpleModificationListener.Instance.RemoveNode(avatarModel);

        avatarModel.objectModel.SetValue(AvatarModel);
        avatarModel.objectScale.SetValue(user.userSize.GetValue(user).Struct());

        HandledAvatars[user].avatar = avatarModel;

        return avatarModel.GetLoadEntity();
    }

    private List<Operation> BindAvatar(UMI3DTrackedUser user, UMI3DModel avatarModel)
    {
        List<Operation> ops = new();

        if (bindRig)
        {
            var bindings = Rigs.binds.Select(bind =>
                new RigBoneBinding(avatarModel.Id(), bind.rigName, user.Id(), bind.boneType)
                {
                    syncPosition = true,
                    offsetPosition = bind.positionOffset,
                    syncRotation = true,
                    offsetRotation = Quaternion.Euler(bind.rotationOffset),
                }).Cast<AbstractSingleBinding>();

            MultiBinding multiBinding = new(avatarModel.Id())
            {
                partialFit = false,
                priority = 100,
                bindings = bindings.ToList()
            };

            ops.AddRange(bindingHelperServer.AddBinding(multiBinding));
        }

        return ops;
    }

    private List<Operation> SendWalkingAnimations(UMI3DCollaborationUser user, UMI3DNode avatarNode, SubskeletonDescription walkingSubskeleton)
    {
        List<Operation> ops = new();

        // Create skeleton node for animations
        GameObject subskeletonNodeGo = new("Movement animation subskeleton");
        subskeletonNodeGo.transform.SetParent(avatarNode.transform);
        subskeletonNodeGo.transform.localPosition = Vector3.zero;
        subskeletonNodeGo.transform.localRotation = Quaternion.identity;

        UMI3DSkeletonAnimationNode skeletonNode = subskeletonNodeGo.AddComponent<UMI3DSkeletonAnimationNode>();
        SimpleModificationListener.Instance.RemoveNode(skeletonNode);
        skeletonNode.objectModel.SetValue(walkingSubskeleton.animatedSubkeletonBundleResource);
        skeletonNode.userId = user.Id();
        skeletonNode.priority = -10;
        skeletonNode.animationStates = walkingSubskeleton.animatorStateNames;
        skeletonNode.animatorSelfTrackedParameters = new SkeletonAnimationParameter[1] {
            new()
            {
                parameterKey = (uint)SkeletonAnimatorParameterKeys.SPEED_X_Y,
                ranges = new List<SkeletonAnimationParameter.Range>()
                {
                    new () { startBound = 0f,   endBound = 1f,      result = 0f},
                    new () { startBound = 1f,   endBound = 5f,      result = 3f},
                    new () { startBound = 5f,   endBound = 10f,     result = 10f},
                }
            }};

        // Create Animator animations
        ops.AddRange(skeletonNode.GenerateAnimations(areLooping: true));

        HandledAvatars[user].walkingSkeletonNode = skeletonNode;
        ops.Add(skeletonNode.GetLoadEntity());

        return ops;
    }

    private List<Operation> SendEmotes(UMI3DCollaborationUser user, UMI3DNode avatarNode, EmotesSubskeletonDescription emoteSubskeleton)
    {
        List<Operation> ops = new();

        // Create skeleton node for animations
        GameObject subskeletonGo = new("Emote animation subskeleton");
        subskeletonGo.transform.SetParent(avatarNode.transform);
        subskeletonGo.transform.localPosition = Vector3.zero;
        subskeletonGo.transform.localRotation = Quaternion.identity;

        UMI3DSkeletonAnimationNode skeletonNode = subskeletonGo.AddComponent<UMI3DSkeletonAnimationNode>();
        SimpleModificationListener.Instance.RemoveNode(skeletonNode);

        var usedAnimationState = emoteSubskeleton.animatorStateNames.Take(emoteSubskeleton.emoteConfig.IncludedEmotes.Count).ToList();

        // Create skeleton node as a UMI3D component
        skeletonNode.objectModel.SetValue(emoteSubskeleton.animatedSubkeletonBundleResource);
        skeletonNode.userId = user.Id();
        skeletonNode.priority = 100;
        skeletonNode.animationStates = usedAnimationState;

        // Create animations
        ops.AddRange(skeletonNode.GenerateAnimations(stateNames: usedAnimationState, arePlaying: false));

        HandledAvatars[user].emotesSkeletonNode = skeletonNode;
        ops.Add(skeletonNode.GetLoadEntity());

        // Associate animation with emotes
        EmoteDispatcher.Instance.EmotesConfigs[user.Id()] = emoteSubskeleton.emoteConfig;

        int indexAnim = 0;
        foreach (var emote in emoteSubskeleton.emoteConfig.IncludedEmotes)
        {
            UMI3DAbstractAnimation animation = UMI3DEnvironment.Instance._GetEntityInstance<UMI3DAbstractAnimation>(skeletonNode.relatedAnimationIds[indexAnim++]);

            if (string.IsNullOrEmpty(emote.label) && animation is UMI3DAnimatorAnimation animatorAnimation)
                emote.label = animatorAnimation.objectStateName.GetValue();

            emote.AnimationId.SetValue(user, animation.Id());
            emote.Available.SetValue(user, emoteSubskeleton.emoteConfig.allAvailableAtStartByDefault || emote.availableAtStart);

            ops.Add(animation.GetLoadEntity());
        }

        ops.Add(emoteSubskeleton.emoteConfig.GetLoadEntity(new() { user }));

        return ops;
    }
}