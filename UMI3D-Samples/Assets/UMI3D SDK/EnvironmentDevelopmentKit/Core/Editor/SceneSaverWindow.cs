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
using inetum.unityUtils;
using inetum.unityUtils.editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using umi3d.common;
using UnityEditor;
using UnityEngine;

namespace umi3d.edk.editor
{
    public class SceneSaverWindow : InitedWindow<SceneSaverWindow>
    {
        const string fileName = "SceneSaverWindowData";
        ScriptableLoader<SceneSaverWindowData> draw;

        UnityEngine.GameObject[] gameobjects;

        [MenuItem("UMI3D/Scene Save")]
        static void Open()
        {
            OpenWindow();
        }

        protected override void Draw()
        {
            if (GUILayout.Button("Load TMP"))
            {
                var dto = UMI3DDtoSerializer.FromJson(draw.data.tmp);
                if(dto is GlTFEnvironmentDto environmentDto)
                {
                    var env = gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DEnvironment>()).FirstOrDefault(e => e != null)?.gameObject ?? new GameObject("UMI3DEnvironment");
                    if (environmentDto.extensions is ComponentExtensionSO ce)
                    {
                        ComponentExtensionSOLoader.LoadOrUpdate(env, ce);
                    }
                    else
                    {
                        env.GetOrAddComponent<UMI3DEnvironment>();
                    }
                }
            }


            if (GUILayout.Button("Save environment"))
            {
                var env = gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DEnvironment>()).FirstOrDefault(e => e != null);
                if (env != null)
                {
                    GlTFEnvironmentDto glTFEnvironmentDto = new GlTFEnvironmentDto()
                    {
                        extensions = (env as ISavable).Save()
                    };
                    foreach (var obj in gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DScene>()))
                    {
                        var dto = new GlTFSceneDto
                        {
                            name = obj.name,
                            extensions = (obj as ISavable)?.Save()
                        };
                        glTFEnvironmentDto.scenes.Add(dto);
                    }

                    draw.data.tmp = glTFEnvironmentDto.ToJson();
                }
            }


            foreach (var obj in gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DScene>()))
            {
                if (GUILayout.Button(obj.name))
                {
                    draw.data.tmp = (obj as ISavable).Save().ToJson();
                }
            }

            draw.editor.DrawDefaultInspector();
        }

        protected override void Init()
        {
            gameobjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            draw = new ScriptableLoader<SceneSaverWindowData>(fileName);
        }
    }
}
#endif
