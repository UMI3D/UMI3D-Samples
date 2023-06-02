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
using inetum.unityUtils.editor;
using NUnit.Framework;
using System.Collections.Generic;
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

            if (GUILayout.Button("Save environment"))
            {
                SaveReference references = new SaveReference();
                UMI3DEnvironment env = gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DEnvironment>()).FirstOrDefault(e => e != null);
                if (env != null)
                {
                    SaveEnvironment(env, references);
                }
            }

            if (GUILayout.Button("Load TMP"))
            {
                SaveReference references = new SaveReference();
                UMI3DDto dto = UMI3DDtoSerializer.FromJson(draw.data.tmp);
                if (dto is GlTFEnvironmentDto environmentDto)
                {
                    LoadEnvironment(environmentDto, references);
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


    }

    public class SceneSaver
    {
        private void SaveEnvironment(UMI3DEnvironment env, SaveReference references)
        {
            var ext = new NodeExtension()
            {
                sceneIndex = -1,
                id = references.GetId(env.gameObject),
                extensions = env.gameObject.GetComponentExtensionSOs(references).ToList()
            };

            var glTFEnvironmentDto = new GlTFEnvironmentDto()
            {
                extensions = ext,
                id = (ulong)ext.id
            };

            SaveScenes(glTFEnvironmentDto, references);

            draw.data.tmp = glTFEnvironmentDto.ToJson();
        }

        private void SaveScenes(GlTFEnvironmentDto glTFEnvironmentDto, List<UMI3DScene> scenes, SaveReference references)
        {
            foreach (UMI3DScene obj in gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DScene>()))
            {
                var ext = new NodeExtension()
                {
                    sceneIndex = -1,
                    id = references.GetId(obj.gameObject),
                    extensions = obj.gameObject.GetComponentExtensionSOs(references).ToList()
                };

                var glTFSceneDto = new GlTFSceneDto
                {
                    name = obj.name,
                    extensions = ext,
                };

                SaveNodes(glTFSceneDto, obj.transform, references);


                glTFEnvironmentDto.scenes.Add(glTFSceneDto);
            }
        }

        private List<int> SaveNodes(GlTFSceneDto glTFSceneDto, Transform node, SaveReference references)
        {
            var l = new List<int>();
            foreach (Transform t in node)
            {
                if (t == node.transform || t.GetComponent<UMI3DScene>() != null)
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
                        id = references.GetId(t.gameObject),
                        extensions = t.GetComponentExtensionSOs(references).ToList()
                    }
                };

                l.Add(glTFSceneDto.nodes.Count);
                glTFSceneDto.nodes.Add(glTFNodeDto);
                glTFNodeDto.children = SaveNodes(glTFSceneDto, t, references);
            }
            return l;
        }

        private void LoadEnvironment(GlTFEnvironmentDto environmentDto, SaveReference references)
        {
            GameObject env = gameobjects.SelectMany(o => o.GetComponentsInChildren<UMI3DEnvironment>()).FirstOrDefault(e => e != null)?.gameObject ?? new GameObject("UMI3DEnvironment");
            if (environmentDto.extensions is NodeExtension nodeExt)
            {
                foreach (ComponentExtensionSO ext in nodeExt.extensions)
                {
                    references.GetId(env, nodeExt.id);
                    switch (ext)
                    {
                        case ComponentExtensionSO ce:
                            UMI3DSceneLoader.LoadOrUpdate(env, ce);
                            break;
                        default:
                            UnityEngine.Debug.Log(ext);
                            break;
                    }
                }
            }
            foreach (GlTFSceneDto g in environmentDto.scenes)
            {
                LoadScene(g, env, references);
            }
        }

        private void LoadScene(GlTFSceneDto glTFSceneDto, GameObject environment, SaveReference references)
        {
            var scene = new GameObject(glTFSceneDto.name);
            scene.transform.SetParent(environment.transform);

            if (glTFSceneDto.extensions is NodeExtension nodeExt)
            {
                references.GetId(scene, nodeExt.id);

                foreach (ComponentExtensionSO ext in nodeExt.extensions)
                {
                    switch (ext)
                    {
                        case ComponentExtensionSO ce:
                            UMI3DSceneLoader.LoadOrUpdate(scene, ce);
                            break;
                        default:
                            UnityEngine.Debug.Log(ext);
                            break;
                    }
                }
            }

            foreach (GlTFNodeDto node in glTFSceneDto.nodes)
                LoadNode(node, scene, references);

            foreach (GlTFNodeDto node in glTFSceneDto.nodes)
                SetNode(glTFSceneDto, node, scene, references);
        }

        private void LoadNode(GlTFNodeDto glTFNodeDto, GameObject parent, SaveReference references)
        {
            var node = new GameObject(glTFNodeDto.name);
            node.transform.SetParent(parent.transform);

            if (glTFNodeDto.extensions is NodeExtension nodeExt)
            {
                references.GetId(node, nodeExt.id);
                foreach (ComponentExtensionSO ext in nodeExt.extensions)
                {
                    switch (ext)
                    {
                        case ComponentExtensionSO ce:
                            UMI3DSceneLoader.LoadOrUpdate(node, ce);
                            break;
                        default:
                            UnityEngine.Debug.Log(ext);
                            break;
                    }
                }
            }
            node.transform.position = glTFNodeDto.position.Struct();
            node.transform.rotation = glTFNodeDto.rotation.Quaternion();
            node.transform.localScale = glTFNodeDto.scale.Struct();
        }

        private async void SetNode(GlTFSceneDto glTFSceneDto, GlTFNodeDto glTFNodeDto, GameObject parent, SaveReference references)
        {
            GameObject node = null;
            if (glTFNodeDto.extensions is NodeExtension ext)
            {
                node = await references.GetEntity<GameObject>(ext.id);
            }
            if (node == null)
                throw new System.Exception("No node found");

            foreach (int child in glTFNodeDto.children)
            {
                if (glTFSceneDto.nodes[child].extensions is NodeExtension ext2)
                {
                    GameObject n = await references.GetEntity<GameObject>(ext2.id);

                    n.transform.SetParent(node.transform, false);
                }
            }
        }
    }


}
#endif
