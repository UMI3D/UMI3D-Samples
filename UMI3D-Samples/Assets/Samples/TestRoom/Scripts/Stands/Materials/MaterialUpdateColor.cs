/*
Copyright 2019 - 2021 Inetum

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
using System;
using System.Collections;
using System.Collections.Generic;
using umi3d.edk;
using UnityEngine;

public class MaterialUpdateColor : MonoBehaviour
{
    /// <summary>
    /// Color used to overide materials
    /// </summary>
    public Color color = Color.white;

    /// <summary>
    /// Model to display the current random color
    /// </summary>
    public UMI3DModel currentColorDisplayer;

    /// <summary>
    /// plot prefab to test override on specific material
    /// </summary>
    public UMI3DModel plot;

    public MaterialSO matToAdd;

    private PBRMaterial pBRMaterial;
    private Color originalColor = new Color();
    private MaterialOverrider materialOverrider = new MaterialOverrider();

    // Start is called before the first frame update
    void Start()
    {
        materialOverrider.overrideAllMaterial = true;
        pBRMaterial = ScriptableObject.CreateInstance<PBRMaterial>();
        pBRMaterial.baseColorFactor = color;
        materialOverrider.newMaterial = pBRMaterial;
        materialOverrider.newMaterial.name = "new random material";

        Transaction transaction = new Transaction();
        // add Materials send to client and and in preloaded for futur client
        transaction.AddIfNotNull(pBRMaterial.GetLoadEntity());
        transaction.AddIfNotNull(matToAdd.GetLoadEntity());
        UMI3DEnvironment.Instance.GetComponentInChildren<UMI3DScene>().PreloadedMaterials.Add(pBRMaterial);
        UMI3DEnvironment.Instance.GetComponentInChildren<UMI3DScene>().PreloadedMaterials.Add(matToAdd);

        // add overider 
        transaction.AddIfNotNull(currentColorDisplayer.objectMaterialsOverrided.SetValue(true));
        transaction.AddIfNotNull(currentColorDisplayer.objectMaterialOverriders.SetValue(new List<MaterialOverrider>() { materialOverrider }));
        transaction.Dispatch();
    }


    public void RandomColor()
    {
        color = UnityEngine.Random.ColorHSV();
        color.a = 1f;
        Transaction transaction = new Transaction();
        transaction.AddIfNotNull(pBRMaterial.objectBaseColorFactor.SetValue(color));

        transaction.Dispatch();
    }

    public void ChangeColorOnMat(PBRMaterial mat)
    {
        if (! materialsToReset.ConvertAll<PBRMaterial>( (t) => t.Item1).Contains(mat))
            materialsToReset.Add(new Tuple<PBRMaterial, Color>(mat, mat.objectBaseColorFactor.GetValue()));
        Transaction transaction = new Transaction();
        transaction.AddIfNotNull(mat.objectBaseColorFactor.SetValue(color));
        transaction.Dispatch();
    }

    //Change color property
    public void ChangeOneMaterial(string materialName)
    {
        Transaction transaction = new Transaction();
        transaction.AddIfNotNull(plot.objectMaterialsOverrided.SetValue(true));
        var mat = ScriptableObject.CreateInstance<PBRMaterial>();
        mat.alphaMode = MaterialSO.AlphaMode.OPAQUE;
        mat.baseColorFactor = color;
        // add mat on resources 
        transaction.AddIfNotNull(mat.GetLoadEntity());
        UMI3DEnvironment.Instance.GetComponentInChildren<UMI3DScene>().PreloadedMaterials.Add(mat);
        var overrider = new MaterialOverrider()
        {
            addMaterialIfNotExists = false,
            overrideAllMaterial = false,
            newMaterial = mat,
            overidedMaterials = new List<string>() { materialName}
        };
        transaction.AddIfNotNull(plot.objectMaterialOverriders.Add(overrider));
        transaction.Dispatch();
    }

    public void ChangeAllMaterial()
    {
        Transaction transaction = new Transaction();
        transaction.AddIfNotNull(plot.objectMaterialsOverrided.SetValue(true));
        var mat = ScriptableObject.CreateInstance<PBRMaterial>();
        mat.alphaMode = MaterialSO.AlphaMode.OPAQUE;
        mat.baseColorFactor = color;
        // add mat on resources 
        transaction.AddIfNotNull(mat.GetLoadEntity());
        UMI3DEnvironment.Instance.GetComponentInChildren<UMI3DScene>().PreloadedMaterials.Add(mat);
        var overrider = new MaterialOverrider()
        {
            addMaterialIfNotExists = false,
            overrideAllMaterial = true,
            newMaterial = mat,
        };
        transaction.AddIfNotNull(plot.objectMaterialOverriders.Add(overrider));
        transaction.Dispatch();
    }

    public void ClearMaterial()
    {
        Transaction transaction = new Transaction();
        transaction.AddIfNotNull(plot.objectMaterialOverriders.SetValue(new List<MaterialOverrider>() {}));
        transaction.AddIfNotNull(plot.objectMaterialsOverrided.SetValue(false));
        transaction.Dispatch();

    }

    //add and remove materials
    private bool isMaterialAdded = false;
    private MaterialOverrider currentAddedOverrider;
    public void AddMaterial(UMI3DModel model)
    {
        if (!isMaterialAdded)
        {
            Transaction transaction = new Transaction();
            transaction.AddIfNotNull(model.objectMaterialsOverrided.SetValue(true));
            currentAddedOverrider = new MaterialOverrider()
            {
                addMaterialIfNotExists = true,
                overrideAllMaterial = true,
                newMaterial = matToAdd,
            };
            transaction.AddIfNotNull(model.objectMaterialOverriders.Add(currentAddedOverrider));
            transaction.Dispatch();
            isMaterialAdded = true;
        }
    }


    private List<MaterialOverrider> overriderListAdditif = new();
    private PBRMaterial additivMat;
    private MaterialOverrider addMaterialOverrider;
    public void AddMultipleMaterial(UMI3DModel model)
    {
        Transaction transaction = new Transaction();
        modelsToReset.Add(model);
        if (additivMat == null)
        {
            additivMat = ScriptableObject.CreateInstance<PBRMaterial>();

            /* other way to do the same thing 
            toggleEmissiveMat.shaderProperties.Add("_EMISSION", true);
            toggleEmissiveMat.shaderProperties.Add("_EmissionColor", ToUMI3DSerializable.ToSerializableColor(UnityEngine.Random.ColorHSV(), null));
            */
            additivMat.objectBaseColorFactor.SetValue(new Color (0,0,1,0.1f));
            additivMat.alphaMode = MaterialSO.AlphaMode.BLEND;

            transaction.AddIfNotNull(additivMat.GetLoadEntity());
            UMI3DEnvironment.Instance.GetComponentInChildren<UMI3DScene>().PreloadedMaterials.Add(additivMat);
            addMaterialOverrider = new MaterialOverrider()
            {
                overrideAllMaterial = false,
                newMaterial = additivMat,
                overidedMaterials = new() { "AddMat" },
                addMaterialIfNotExists = true
            };
            transaction.AddIfNotNull(model.objectMaterialsOverrided.SetValue(true));
            overriderListAdditif.Add(addMaterialOverrider);
            transaction.AddIfNotNull(model.objectMaterialOverriders.SetValue(overriderListAdditif));
        }
        else
        {
            transaction.AddIfNotNull(model.objectMaterialsOverrided.SetValue(true)); // needed after reset 
            transaction.AddIfNotNull(model.objectMaterialOverriders.Add(addMaterialOverrider));

        }
        transaction.Dispatch();

    }
    public void RemoveMaterial(UMI3DModel model)
    {
        if (isMaterialAdded)
        {
            Transaction transaction = new Transaction();
            transaction.AddIfNotNull(model.objectMaterialOverriders.Remove(currentAddedOverrider));
            transaction.Dispatch();
            isMaterialAdded = false;
        }
    }

    //Shader properties by dictionary
    private OriginalMaterial originalMatForEnvColoring;
    private List<MaterialOverrider> overiderList = new();
    public void ActiveEnvColoringProperty(UMI3DModel model)
    {
        Transaction transaction = new Transaction();
        modelsToReset.Add(model);
        if(originalMatForEnvColoring == null)
        {
            originalMatForEnvColoring = ScriptableObject.CreateInstance<OriginalMaterial>();
            originalMatForEnvColoring.shaderProperties.Add("_EMISSION", true);
            originalMatForEnvColoring.shaderProperties.Add("_EmissionColor",  ToUMI3DSerializable.ToSerializableColor(UnityEngine.Random.ColorHSV(), null));

            transaction.AddIfNotNull(originalMatForEnvColoring.GetLoadEntity());
            UMI3DEnvironment.Instance.GetComponentInChildren<UMI3DScene>().PreloadedMaterials.Add(originalMatForEnvColoring);

            transaction.AddIfNotNull(model.objectMaterialsOverrided.SetValue(true));
            overiderList.Add(new MaterialOverrider() { overrideAllMaterial = true, newMaterial = originalMatForEnvColoring });
            transaction.AddIfNotNull(model.objectMaterialOverriders.SetValue(overiderList));
        }
        else
        {
            transaction.AddIfNotNull(model.objectMaterialsOverrided.SetValue(true)); // needed after reset 
            transaction.AddIfNotNull(model.objectMaterialOverriders.SetValue(overiderList));

            transaction.AddIfNotNull(originalMatForEnvColoring.objectShaderProperties.SetValue("_EmissionColor", ToUMI3DSerializable.ToSerializableColor(UnityEngine.Random.ColorHSV(),null)));
        }
        transaction.Dispatch();
    }


    private List<MaterialOverrider> overiderListForToggle = new();
    private PBRMaterial toggleEmissiveMat;
    public void ToggleEmissive(UMI3DModel model)
    {
        Transaction transaction = new Transaction();
        modelsToReset.Add(model);
        if (toggleEmissiveMat == null)
        {
            toggleEmissiveMat = ScriptableObject.CreateInstance<PBRMaterial>();

            /* other way to do the same thing 
            toggleEmissiveMat.shaderProperties.Add("_EMISSION", true);
            toggleEmissiveMat.shaderProperties.Add("_EmissionColor", ToUMI3DSerializable.ToSerializableColor(UnityEngine.Random.ColorHSV(), null));
            */
            toggleEmissiveMat.objectEmissiveFactor.SetValue(UnityEngine.Random.ColorHSV());

            transaction.AddIfNotNull(toggleEmissiveMat.GetLoadEntity());
            UMI3DEnvironment.Instance.GetComponentInChildren<UMI3DScene>().PreloadedMaterials.Add(toggleEmissiveMat);

            transaction.AddIfNotNull(model.objectMaterialsOverrided.SetValue(true));
            overiderListForToggle.Add(new MaterialOverrider() { overrideAllMaterial = true, newMaterial = toggleEmissiveMat });
            transaction.AddIfNotNull(model.objectMaterialOverriders.SetValue(overiderListForToggle));
        }
        else
        {
            transaction.AddIfNotNull(model.objectMaterialsOverrided.SetValue(true)); // needed after reset 
            transaction.AddIfNotNull(model.objectMaterialOverriders.SetValue(overiderListForToggle));

            //transaction.AddIfNotNull(toggleEmissiveMat.objectShaderProperties.SetValue("_EmissionColor", ToUMI3DSerializable.ToSerializableColor(UnityEngine.Random.ColorHSV(), null)));
            transaction.AddIfNotNull(toggleEmissiveMat.objectEmissiveFactor.SetValue(UnityEngine.Random.ColorHSV()));

        }
        transaction.Dispatch();
    }



    //Reset
    private List<Tuple<PBRMaterial, Color>> materialsToReset = new List<Tuple<PBRMaterial, Color>>();
    public List<AbstractRenderedNode> modelsToReset = new List<AbstractRenderedNode>();
    public void ResetMat()
    {
        Transaction transaction = new Transaction();
        foreach (AbstractRenderedNode item in modelsToReset)
        {
            transaction.AddIfNotNull(item.objectMaterialsOverrided.SetValue(false));
            transaction.AddIfNotNull(item.objectMaterialOverriders.SetValue(new List<MaterialOverrider>()));
        }
        foreach (Tuple<PBRMaterial, Color> item in materialsToReset)
        {
            transaction.AddIfNotNull(item.Item1.objectBaseColorFactor.SetValue(item.Item2));
        }

        color = Color.white;
        transaction.AddIfNotNull(currentColorDisplayer.objectMaterialsOverrided.SetValue(true));
        transaction.AddIfNotNull(pBRMaterial.objectBaseColorFactor.SetValue(color));
        transaction.AddIfNotNull(currentColorDisplayer.objectMaterialOverriders.SetValue(new List<MaterialOverrider>() { materialOverrider }));

        transaction.Dispatch();
    }

    //On destroy, reset existing pbr materials
    private void OnDestroy()
    {
        Transaction transaction = new Transaction();
        foreach (Tuple<PBRMaterial, Color> item in materialsToReset)
        {
            transaction.AddIfNotNull(item.Item1.objectBaseColorFactor.SetValue(item.Item2));
        }

    }

}
