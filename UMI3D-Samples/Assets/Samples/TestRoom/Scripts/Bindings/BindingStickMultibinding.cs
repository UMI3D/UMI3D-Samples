using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using umi3d.edk;

using UnityEngine;

public class BindingStickMultibinding : MonoBehaviour
{
    [SerializeField]
    UMI3DModel StickLeft;

    [SerializeField]
    UMI3DModel StickRight;

    [SerializeField]
    UMI3DModel StickMiddle;

    [SerializeField]
    MaterialSO mixMaterial;

    private MaterialSO baseMaterial;

    private AbstractSingleBinding bindingLeft;
    private AbstractSingleBinding bindingRight;

    private bool boundLeft;
    private bool boundRight;

    private void Start()
    {
        baseMaterial = StickMiddle.objectMaterialOverriders.GetValue(0).newMaterial;
    }

    public void TriggerBindLeftStick()
    {
        Transaction t = new() { reliable = true };
        if (!boundLeft)
        {
            bindingLeft = new NodeBinding(StickMiddle.Id(), StickLeft.Id())
            {
                syncPosition = true,
                offsetPosition = StickMiddle.objectPosition.GetValue() - StickLeft.objectPosition.GetValue(),
            };

            if (!boundRight)
            {
                t.AddIfNotNull(BindingHelper.Instance.AddBinding(bindingLeft));
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { addMaterialIfNotExists = true, newMaterial = StickLeft.objectMaterialOverriders[0].newMaterial }));
            }
            else
            {
                t.AddIfNotNull(BindingHelper.Instance.RemoveAllBindings(StickMiddle.Id()));

                var multi = new MultiBinding(StickMiddle.Id())
                {
                    bindings = new() { bindingLeft, bindingRight }
                };

                t.AddIfNotNull(BindingHelper.Instance.AddBinding(multi));
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { addMaterialIfNotExists = true, newMaterial = mixMaterial }));
            }
                
            boundLeft = true;
        }
        else
        {
            t.AddIfNotNull(BindingHelper.Instance.RemoveAllBindings(StickMiddle.Id()));
            if (!boundRight)
            {
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { addMaterialIfNotExists = true, newMaterial = baseMaterial }));
            }
            else
            {
                t.AddIfNotNull(BindingHelper.Instance.AddBinding(bindingRight));
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { addMaterialIfNotExists = true, newMaterial = StickRight.objectMaterialOverriders[0].newMaterial }));
            }
            
            boundLeft = false;
        }
        t.Dispatch();
    }


    public void TriggerBindRighttick()
    {
        Transaction t = new() { reliable = true };
        if (!boundRight)
        {
            bindingRight = new NodeBinding(StickMiddle.Id(), StickRight.Id())
            {
                syncPosition = true,
                offsetPosition = StickMiddle.objectPosition.GetValue() - StickRight.objectPosition.GetValue(),
            };

            if (!boundLeft)
            {
                t.AddIfNotNull(BindingHelper.Instance.AddBinding(bindingRight));
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { addMaterialIfNotExists = true, newMaterial = StickRight.objectMaterialOverriders[0].newMaterial }));
            }
            else
            {
                t.AddIfNotNull(BindingHelper.Instance.RemoveAllBindings(StickMiddle.Id()));

                var multi = new MultiBinding(StickMiddle.Id())
                {
                    bindings = new() { bindingLeft, bindingRight }
                };

                t.AddIfNotNull(BindingHelper.Instance.AddBinding(multi));
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { addMaterialIfNotExists = true, newMaterial = mixMaterial }));
            }
                
            boundRight = true;
        }
        else
        {
            t.AddIfNotNull(BindingHelper.Instance.RemoveAllBindings(StickMiddle.Id()));
            if (!boundLeft)
            {
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { addMaterialIfNotExists = true, newMaterial = baseMaterial }));
            }
            else
            {
                t.AddIfNotNull(BindingHelper.Instance.AddBinding(bindingLeft));
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { addMaterialIfNotExists = true, newMaterial = StickLeft.objectMaterialOverriders[0].newMaterial }));
            }

            boundRight = false;
        }
        t.Dispatch();
    }
    
}
