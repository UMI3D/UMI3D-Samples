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
using System.Linq;

using umi3d.common.userCapture.animation;
using umi3d.edk;
using umi3d.edk.collaboration;
using umi3d.edk.userCapture.animation;

using UnityEngine;

public class WalkingAnimationsManager : MonoBehaviour
{
    [Header("Movement animation")]
    [SerializeField, EditorReadOnly, Tooltip("If true, user's avatar will be animated on movement.")]
    private bool shouldSendWalkingAnimator;

    [SerializeField, EditorReadOnly, Tooltip("Resource for the animation bundle.")]
    public UMI3DResource animatedSubskeletonBundleResource;

    [SerializeField, Tooltip("Place where to store skeleton animations.")]
    private UMI3DNode animationsRootNode;

    private readonly Dictionary<UMI3DUser, UMI3DSkeletonAnimationNode> skeletonAnimationNodes = new();

    private IUMI3DServer UMI3DServerService;

    private void Start()
    {
        UMI3DServerService = UMI3DServer.Instance;

        UMI3DServerService.OnUserActive.AddListener((user) => Handle(user as UMI3DCollaborationUser));
        UMI3DServerService.OnUserLeave.AddListener((user) => Unhandle(user as UMI3DCollaborationUser));
        UMI3DServerService.OnUserMissing.AddListener((user) => Unhandle(user as UMI3DCollaborationUser));
    }

    private void Handle(UMI3DCollaborationUser user)
    {
        if (!shouldSendWalkingAnimator || skeletonAnimationNodes.ContainsKey(user))
            return;

        Transaction t = new(true);

        t.AddIfNotNull(from nodeKVP in skeletonAnimationNodes
                        where nodeKVP.Key.Id() != user.Id()
                        let skeletonNode = nodeKVP.Value
                        let animations = skeletonNode.GetLoadAnimations(user)
                        from operation in animations
                        select operation);
        
        t.AddIfNotNull(LoadWalkingAnimations(user));
        t.Dispatch();
    }

    private void Unhandle(UMI3DCollaborationUser user)
    {
        if (!shouldSendWalkingAnimator || !skeletonAnimationNodes.ContainsKey(user))
            return;

        Transaction t = new(true);
        t.AddIfNotNull(Clean(user));
        t.Dispatch();
    }

    public IEnumerable<Operation> Clean(UMI3DCollaborationUser user)
    {
        List<Operation> ops = new();

        // walking animation
        ops.AddRange(skeletonAnimationNodes[user].GetDeleteAnimations());
        ops.Add(skeletonAnimationNodes[user].GetDeleteEntity());
        UnityEngine.Object.Destroy(skeletonAnimationNodes[user].gameObject);
        skeletonAnimationNodes.Remove(user);

        return ops;
    }

    private IEnumerable<Operation> LoadWalkingAnimations(UMI3DCollaborationUser user)
    {
        List<Operation> ops = new();

        // Create skeleton node for animations
        GameObject subskeletonNodeGo = new($"Movement animation subskeleton - user {user.Id()}");
        subskeletonNodeGo.transform.SetParent(animationsRootNode.transform);
        subskeletonNodeGo.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        UMI3DSkeletonAnimationNode skeletonNode = subskeletonNodeGo.AddComponent<UMI3DSkeletonAnimationNode>();
        skeletonNode.Register();
        
        skeletonNode.objectModel.SetValue(animatedSubskeletonBundleResource);
        skeletonNode.userId = user.Id();
        skeletonNode.priority = -10;
        skeletonNode.animationStates = new List<string>() { string.Empty };
        skeletonNode.animatorSelfTrackedParameters = new SkeletonAnimationParameter[] {
            new()
            {
                parameterName = SkeletonAnimatorParameterKeys.SPEED_Z.ToString(),
                parameterKey = (uint)SkeletonAnimatorParameterKeys.SPEED_Z,
                ranges = new()
            },
            new()
            {
                parameterName = SkeletonAnimatorParameterKeys.SPEED_X.ToString(),
                parameterKey = (uint)SkeletonAnimatorParameterKeys.SPEED_X,
                ranges = new List<SkeletonAnimationParameter.Range>()
                {
                    new () { startBound = -1f,   endBound = 1f,      result = 0f},
                }
            },
            new()
            {
                parameterName = SkeletonAnimatorParameterKeys.SPEED_X_Z.ToString(),
                parameterKey = (uint)SkeletonAnimatorParameterKeys.SPEED_X_Z,
                ranges = new()
            },
            new()
            {
                parameterName = SkeletonAnimatorParameterKeys.JUMP.ToString(),
                parameterKey = (uint)SkeletonAnimatorParameterKeys.JUMP,
                ranges = new()
            },
        };

        // Create Animator animations
        skeletonAnimationNodes.Add(user, skeletonNode);
        skeletonNode.GenerateAnimations(areLooping: true);

        // don't use walking animation for own user in VR, but others receive the animations
        var targetUsers = !user.HasHeadMountedDisplay ? null : UMI3DCollaborationServer.Instance.Users().Except(new UMI3DUser[1] { user })?.ToHashSet();
        ops.AddRange(skeletonNode.GetLoadAnimations(targetUsers));
        ops.Add(skeletonNode.GetLoadEntity(targetUsers));

        return ops;
    }
}