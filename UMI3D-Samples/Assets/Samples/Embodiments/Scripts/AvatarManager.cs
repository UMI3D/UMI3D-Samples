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
    [SerializeField, EditorReadOnly]
    UMI3DResource Avatar;
    
    HashSet<UMI3DUser> Handled = new HashSet<UMI3DUser>();

    public List<SkeletonData> skeletonsData = new();

    [Serializable]
    public class SkeletonData
    {
        [SerializeField]
        public UMI3DResource SkeletonResource;

        [SerializeField]
        public UMI3DEmotesConfig EmoteConfig;

        [SerializeField]
        public List<string> animatorStateNames;
    }

    [System.Serializable]
    public class Bind
    {
        [ConstEnum(typeof(BoneType),typeof(uint))]
        public uint boneType;
        public string rigName;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
    }

    [System.Serializable]
    public class BindList
    {
        public List<Bind> binds;
    }

    [SerializeField, EditorReadOnly]
    bool bindRig;

    [SerializeField, EditorReadOnly]
    BindList binds;

    [SerializeField, EditorReadOnly]
    Vector3 positionOffset;

    [SerializeField, EditorReadOnly]
    Vector3 rotationOffset;

    // Start is called before the first frame update
    void Start()
    {
        //UMI3DEmbodimentManager.Instance.NewEmbodiment.AddListener(NewAvatar);
        //TODO: find a way to start newavatar
        UMI3DCollaborationServer.Instance.OnUserJoin.AddListener((user) => NewAvatar(user as UMI3DTrackedUser));
    }

    void NewAvatar(UMI3DTrackedUser user)
    {
        if (UMI3DCollaborationServer.Collaboration.GetUser(user.Id()) != null && !Handled.Contains(UMI3DCollaborationServer.Collaboration.GetUser(user.Id())))
        {
            Handled.Add(UMI3DCollaborationServer.Collaboration.GetUser(user.Id()));
            StartCoroutine(_NewAvatar(UMI3DCollaborationServer.Collaboration.GetUser(user.Id())));
        }
    }

    IEnumerator _NewAvatar(UMI3DCollaborationUser user)
    {
        if (user == null) yield break;
        var wait = new WaitForFixedUpdate();

        UMI3DNode SkeletonParentNode = UMI3DEnvironment.GetEntityInstance<UMI3DNode>(user.CurrentTrackingFrame.parentId);

        //while (SkeletonParentNode == null)
        //{
        //    yield return wait;
        //    SkeletonParentNode = UMI3DEnvironment.Instance.GetEntityInstance<UMI3DNode>(user.CurrentTrackingFrame.parentId);
        //}

        while (user.status.Equals(StatusType.READY))
        {
            yield return wait;
        }

        LoadAvatar(user);
    }

    private void LoadAvatar(UMI3DCollaborationUser user)
    {
        Debug.Log("Load avatar");
        List<Operation> ops = new();

        GameObject avatarModelnode = new("AvatarModel");
        //avatarModelnode.transform.SetParent(parentNode.transform);
        avatarModelnode.transform.localPosition = Vector3.zero;
        avatarModelnode.transform.localRotation = Quaternion.identity;

        UMI3DModel avatarModel = avatarModelnode.AddComponent<UMI3DModel>();
        avatarModel.objectModel.SetValue(Avatar);
        //avatarModel.objectScale.SetValue(UMI3DEmbodimentManager.Instance.embodimentSize[user.Id()]);

        LoadEntity op = avatarModel.GetLoadEntity();
        ops.Add(op);

        foreach (var skeletonData in skeletonsData)
        {
            GameObject skeletonNode = new("Subskeleton");
            //skeletonNode.transform.SetParent(parentNode.transform);
            skeletonNode.transform.localPosition = Vector3.zero;
            skeletonNode.transform.localRotation = Quaternion.identity;

            UMI3DSkeletonNode skeleton = skeletonNode.AddComponent<UMI3DSkeletonNode>();
            skeleton.objectModel.SetValue(skeletonData.SkeletonResource);
            //skeleton.objectScale.SetValue(UMI3DEmbodimentManager.Instance.embodimentSize[user.Id()]);
            ops.Add(skeleton.GetLoadEntity());

            // Create animations
            List<UMI3DAbstractAnimation> animations = new();
            foreach (var animatorStateName in skeletonData.animatorStateNames)
            {
                UMI3DAnimatorAnimation animation = skeletonNode.AddComponent<UMI3DAnimatorAnimation>();
                animation.Register();
                animation.objectNode.SetValue(skeleton);
                animation.objectStateName.SetValue(animatorStateName);
                animations.Add(animation);
                ops.Add(animation.GetLoadEntity());
            }

            // Associate animation with emotes
            if (skeletonData.EmoteConfig == null)
                continue;

            UMI3DCollaborationServer.Collaboration.emotesConfigs.Add(user.Id(), skeletonData.EmoteConfig);

            int indexAnim = 0;
            foreach (var emote in skeletonData.EmoteConfig.IncludedEmotes)
            {
                UMI3DAbstractAnimation animation = animations[indexAnim++];

                if (string.IsNullOrEmpty(emote.label) && animation is UMI3DAnimatorAnimation animatorAnimation)
                    emote.label = animatorAnimation.objectStateName.GetValue();

                emote.AnimationId.SetValue(user, animation.Id());
                emote.Available.SetValue(user, skeletonData.EmoteConfig.allAvailableAtStartByDefault || emote.availableAtStart);

                ops.Add(animation.GetLoadEntity());
            }
            ops.Add(skeletonData.EmoteConfig.GetLoadEntity());
        }
        var transaction = new Transaction() { reliable = true };
        transaction.AddIfNotNull(ops);
        transaction.Dispatch();

        Debug.Log("sent emoteconfig");

        StartCoroutine(Binding(avatarModel, user));
    }

    IEnumerator Binding(UMI3DModel avatarModel, UMI3DTrackedUser user)
    {
        List<Operation> ops = new();

        yield return new WaitForSeconds(10);

        // TODO: Re-enable when bone API wil be working
        //if (bindRig)
        //{
        //    var bindings = binds.binds.Select(bind => new RigBoneBinding(avatarModel.Id(), bind.boneType, user.Id())
        //    {
        //        users = new() { user },
        //        rigName = bind.rigName,
        //        offsetRotation = Quaternion.Euler(bind.rotationOffset),
        //        offsetPosition = bind.positionOffset,
        //        syncPosition = bind.boneType.Equals(BoneType.CenterFeet) || bind.boneType.Equals(BoneType.Hips),
        //    }).Cast<AbstractSingleBinding>();

        //    MultiBinding multiBinding = new(avatarModel.Id())
        //    {
        //        partialFit = false,
        //        priority = 100,
        //        bindings = bindings.ToList()
        //    };

        //    //ops.AddRange(BindingHelper.Instance.AddBinding(multiBinding));
        //}
        //var transaction = new Transaction() { reliable = true };
        //transaction.AddIfNotNull(ops);
        //transaction.Dispatch();
        yield break;
    }

}