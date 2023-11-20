using inetum.unityUtils;

using System.Linq;

using umi3d.common.userCapture.pose;
using umi3d.edk;
using umi3d.edk.interaction;
using umi3d.edk.userCapture.pose;

using UnityEngine;

public class PoseOnTrigger : MonoBehaviour
{
    private UMI3DPoseAnimator poseAnimator;
    private UMI3DEvent umi3dEvent;
    private UMI3DEnvironmentPoseCondition poseCondition;

    private void Start()
    {
        poseAnimator = gameObject.GetOrAddComponent<UMI3DPoseAnimator>();
        umi3dEvent = gameObject.GetOrAddComponent<UMI3DEvent>();

        poseCondition = new UMI3DEnvironmentPoseCondition();
        poseAnimator.environmentPoseConditions.Add(poseCondition);

        poseAnimator.duration = new()
        {
            duration = 1,
            hasMax = true,
            max = 1,
            hasMin = true,
            min = 1,
        };

        umi3dEvent.onTrigger.AddListener((content) => RequestPoseApplication(content.user));
    }

    private void RequestPoseApplication(UMI3DUser user)
    {
        TryActivatePoseAnimatorRequest activatePoseAnimatorRequest = new(poseAnimator.Id()) { users = new() { user } };

        Transaction t = new(true);
        t.AddIfNotNull(poseCondition.Validate(user));
        t.AddIfNotNull(activatePoseAnimatorRequest);
        t.Dispatch();
    }
}