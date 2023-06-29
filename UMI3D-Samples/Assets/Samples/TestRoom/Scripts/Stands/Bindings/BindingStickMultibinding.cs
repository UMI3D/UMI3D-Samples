using umi3d.edk;
using umi3d.edk.binding;

using UnityEngine;

public class BindingStickMultibinding : MonoBehaviour
{
    [SerializeField]
    private UMI3DModel StickLeft;

    [SerializeField]
    private UMI3DModel StickRight;

    [SerializeField]
    private UMI3DModel StickMiddle;

    [SerializeField]
    private MaterialSO mixMaterial;

    private MaterialSO baseMaterial;

    private AbstractSingleBinding bindingLeft;
    private AbstractSingleBinding bindingRight;

    private bool boundLeft;
    private bool boundRight;

    private IBindingService bindingHelperServer;

    private void Start()
    {
        bindingHelperServer = BindingManager.Instance;
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

            t.AddIfNotNull(bindingHelperServer.AddBinding(bindingLeft));
            boundLeft = true;

            if (!boundRight)
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { overrideAllMaterial = true, newMaterial = StickLeft.objectMaterialOverriders[0].newMaterial }));
            else
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { overrideAllMaterial = true, newMaterial = mixMaterial }));
        }
        else
        {
            t.AddIfNotNull(bindingHelperServer.RemoveBinding(bindingLeft));
            boundLeft = false;

            if (!boundRight)
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { overrideAllMaterial = true, newMaterial = baseMaterial }));
            else
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { overrideAllMaterial = true, newMaterial = StickRight.objectMaterialOverriders[0].newMaterial }));
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
                offsetPosition = StickMiddle.objectPosition.GetValue() - StickRight.objectPosition.GetValue()
            };

            t.AddIfNotNull(bindingHelperServer.AddBinding(bindingRight));
            boundRight = true;

            if (!boundLeft)
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { overrideAllMaterial = true, newMaterial = StickRight.objectMaterialOverriders[0].newMaterial }));
            else
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { overrideAllMaterial = true, newMaterial = mixMaterial }));
        }
        else
        {
            t.AddIfNotNull(bindingHelperServer.RemoveBinding(bindingRight));
            boundRight = false;

            if (!boundLeft)
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { overrideAllMaterial = true, newMaterial = baseMaterial }));
            else
                t.AddIfNotNull(StickMiddle.objectMaterialOverriders.SetValue(0, new MaterialOverrider() { overrideAllMaterial = true, newMaterial = StickLeft.objectMaterialOverriders[0].newMaterial }));
        }
        t.Dispatch();
    }
}