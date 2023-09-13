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
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer;
using inetum.unityUtils;
using inetum.unityUtils.editor;
using Newtonsoft.Json;
using NUnit;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using umi3d.common;
using umi3d.edk.save;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace umi3d.edk.editor
{
    public class SceneSaverWindow : InitedWindow<SceneSaverWindow>
    {
        #region Fields

        private const string fileName = "SceneSaverWindowData";
        private ScriptableLoader<SceneSaverWindowData> draw;
        private UnityEngine.GameObject[] gameobjects;

        #endregion

        #region Window

        protected override void Init()
        {
            draw = new ScriptableLoader<SceneSaverWindowData>(fileName);

            RefreshGameObjects();
        }

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
                Save();

                Debug.Log($"<color=#0000FF>Done Saving Environment</color>");
            }
        }

        #endregion

        #region Save

        public void Save()
        {
            RefreshGameObjects();

            SaveReference references = new SaveReference();
            UMI3DEnvironment environment = gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DEnvironment>()).FirstOrDefault(e => e != null);

            if (Directory.Exists(Application.dataPath + "/../mod"))
                Directory.Delete(Application.dataPath + "/../mod", true);
            Directory.CreateDirectory(Application.dataPath + "/../mod");

            SaveBase();

            SaveContents(references, environment);

            SaveData();

            if (!Directory.Exists(Application.dataPath + "/../mods"))
                Directory.CreateDirectory(Application.dataPath + "/../mods");
            Zip(new DirectoryInfo(Application.dataPath + "/../mod"), Application.dataPath + "/../mods/" + (draw.data.id + "-" + draw.data.version).Replace('.', '_') + ".umi3d");

            Directory.Delete(Application.dataPath + "/../mod", true);
        }

        public void SaveBase()
        {
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

            UMI3DServer server = gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DServer>()).FirstOrDefault(s => s != null);
            if (server != null)
            {
                using (StreamWriter sw = File.CreateText(Application.dataPath + "/../mod/networking.json"))
                {
                    sw.Write(new UMI3DNetworking()
                    {
                        serverDomain = UMI3DServer.GetHttpUrl(),
                        httpPort = 50043,//server.httpPort,
                        udpPort = -1,
                        natPort = -1,//server.forgeNatServerPort,
                        natDomain = "",//server.forgeNatServerHost,
                        masterPort = -1,//server.forgeMasterServerPort,
                        worldControllerUrl = ""
                    }.ToJson());
                    sw.Dispose();
                }
            }
        }

        public void SaveData()
        {
            DirectoryInfo source = new DirectoryInfo(Application.dataPath + "/../data");
            DirectoryInfo target = Directory.CreateDirectory(Application.dataPath + "/../mod/data");
            CopyFilesRecursively(source, target);
        }

            #region Contents

        public void SaveContents(SaveReference references, UMI3DEnvironment environment)
        {
            Directory.CreateDirectory(Application.dataPath + "/../mod/contents");

            SaveContentsJsons(references, environment);

            SaveContentsDlls();
        }

        public void SaveContentsJsons(SaveReference references, UMI3DEnvironment environment)
        {
            Directory.CreateDirectory(Application.dataPath + "/../mod/contents/jsons");

            if (environment != null)
            {
                string json = SceneSaver.SaveEnvironment(environment, gameobjects.ToList(), references);

                using (StreamWriter sw = File.CreateText(Application.dataPath + "/../mod/contents/jsons/scene.json"))
                {
                    sw.Write(json);
                    sw.Dispose();
                }
            }
        }

        public void SaveContentsDlls()
        {
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
        }

            #endregion

        #endregion

        #region Utility

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));

            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(System.IO.Path.Combine(target.FullName, file.Name));
        }

        protected void RefreshGameObjects()
        {
            gameobjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        }

        protected void Zip(DirectoryInfo source, string zipPath)
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            using (var fs = new FileStream(zipPath, FileMode.Create))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                byte[] bytes = new byte[10240];
                int numbytes;

                foreach (FileInfo file in source.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    string path = file.FullName.Remove(0, source.FullName.Length+1);

                    using (FileStream fileStream = File.OpenRead(file.FullName))
                    using (Stream zipEntryStream = zip.CreateEntry(path).Open())
                    {
                        while ((numbytes = fileStream.Read(bytes, 0, 10240)) > 0)
                        {
                            zipEntryStream.Write(bytes, 0, numbytes);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
#endif
