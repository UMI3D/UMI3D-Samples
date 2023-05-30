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
                        extensions = (env as ISavable).Save(),
                        id = (ulong)SaveReference.GetId(env)

                    };
                    SaveScenes(glTFEnvironmentDto);

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

        void SaveScenes(GlTFEnvironmentDto glTFEnvironmentDto)
        {
            foreach (var obj in gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DScene>()))
            {
                var glTFSceneDto = new GlTFSceneDto
                {
                    name = obj.name,
                    extensions = (obj as ISavable)?.Save(),

                };

                SaveNodes(glTFSceneDto,obj.transform);


                glTFEnvironmentDto.scenes.Add(glTFSceneDto);
            }
        }
        List<int> SaveNodes(GlTFSceneDto glTFSceneDto, Transform node)
        {
            var l = new List<int>();
            foreach(Transform t in node)
            {
                if(t == node.transform || t.GetComponent<UMI3DScene>() != null)
                    continue;

                GlTFNodeDto glTFNodeDto = new()
                {
                    name = t.name,
                    position = t.localPosition.Dto(),
                    rotation = t.localRotation.Dto(),
                    scale = t.localScale.Dto(),
                    extensions = new NodeExtension()
                    {
                        sceneIndex = glTFSceneDto.nodes.Count,
                        id = SaveReference.GetId(t),
                        extensions = t.GetComponents<ISavable>().Select(s => s.Save()).ToList()
                    }
                };

                l.Add(glTFSceneDto.nodes.Count);
                glTFSceneDto.nodes.Add(glTFNodeDto);
                glTFNodeDto.children = SaveNodes(glTFSceneDto, t);
            }
            return l;
        }

    }

    public class NodeExtension
    {
        public int sceneIndex;
        public long id;
        public List<ComponentExtensionSO> extensions = new List<ComponentExtensionSO>();
    }



}
#endif
