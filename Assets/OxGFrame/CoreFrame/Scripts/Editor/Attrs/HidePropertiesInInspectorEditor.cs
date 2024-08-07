﻿using System.Collections.Generic;
using UnityEditor;

namespace OxGFrame.CoreFrame.Editor
{
    [CustomEditor(typeof(FrameBase), true)]
    [CanEditMultipleObjects()]
    public class HidePropertiesInInspectorEditor : UnityEditor.Editor
    {
        private HashSet<string> _hiddenProperties;

        protected virtual void OnEnable()
        {
            var targetType = this.target.GetType();
            var attrs = targetType.GetCustomAttributes(typeof(HidePropertiesInInspector), true) as HidePropertiesInInspector[];
            if (attrs != null && attrs.Length > 0)
            {
                this._hiddenProperties = new HashSet<string>();
                foreach (var attr in attrs)
                {
                    foreach (var property in attr.hiddenProperties)
                    {
                        this._hiddenProperties.Add(property);
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            this.DrawDefaultInspector();
        }

        public new bool DrawDefaultInspector()
        {
            // draw properties
            this.serializedObject.Update();
            var result = DrawDefaultInspectorExcept(this.serializedObject, this._hiddenProperties);
            if (result) this.serializedObject.ApplyModifiedProperties();

            return result;
        }

        #region Static Interface
        public static bool DrawDefaultInspectorExcept(SerializedObject serializedObject, HashSet<string> propsNotToDraw)
        {
            if (serializedObject == null) throw new System.ArgumentNullException("serializedObject");

            try
            {
                EditorGUI.BeginChangeCheck();
                var iterator = serializedObject.GetIterator();
                for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
                {
                    if (propsNotToDraw == null || !propsNotToDraw.Contains(iterator.name))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                }
                return EditorGUI.EndChangeCheck();
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}