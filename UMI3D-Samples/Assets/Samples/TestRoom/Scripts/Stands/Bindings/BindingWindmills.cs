using System.Collections.Generic;
using System.Linq;
using umi3d.edk;
using umi3d.edk.binding;
using umi3d.edk.userCapture;
using UnityEngine;

public class BindingWindmills : MonoBehaviour
{
    public UMI3DNode mainWindmill;

    public List<UMI3DNode> boundNodes;

    private bool arebound;

    private IBindingService bindingHelperServer;
    private UMI3DServer umi3dServerService;

    private void Start()
    {
        bindingHelperServer = BindingManager.Instance;
        umi3dServerService = UMI3DServer.Instance;
    }

    public void TriggerBindWindmills()
    {
        if (!arebound)
        {
            StartBindWindmills();
            arebound = true;
        }
        else
        {
            StopBindWindmills();
            arebound = false;
        }
    }

    private void StartBindWindmills()
    {
        Transaction t = new()
        {
            reliable = true
        };

        foreach (var node in boundNodes)
        {
            NodeBinding binding = new(node.Id(), mainWindmill.Id())
            {
                syncRotation = true,
                offsetRotation = Quaternion.Inverse(mainWindmill.transform.rotation) * node.objectRotation.GetValue()
            };

            var op = bindingHelperServer.AddBinding(binding);
            t.AddIfNotNull(op);
        }

        t.Dispatch();
    }

    private void StopBindWindmills()
    {
        Transaction t = new()
        {
            reliable = true
        };

        foreach (var node in boundNodes)
        {
            var nodeBinding = bindingHelperServer.GetBindings(umi3dServerService.Users().First())[node.Id()] as NodeBinding;

            var op = bindingHelperServer.RemoveAllBindings(node.Id());
            t.AddIfNotNull(op);
            t.AddIfNotNull(node.objectRotation.SetValue(mainWindmill.transform.rotation * nodeBinding.offsetRotation));
        }

        t.Dispatch();
    }
}