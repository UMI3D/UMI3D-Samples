/*
Copyright 2019 - 2023 Inetum

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

#if UNITY_EDITOR
using Codice.CM.SEIDInfo;
using inetum.unityUtils;
using inetum.unityUtils.editor;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using umi3d.common;
using umi3d.edk.collaboration;
using umi3d.edk.save;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace umi3d.edk.editor
{
    public class SceneSaverWindow : InitedWindow<SceneSaverWindow>
    {
        private const string fileName = "SceneSaverWindowData";
        private ScriptableLoader<SceneSaverWindowData> draw;
        private UnityEngine.GameObject[] gameobjects;

        [MenuItem("UMI3D/Scene Save")]
        private static void Open()
        {
            OpenWindow();
        }

        protected override void Draw()
        {
            draw.editor.DrawDefaultInspector();

            if (GUILayout.Button("Save"))
            {
                RefreshGameObjects();

                SaveReference references = new SaveReference();
                UMI3DEnvironment env = gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DEnvironment>()).FirstOrDefault(e => e != null);

                if (Directory.Exists(Application.dataPath + "/../mod"))
                    Directory.Delete(Application.dataPath + "/../mod", true);
                Directory.CreateDirectory(Application.dataPath + "/../mod");

                using (StreamWriter sw = File.CreateText(Application.dataPath + "/../mod/info.json"))
                {
                    sw.Write(new UMI3DInfo()
                    {
                        id = draw.data.id,
                        name = draw.data.name,
                        version = draw.data.version,
                        umi3dVersion = UMI3DVersion.version,
                        type = ""
                    }.ToJson());
                    sw.Dispose();
                }

                UMI3DCollaborationServer server = gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DCollaborationServer>()).FirstOrDefault(s => s != null);
                if (server != null)
                {
                    using (StreamWriter sw = File.CreateText(Application.dataPath + "/../mod/networking.json"))
                    {
                        sw.Write(new UMI3DNetworking()
                        {
                            serverDomain = "",
                            httpPort = server.httpPort,
                            udpPort = -1,
                            natPort = server.forgeNatServerPort,
                            natDomain = server.forgeNatServerHost,
                            masterPort = server.forgeMasterServerPort,
                            worldControllerUrl = ""
                        }.ToJson());
                        sw.Dispose();
                    }
                }

                if (env != null)
                {
                    string json = SceneSaver.SaveEnvironment(env, gameobjects.ToList() ,references);

                    using (StreamWriter sw = File.CreateText(Application.dataPath + "/../mod/contents/jsons/scene.json"))
                    {
                        sw.Write(json);
                        sw.Dispose();
                    }
                }

                if (draw.data.assemblies != null && draw.data.assemblies.Count > 0)
                {
                    List<string> assembliesToBuild = draw.data.assemblies.Select(assemblyDefinition => assemblyDefinition.name).ToList();

                    Assembly[] playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
                    foreach (var assembly in playerAssemblies)
                    {
                        if (assembliesToBuild.Contains(assembly.name))
                        {
                            if (File.Exists(assembly.outputPath))
                            {
                                string filename = System.IO.Path.GetFileName(assembly.outputPath);
                                string outputfile = System.IO.Path.Combine(Application.dataPath + "/../mod/contents/dll/", filename);

                                if (!Directory.Exists(Application.dataPath + "/../mod/contents/dll"))
                                    Directory.CreateDirectory(Application.dataPath + "/../mod/contents/dll");

                                File.Copy(assembly.outputPath, outputfile, true);
                            }
                        }
                    }
                }

                Debug.Log($"<color=#0000FF>Done Saving Environment</color>");
            }

            if (GUILayout.Button("Load"))
            {
                RefreshGameObjects();

                if (Directory.Exists(Application.dataPath + "/../mod/contents/dll"))
                {
                    if (!Directory.Exists(Application.dataPath + "/Mods"))
                        Directory.CreateDirectory(Application.dataPath + "/Mods");

                    foreach (string dllFile in Directory.GetFiles(Application.dataPath + "/../mod/contents/dll"))
                    {
                        File.Copy(dllFile, Application.dataPath + "/Mods/" + System.IO.Path.GetFileName(dllFile), true);
                        System.Reflection.Assembly.LoadFile(Application.dataPath + "/Mods/" + System.IO.Path.GetFileName(dllFile));
                    }

                    AssetDatabase.Refresh();
                }
                
                if (File.Exists(Application.dataPath + "/../mod/contents/jsons/scene.json"))
                {
                    string json = File.ReadAllText(Application.dataPath + "/../mod/contents/jsons/scene.json");

                    SaveReference references = new SaveReference();
                    UMI3DDto dto = UMI3DDtoSerializer.FromJson(json);

                    if (dto is GlTFEnvironmentDto environmentDto)
                    {
                        GameObject env = gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DEnvironment>()).FirstOrDefault(e => e != null)?.gameObject ?? new GameObject("UMI3DEnvironment");
                        SceneSaver.LoadEnvironment(environmentDto, env, references);
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    }
                }
                
                Debug.Log($"<color=#0000FF>Done Loading Environment</color>");
            }
        }

        protected override void Init()
        {
            draw = new ScriptableLoader<SceneSaverWindowData>(fileName);

            RefreshGameObjects();
        }

        protected void RefreshGameObjects()
        {
            gameobjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        }
    }
}
#endif
