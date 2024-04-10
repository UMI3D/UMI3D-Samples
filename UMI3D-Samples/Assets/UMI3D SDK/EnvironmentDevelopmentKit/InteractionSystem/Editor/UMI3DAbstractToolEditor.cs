﻿/*
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

#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using umi3d.edk.interaction;
using UnityEditor;
using UnityEngine;

namespace umi3d.edk.editor
{
    /// <summary>
    /// <see cref="AbstractTool"/> custom editor.
    /// </summary>
    [CustomEditor(typeof(AbstractTool), true)]
    public class UMI3DAbstractToolEditor : Editor
    {
        private AbstractTool t;
        private SerializedProperty Active;
        protected SerializedObject _target;
        private SerializedProperty interactions;
        private SerializedProperty display;
        private ListDisplayer<AbstractInteraction> ListDisplayer;
        private SerializedProperty onProjection;
        private SerializedProperty onRelease;
        private static bool displayEvent = false;

        private GUIStyle toolNameStyle = new();
        private Color toolNameColorActive = new Color(0.15f, 0.15f, 1f, 1f);
        private Color toolNameColorInactive = Color.gray;

        protected virtual void OnEnable()
        {
            t = (AbstractTool)target;
            _target = new SerializedObject(t);
            display = _target.FindProperty("Display");
            interactions = _target.FindProperty("Interactions");
            onProjection = _target.FindProperty("onProjection");
            onRelease = _target.FindProperty("onRelease");
            Active = _target.FindProperty("Active");
            wasActive = Active.boolValue;
            ListDisplayer = new ListDisplayer<AbstractInteraction>();

            toolNameStyle.contentOffset = new Vector2(5, 5);
            toolNameStyle.contentOffset = new Vector2(5, 5);
        }

        protected virtual void _OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(Active);
            EditorGUILayout.PropertyField(display);
            ListDisplayer.Display(ref showList, interactions, t.Interactions,
                t =>
                {
                    switch (t)
                    {
                        case AbstractInteraction i:
                            return new List<AbstractInteraction>() { i };
                        case GameObject g:
                            return g.GetComponents<AbstractInteraction>().ToList();
                        default:
                            return null;
                    }
                });

            displayEvent = EditorGUILayout.Foldout(displayEvent, "Tool Events", true);
            if (displayEvent)
            {
                EditorGUILayout.PropertyField(onProjection, true);
                EditorGUILayout.PropertyField(onRelease, true);
            }
        }

        private static bool showList = true;

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            _target.Update();
            _OnInspectorGUI();
            _target.ApplyModifiedProperties();
        }

        private bool wasActive;

        public virtual void OnSceneGUI()
        {
            var t = target as AbstractTool;
            if (t.Active && !wasActive)
            {
                toolNameStyle.normal.textColor = toolNameColorActive;
                wasActive = true;
            }
            else if (!t.Active && wasActive)
            {
                toolNameStyle.normal.textColor = toolNameColorInactive;
                wasActive = false;
            }

            Handles.Label(t.transform.position,
                              t.Display.name,
                              toolNameStyle);
        }
    }
}
#endif