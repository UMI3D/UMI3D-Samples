using inetum.unityUtils;

using System.Collections.Generic;
using System.Linq;

using umi3d.edk;
using umi3d.edk.binding;

using UnityEngine;

public class BindingNodeRig : MonoBehaviour
{
    public UMI3DModel model;

    [SerializeField]
    private Transform dancerRoot;

    private SkeletonStructure dancerStructure = new();

    private class SkeletonStructure
    {
        public Transform root;
        public List<(string rigName, UMI3DNode node)> nodes = new();
    }

    private bool isDancing = false;

    private MultiBinding danceBinding;

    private IBindingService bindingService;

    private void Start()
    {
        if (model == null)
            model = GetComponent<UMI3DModel>();

        dancerStructure.root = dancerRoot;
        AddNodeOnAllRigs(dancerRoot.transform, dancerStructure);
        danceBinding = new MultiBinding(model.Id())
        {
            bindings = dancerStructure.nodes.Select(kp => new RigNodeBinding(model.Id(), kp.rigName, kp.node.Id())
            {
                syncPosition = true,
                syncRotation = true
            }).Cast<AbstractSingleBinding>().ToList(),
        };

        bindingService = BindingManager.Instance;
    }

    private void AddNodeOnAllRigs(Transform t, SkeletonStructure structure)
    {
        // creates nodes on each transform of the hierarchy on the server
        var node = t.gameObject.GetOrAddComponent<UMI3DNode>();
        structure.nodes.Add((rigName: t.name, node));
        foreach (Transform child in t)
        {
            if (child != t)
                AddNodeOnAllRigs(child, structure);
        }
    }

    public void Trigger()
    {
        if (!isDancing)
        {
            Transaction t = new() { reliable = true };
            t.AddIfNotNull(bindingService.AddBinding(danceBinding));
            t.Dispatch();
            isDancing = true;
        }
    }
}