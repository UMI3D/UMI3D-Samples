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
using NUnit.Framework;
using System;
using System.Linq;
using umi3d.common;
using umi3d.edk.save;
using UnityEditor;
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
            if (GUILayout.Button("Test"))
            {
                UMI3DSceneLoaderModuleUtils.GetModulesType().Debug();
            }

            if (GUILayout.Button("Save environment"))
            {
                SaveReference references = new SaveReference();
                UMI3DEnvironment env = gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DEnvironment>()).FirstOrDefault(e => e != null);
                if (env != null)
                {
                    draw.data.tmp = SceneSaver.SaveEnvironment(env, gameobjects.ToList() ,references);
                }
            }

            if (GUILayout.Button("Load TMP"))
            {
                SaveReference references = new SaveReference();
                UMI3DDto dto = UMI3DDtoSerializer.FromJson(draw.data.tmp);
                if (dto is GlTFEnvironmentDto environmentDto)
                {
                    GameObject env = gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DEnvironment>()).FirstOrDefault(e => e != null)?.gameObject ?? new GameObject("UMI3DEnvironment");
                    SceneSaver.LoadEnvironment(environmentDto,env, references);
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }

            draw.editor.DrawDefaultInspector();
        }

        protected override void Init()
        {
            gameobjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            draw = new ScriptableLoader<SceneSaverWindowData>(fileName);
        }


        class A { }
        class B : A ,IB1,IB2 { }
        class C : B{ }
        class D : C { }

        interface IB1 { }
        interface IB2 { }
    }


}
#endif
