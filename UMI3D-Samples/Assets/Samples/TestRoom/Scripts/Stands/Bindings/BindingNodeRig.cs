using inetum.unityUtils;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using umi3d.edk;
using umi3d.edk.binding;
using UnityEngine;

public class BindingNodeRig : MonoBehaviour
{
    public UMI3DModel model;
    public Transform transform;
    Dictionary<string,UMI3DNode> nodes;
    bool isBinding = false;
    MultiBinding binding;

    private void Start()
    {
        if(model == null)
            model = GetComponent<UMI3DModel>();
        if(transform == null)
            transform = GetComponent<Transform>();
        nodes = new();
        InitRigRec(transform);
    }

    void InitRigRec(Transform t)
    {
        var node = t.gameObject.GetOrAddComponent<UMI3DNode>();
        nodes[t.name] = node;
        foreach(Transform child in t)
        {
            if (child != t)
                InitRigRec(child);
        }
    }

    

    public void Trigger()
    {
        Transaction t = new() { reliable = true };
        isBinding = !isBinding;
        if (isBinding)
        {
            binding = new MultiBinding(model.Id())
            {
                bindings = nodes.Select(kp => new RigNodeBinding(model.Id(), kp.Key, kp.Value.Id())
                {
                    syncPosition = true,
                    syncRotation = true,
                    offsetRotation = Quaternion.Euler(new Vector3(0,-90,0))
                    
                } ).Cast<AbstractSingleBinding>().ToList(),
                
            };
            t.AddIfNotNull(BindingManager.Instance.AddBinding(binding));
        }
        else if(binding != null)
        {
            t.AddIfNotNull(BindingManager.Instance.RemoveBinding(binding));
        }
        t.Dispatch();
    }
}