using System.Collections.Generic;
using System.Linq;

using umi3d.edk;
using umi3d.edk.collaboration;
using umi3d.edk.collaboration.emotes;
using umi3d.edk.userCapture.animation;

using UnityEngine;

[RequireComponent(typeof(AvatarManager))]
public class EmoteManager : MonoBehaviour
{
    [SerializeField, Tooltip("If true, emotes are sent to users when joining the environment.")]
    private bool shouldSendEmotes = true;

    [SerializeField, Tooltip("Emotes configuration file.")]
    private UMI3DEmotesConfig emoteConfig;

    [SerializeField, Tooltip("Emote animations ressource bundle.")]
    private UMI3DResource emoteBundleResource;

    [SerializeField, Tooltip("Name of the state in the animations ressource bundle, in the order of emotes.")]
    private List<string> animatorStateNames = new();

    [SerializeField, Tooltip("Place where to store emote animations.")]
    private UMI3DNode emotesNodeRoot;

    // Store nodes that contains our emote animations.
    private readonly Dictionary<UMI3DUser, UMI3DSkeletonAnimationNode> emoteAnimationNodes = new();

    private IUMI3DServer UMI3DServerService;
    private IEmoteDispatcher emoteDispatcher;

    private void Start()
    {
        UMI3DServerService = UMI3DServer.Instance;
        emoteDispatcher = EmoteDispatcher.Instance;

        UMI3DServerService.OnUserActive.AddListener(Handle);
        UMI3DServerService.OnUserLeave.AddListener(Unhandle);
        UMI3DServerService.OnUserMissing.AddListener(Unhandle);
    }

    private void Handle(UMI3DUser user)
    {
        if (!shouldSendEmotes || emoteAnimationNodes.ContainsKey(user) || user is not UMI3DCollaborationUser cUser)
            return;

        Transaction t = new(true);
        t.AddIfNotNull(LoadEmotes(cUser));
        t.Dispatch();
    }

    private void Unhandle(UMI3DUser user)
    {
        if (!shouldSendEmotes || !emoteAnimationNodes.ContainsKey(user) || user is not UMI3DCollaborationUser cUser)
            return;

        Transaction t = new(true);
        t.AddIfNotNull(CleanEmotes(cUser));
        t.Dispatch();
    }


    public IEnumerable<Operation> LoadEmotes(UMI3DCollaborationUser user)
    {
        // Associate animation with emotes
        emoteDispatcher.EmotesConfigs.Add(user.Id(), emoteConfig);

        List<Operation> ops = new();
        if (emotesNodeRoot == null)
        {
            emotesNodeRoot = CreateEmoteAnimationsRootNode();
            ops.Add(emotesNodeRoot.GetLoadEntity());
        }

        ops.AddRange(CreateSkeletonAnimationNode(user));
        ops.AddRange(SetupEmotes(user));

        return ops;
    }

    /// <summary>
    /// Create the node under which all skeleton animations for emotes are put.
    /// </summary>
    /// <returns></returns>
    private UMI3DNode CreateEmoteAnimationsRootNode()
    {
        UMI3DScene scene = UMI3DEnvironment.Instance.scenes.Find(x => x.name.Contains("Avatar"));
        if (scene == null)
            scene = UMI3DEnvironment.Instance.scenes[0];
        UMI3DNode emoteRootNode = new GameObject("Emotes Subskeletons Animations").AddComponent<UMI3DNode>();
        emoteRootNode.nodeName = emoteRootNode.name;
        emoteRootNode.transform.SetParent(scene.transform);
        return emoteRootNode;
    }

    public IEnumerable<Operation> CleanEmotes(UMI3DCollaborationUser user)
    {
        List<Operation> ops = new();

        ops.AddRange(emoteAnimationNodes[user].GetDeleteAnimations());
        ops.Add(emoteAnimationNodes[user].GetDeleteEntity());

        UnityEngine.Object.Destroy(emoteAnimationNodes[user].gameObject);

        emoteAnimationNodes.Remove(user);
        emoteDispatcher.EmotesConfigs.Remove(user.Id());

        return ops;
    }

    private IEnumerable<Operation> CreateSkeletonAnimationNode(UMI3DCollaborationUser user)
    {
        GameObject subskeletonGo = new($"Emote animation subskeleton - user {user.Id()}");
        subskeletonGo.transform.SetParent(emotesNodeRoot.transform);
        subskeletonGo.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        // Create skeleton node for animations
        UMI3DSkeletonAnimationNode skeletonNode = subskeletonGo.AddComponent<UMI3DSkeletonAnimationNode>();

        // Create skeleton node as a UMI3D component
        skeletonNode.objectModel.SetValue(emoteBundleResource);
        skeletonNode.userId = user.Id();
        skeletonNode.priority = 100; // priority over 100 overrides other default skeleton animations and poses
        var stateNames = animatorStateNames.Take(emoteConfig.IncludedEmotes.Count).ToList(); // Only take the animator states names up till what has been defined in the emoteConfig

        // Create animations
        List<Operation> ops = new();
        ops.AddRange(skeletonNode.GenerateAnimations(stateNames: stateNames, arePlaying: false));
        ops.Add(skeletonNode.GetLoadEntity());

        emoteAnimationNodes.Add(user, skeletonNode);

        return ops;
    }

    private IEnumerable<Operation> SetupEmotes(UMI3DCollaborationUser user)
    {
        List<Operation> ops = new();

        var skeletonNode = emoteAnimationNodes[user];

        int indexAnim = 0;
        foreach (var emote in emoteConfig.IncludedEmotes)
        {
            // get the animations created in CreateSkeletonAnimationNode()
            UMI3DAbstractAnimation animation = UMI3DEnvironment.Instance._GetEntityInstance<UMI3DAbstractAnimation>(skeletonNode.relatedAnimationIds[indexAnim++]);

            emote.AnimationId.SetValue(user, animation.Id());
            emote.Available.SetValue(user, emoteConfig.allAvailableAtStartByDefault || emote.availableAtStart);

            // If no label is defined, emote label is the animation label
            if (string.IsNullOrEmpty(emote.label) && animation is UMI3DAnimatorAnimation animatorAnimation)
                emote.label = animatorAnimation.objectStateName.GetValue();
        }
        // only the new user receive a copy of its emote config
        ops.Add(emoteConfig.GetLoadEntity(new() { user }));

        return ops;
    }
}