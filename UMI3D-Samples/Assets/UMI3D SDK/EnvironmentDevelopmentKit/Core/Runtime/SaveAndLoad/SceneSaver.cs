﻿/*
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
using System.Collections.Generic;
using System.Linq;
using umi3d.common;
using umi3d.edk.save;
using UnityEngine;

namespace umi3d.edk
{
    public static class SceneSaver
    {
        static public string SaveEnvironment(UMI3DEnvironment env, List<GameObject> objects, SaveReference references)
        {
            var ext = new NodeExtension()
            {
                sceneIndex = -1,
                id = references.GetId(env.gameObject),
                extensions = env.gameObject.GetComponentExtensionSOs(references).ToList<ExtensionSO>()
            };

            var glTFEnvironmentDto = new GlTFEnvironmentDto()
            {
                extensions = ext,
                id = (ulong)ext.id
            };

            SaveScenes(glTFEnvironmentDto, objects.SelectMany(o => o.GetComponentsInChildren<UMI3DScene>()).ToList(), references);

            ext.extensions.AddRange(UMI3DSceneLoader.GetScriptables(references));

            return glTFEnvironmentDto.ToJson();
        }

        static public void SaveScenes(GlTFEnvironmentDto glTFEnvironmentDto, List<UMI3DScene> scenes, SaveReference references)
        {
            foreach (UMI3DScene obj in scenes)
            {
                var ext = new NodeExtension()
                {
                    sceneIndex = -1,
                    id = references.GetId(obj.gameObject),
                    extensions = obj.gameObject.GetComponentExtensionSOs(references).ToList<ExtensionSO>()
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

        static public List<int> SaveNodes(GlTFSceneDto glTFSceneDto, Transform node, SaveReference references)
        {
            var l = new List<int>();
            foreach (Transform t in node)
            {
                if (t == node.transform || t.GetComponent<UMI3DScene>() != null)
                    continue;

                GlTFNodeDto glTFNodeDto = null;

                if (t is RectTransform rt)
                    glTFNodeDto = new GlTFRectNodeDto()
                    {
                        sizeDelta = rt.sizeDelta.Dto(),
                        anchorMin = rt.anchorMin.Dto(),
                        anchorMax = rt.anchorMax.Dto(),
                        pivot = rt.pivot.Dto()
                    };
                else
                    glTFNodeDto = new GlTFNodeDto();

                glTFNodeDto.name = t.name;
                glTFNodeDto.position = t.localPosition.Dto();
                glTFNodeDto.rotation = t.localRotation.Dto();
                glTFNodeDto.scale = t.localScale.Dto();
                glTFNodeDto.extensions = new NodeExtension()
                {
                    sceneIndex = glTFSceneDto.nodes.Count,
                    id = references.GetId(t.gameObject),
                    extensions = t.GetComponentExtensionSOs(references).ToList<ExtensionSO>()
                };

                l.Add(glTFSceneDto.nodes.Count);
                glTFSceneDto.nodes.Add(glTFNodeDto);
                glTFNodeDto.children = SaveNodes(glTFSceneDto, t, references);
            }
            return l;
        }

        static public void LoadEnvironment(GlTFEnvironmentDto environmentDto, GameObject env, SaveReference references)
        {
            
            if (environmentDto.extensions is NodeExtension nodeExt)
            {
                references.GetId(env, nodeExt.id);
                foreach (ExtensionSO ext in nodeExt.extensions)
                {
                    switch (ext)
                    {
                        case ComponentExtensionSO ce:
                            UMI3DSceneLoader.LoadOrUpdate(env, ce,references);
                            break;
                        case ScriptableExtensionSO se:
                            UMI3DSceneLoader.LoadOrUpdate(env, se, references);
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
            UnityEngine.Debug.Log($"Ref should be ok {references.debug}");
            references.ready = true;
        }

        static public void LoadScene(GlTFSceneDto glTFSceneDto, GameObject environment, SaveReference references)
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
                            UMI3DSceneLoader.LoadOrUpdate(scene, ce,references);
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

        static public void LoadNode(GlTFNodeDto glTFNodeDto, GameObject parent, SaveReference references)
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
                            UMI3DSceneLoader.LoadOrUpdate(node, ce,references);
                            break;
                        default:
                            UnityEngine.Debug.Log(ext);
                            break;
                    }
                }
            }

            if (glTFNodeDto is GlTFRectNodeDto glTFRectNodeDto && node.transform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = glTFRectNodeDto.anchorMin.Struct();
                rectTransform.anchorMax = glTFRectNodeDto.anchorMax.Struct();
                rectTransform.pivot = glTFRectNodeDto.pivot.Struct();
                rectTransform.sizeDelta = glTFRectNodeDto.sizeDelta.Struct();
            }

            node.transform.position = glTFNodeDto.position.Struct();
            node.transform.rotation = glTFNodeDto.rotation.Quaternion();
            node.transform.localScale = glTFNodeDto.scale.Struct();
        }

        static public async void SetNode(GlTFSceneDto glTFSceneDto, GlTFNodeDto glTFNodeDto, GameObject parent, SaveReference references)
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