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
using umi3d.edk;
using umi3d.edk.collaboration;
using umi3d.edk.userCapture;

using UnityEngine;

public class AvatarManager : MonoBehaviour
{
    #region Fields

    private Dictionary<UMI3DUser, HandledInfos> HandledAvatars = new();

    private class HandledInfos
    {
        public UMI3DModel avatar;
        public UMI3DSkeletonNode emotesSkeletonNode;
        public UMI3DSkeletonNode walkingSkeletonNode;
        public Vector3 lastPosition;
        public List<UMI3DAnimatorAnimation> emotesAnimations = new();
        public UMI3DAnimatorAnimation walkingAnimation = new();
    }

    #region Avatar Model

    [Header("Avatar")]
    [SerializeField]
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
        [SerializeField]
        public UMI3DResource animatedSubkeletonBundleResource;

        [SerializeField]
        public List<string> animatorStateNames;
    }

    #region MovementAnimation

    [Header("Movement animation")]
    [SerializeField]
    private bool sendWalkingAnimator;

    [SerializeField]
    private SubskeletonDescription movementSubskeletonData;

    [SerializeField]
    public float maxSpeed = 1f;

    #endregion MovementAnimation

    #region Emotes

    [Header("Emotes")]
    [SerializeField]
    private bool sendEmotes;

    [SerializeField]
    private EmotesSubskeletonDescription emotesSubskeletonData = new();

    [Serializable]
    public class EmotesSubskeletonDescription : SubskeletonDescription
    {
        [SerializeField]
        public UMI3DEmotesConfig emoteConfig;
    }

    #endregion Emotes

    #endregion Fields

    // Start is called before the first frame update
    private void Start()
    {
        //TODO: find a way to start newavatar
        UMI3DCollaborationServer.Instance.OnUserJoin.AddListener(Handle);
        UMI3DCollaborationServer.Instance.OnUserLeave.AddListener(Unhandle);
        UMI3DForgeServer.avatarFrameEvent += OnUserFrame;
    }

    private void OnUserFrame(UserTrackingFrameDto frameDto, ulong userId)
    {
        var user = UMI3DCollaborationServer.Instance.Users().First(x => x.Id() == frameDto.userId);
        if (HandledAvatars.ContainsKey(user))
        {
            float speed = (frameDto.position - HandledAvatars[user].lastPosition).magnitude / maxSpeed;
            if (HandledAvatars[user].walkingAnimation.objectParameters.GetValue().Count == 0)
                return;
            var op = HandledAvatars[user].walkingAnimation.objectParameters.SetValue("Speed", speed);
            HandledAvatars[user].lastPosition = frameDto.position;
            Transaction t = new();
            t.AddIfNotNull(op);
            t.Dispatch();
        }
    }

    private void Handle(UMI3DUser user)
    {
        if (user is not UMI3DTrackedUser trackedUser)
            return;

        if (!HandledAvatars.ContainsKey(UMI3DCollaborationServer.Collaboration.GetUser(user.Id())))
        {
            UMI3DCollaborationServer.Instance.OnUserActive.AddListener(NewAvatar);
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
                t.AddIfNotNull(HandledAvatars[user].walkingAnimation.GetDeleteEntity());
                t.AddIfNotNull(HandledAvatars[user].walkingSkeletonNode.GetDeleteEntity());
                UnityEngine.Object.Destroy(HandledAvatars[user].walkingSkeletonNode.gameObject);
            }

            if (sendEmotes)
            {
                // emotes
                foreach (var animation in HandledAvatars[user].emotesAnimations)
                    t.AddIfNotNull(animation.GetDeleteEntity());
                t.AddIfNotNull(HandledAvatars[user].emotesSkeletonNode.GetDeleteEntity());
                UnityEngine.Object.Destroy(HandledAvatars[user].emotesSkeletonNode.gameObject);
            }

            // bindings
            t.AddIfNotNull(BindingHelper.Instance.RemoveAllBindings(HandledAvatars[user].avatar.Id()));

            // avatar model
            t.AddIfNotNull(HandledAvatars[user].avatar.GetDeleteEntity());

            UnityEngine.Object.Destroy(HandledAvatars[user].avatar.gameObject);

            t.Dispatch();
            HandledAvatars.Remove(user);
        }
    }

    private void NewAvatar(UMI3DUser user)
    {
        var collabUser = user as UMI3DCollaborationUser;

        Transaction t = new() { reliable = true };
        t.AddIfNotNull(LoadAvatar(collabUser, out UMI3DModel avatarModel));
        t.AddIfNotNull(BindAvatar(collabUser, avatarModel));
        if (sendWalkingAnimator)
            t.AddIfNotNull(LoadWalkingAimations(collabUser, avatarModel, movementSubskeletonData));
        if (sendEmotes)
            t.AddIfNotNull(LoadEmotes(collabUser, avatarModel, emotesSubskeletonData));
        t.Dispatch();
        UMI3DCollaborationServer.Instance.OnUserActive.RemoveListener(NewAvatar);
    }

    private Operation LoadAvatar(UMI3DCollaborationUser user, out UMI3DModel avatarModel)
    {
        GameObject avatarModelnode = new($"AvatarModel_User-{user.Id()}");

        avatarModelnode.transform.SetParent(AvatarScene.transform);
        avatarModelnode.transform.localPosition = Vector3.zero;
        avatarModelnode.transform.localRotation = Quaternion.identity;

        avatarModel = avatarModelnode.AddComponent<UMI3DModel>();
        avatarModel.objectModel.SetValue(AvatarModel);
        avatarModel.objectScale.SetValue(user.userSize.GetValue(user));

        HandledAvatars[user] = new HandledInfos() { avatar = avatarModel };

        SimpleModificationListener.Instance.RemoveNode(avatarModel);

        return avatarModel.GetLoadEntity();
    }

    private List<Operation> BindAvatar(UMI3DTrackedUser user, UMI3DModel avatarModel)
    {
        List<Operation> ops = new();

        if (bindRig)
        {
            var bindings = Rigs.binds.Select(bind => new RigBoneBinding(avatarModel.Id(), bind.boneType, user.Id())
            {
                users = new() { user },
                rigName = bind.rigName,
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

            ops.AddRange(BindingHelper.Instance.AddBinding(multiBinding));
        }

        return ops;
    }

    private List<Operation> LoadWalkingAimations(UMI3DCollaborationUser user, UMI3DNode avatarNode, SubskeletonDescription walkingSubskeleton)
    {
        List<Operation> ops = new();

        // Create skeleton node for animations
        GameObject subskeletonNodeGo = new("Movement animation subskeleton");
        subskeletonNodeGo.transform.SetParent(avatarNode.transform);
        subskeletonNodeGo.transform.localPosition = Vector3.zero;
        subskeletonNodeGo.transform.localRotation = Quaternion.identity;

        UMI3DSkeletonNode skeletonNode = subskeletonNodeGo.AddComponent<UMI3DSkeletonNode>();

        List<UMI3DAbstractAnimation> animations = new();
        foreach (var animationState in walkingSubskeleton.animatorStateNames)
        {
            UMI3DAnimatorAnimation animation = subskeletonNodeGo.AddComponent<UMI3DAnimatorAnimation>();
            animation.Register();
            animation.objectNode.SetValue(skeletonNode);
            animation.objectLooping.SetValue(true);
            animation.objectPlaying.SetValue(true);
            animation.objectParameters.Add("Speed", 0);
            animation.objectStateName.SetValue(animationState);
            HandledAvatars[user].walkingAnimation = animation;
            ops.Add(animation.GetLoadEntity());
            animations.Add(animation);
        }

        skeletonNode.objectModel.SetValue(walkingSubskeleton.animatedSubkeletonBundleResource);
        skeletonNode.userId = user.Id();
        skeletonNode.priority = 10;
        skeletonNode.animationStates = walkingSubskeleton.animatorStateNames;
        skeletonNode.relatedAnimationIds = animations.Select(x => x.Id()).ToArray();

        HandledAvatars[user].walkingSkeletonNode = skeletonNode;

        ops.Add(skeletonNode.GetLoadEntity());

        return ops;
    }

    private List<Operation> LoadEmotes(UMI3DCollaborationUser user, UMI3DNode avatarNode, EmotesSubskeletonDescription emoteSubskeleton)
    {
        List<Operation> ops = new();

        // Create skeleton node for animations
        GameObject subskeletonGo = new("Emote animation subskeleton");
        subskeletonGo.transform.SetParent(avatarNode.transform);
        subskeletonGo.transform.localPosition = Vector3.zero;
        subskeletonGo.transform.localRotation = Quaternion.identity;

        UMI3DSkeletonNode skeletonNode = subskeletonGo.AddComponent<UMI3DSkeletonNode>();

        var usedAnimationState = emoteSubskeleton.animatorStateNames.Take(emoteSubskeleton.emoteConfig.IncludedEmotes.Count).ToList();

        // Create animations
        List<UMI3DAbstractAnimation> animations = new();
        foreach (var animatorStateName in usedAnimationState)
        {
            UMI3DAnimatorAnimation animation = subskeletonGo.AddComponent<UMI3DAnimatorAnimation>();
            animation.Register();
            animation.objectNode.SetValue(skeletonNode);
            animation.objectStateName.SetValue(animatorStateName);
            animation.objectPlaying.SetValue(false);
            animations.Add(animation);
            HandledAvatars[user].emotesAnimations.Add(animation);
            ops.Add(animation.GetLoadEntity());
        }

        // Create skeleton node as a UMI3D component
        skeletonNode.objectModel.SetValue(emoteSubskeleton.animatedSubkeletonBundleResource);
        skeletonNode.userId = user.Id();
        skeletonNode.priority = 100;
        skeletonNode.animationStates = usedAnimationState;
        skeletonNode.relatedAnimationIds = animations.Select(x => x.Id()).ToArray();
        HandledAvatars[user].emotesSkeletonNode = skeletonNode;
        ops.Add(skeletonNode.GetLoadEntity());

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