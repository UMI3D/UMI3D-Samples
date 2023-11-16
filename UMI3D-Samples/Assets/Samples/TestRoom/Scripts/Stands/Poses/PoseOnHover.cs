using inetum.unityUtils;

using System.Linq;

using umi3d.common.userCapture.pose;
using umi3d.edk;
using umi3d.edk.interaction;
using umi3d.edk.userCapture.pose;

using UnityEngine;

public class PoseOnHover : MonoBehaviour
{
    private UMI3DPoseAnimator poseAnimator;
    private UMI3DInteractable interactable;
    private UMI3DEnvironmentPoseCondition poseCondition;

    private void Start()
    {
        poseAnimator = gameObject.GetOrAddComponent<UMI3DPoseAnimator>();
        interactable = gameObject.GetOrAddComponent<UMI3DInteractable>();

        poseCondition = new UMI3DEnvironmentPoseCondition();
        poseAnimator.ActivationsConditions = poseAnimator.ActivationsConditions.Append(poseCondition).ToList();

        interactable.onHoverEnter.AddListener((content) => RequestPoseApplication(content.user));
        interactable.onHoverExit.AddListener((content) => RequestPoseStop(content.user));
    }

    private void RequestPoseApplication(UMI3DUser user)
    {
        TryActivatePoseAnimatorRequest activatePoseAnimatorRequest = new(poseAnimator.Id()) { users = new() { user } };

        Transaction t = new(true);
        t.AddIfNotNull(poseCondition.Validate(user));
        t.AddIfNotNull(activatePoseAnimatorRequest);
        t.Dispatch();
    }

    private void RequestPoseStop(UMI3DUser user)
    {
        Transaction t = new(true);
        t.AddIfNotNull(poseCondition.Invalidate(user));
        t.Dispatch();
    }
}