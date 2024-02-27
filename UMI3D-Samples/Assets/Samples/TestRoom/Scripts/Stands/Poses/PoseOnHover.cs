using inetum.unityUtils;

using System.Collections.Generic;

using umi3d.common.userCapture;
using umi3d.common.userCapture.pose;
using umi3d.edk;
using umi3d.edk.interaction;
using umi3d.edk.userCapture.pose;

using UnityEngine;

public class PoseOnHover : MonoBehaviour
{
    private UMI3DPoseAnimator poseAnimator;
    private UMI3DInteractable interactable;
    private UMI3DEnvironmentPoseCondition hoverPoseCondition;

    private UMI3DEnvironmentPoseCondition IsVRposeCondition;

    private void Start()
    {
        poseAnimator = gameObject.GetOrAddComponent<UMI3DPoseAnimator>();
        interactable = gameObject.GetOrAddComponent<UMI3DInteractable>();

        hoverPoseCondition = new UMI3DEnvironmentPoseCondition();

        IPoseAnimatorActivationCondition magnitudeConditionPC = new MagnitudeCondition()
        {
            RelativeNode = GetComponent<UMI3DNode>(),
            Distance = 1,
            Bone = BoneType.Hips
        };

        IPoseAnimatorActivationCondition magnitudeConditionVR = new MagnitudeCondition()
        {
            RelativeNode = GetComponent<UMI3DNode>(),
            Distance = 20,
            Bone = BoneType.Hips
        };

        IsVRposeCondition = new UMI3DEnvironmentPoseCondition(false);

        poseAnimator.ActivationConditions.SetValue(new List<IPoseAnimatorActivationCondition>()
        {
            hoverPoseCondition & ((IsVRposeCondition & magnitudeConditionVR) | (!(IsVRposeCondition as IPoseAnimatorActivationCondition) & magnitudeConditionPC))
        });

        interactable.onHoverEnter.AddListener((content) => RequestPoseApplication(content.user));
        interactable.onHoverExit.AddListener((content) => RequestPoseStop(content.user));

        UMI3DServer.Instance.OnUserActive.AddListener((user) =>
        {
            Transaction t = new(true);
            if (user.HasHeadMountedDisplay)
                t.AddIfNotNull(IsVRposeCondition.Validate(user));
            if (!user.HasHeadMountedDisplay)
                t.AddIfNotNull(IsVRposeCondition.Invalidate(user));
            t.Dispatch();
        });
    }

    private void RequestPoseApplication(UMI3DUser user)
    {
        CheckPoseAnimatorConditionsRequest activatePoseAnimatorRequest = new(poseAnimator.Id()) { users = new() { user } };

        Transaction t = new(true);
        t.AddIfNotNull(hoverPoseCondition.Validate(user));
        t.AddIfNotNull(activatePoseAnimatorRequest);
        t.Dispatch();
    }

    private void RequestPoseStop(UMI3DUser user)
    {
        Transaction t = new(true);
        t.AddIfNotNull(hoverPoseCondition.Invalidate(user));
        t.Dispatch();
    }
}