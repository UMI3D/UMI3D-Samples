using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using umi3d.common;
using umi3d.edk;
using UnityEditor.SceneManagement;
using UnityEditor;
using System.IO.Compression;
using Codice.Client.BaseCommands.WkStatus.Printers;
using System.Linq;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(Umi3dImporter))]
public class Umi3dImporterEditor : ScriptedImporterEditor
{
    public override void OnEnable()
    {
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        // Update the serializedObject in case it has been changed outside the Inspector.
        serializedObject.Update();

        if (GUILayout.Button("Load to Current Scene"))
        {
            Debug.Log(serializedObject.FindProperty("scene").stringValue.Length);
            SaveReference references = new SaveReference();
            UMI3DDto dto = UMI3DDtoSerializer.FromJson(serializedObject.FindProperty("scene").stringValue);

            if (dto is GlTFEnvironmentDto environmentDto)
            {
                GameObject[] gameobjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                GameObject env = gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DEnvironment>()).FirstOrDefault(e => e != null)?.gameObject ?? new GameObject("UMI3DEnvironment");
                SceneSaver.LoadEnvironment(environmentDto, env, references);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        // Apply the changes so Undo/Redo is working
        serializedObject.ApplyModifiedProperties();

        // Call ApplyRevertGUI to show Apply and Revert buttons.
        ApplyRevertGUI();
    }
}