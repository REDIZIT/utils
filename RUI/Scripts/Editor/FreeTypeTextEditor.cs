using System;
using UnityEditor;
using UnityEngine;

namespace REDIZIT.RUI.Editor
{
    [CustomEditor(typeof(FreeTypeText))]
    [CanEditMultipleObjects]
    public class FreeTypeTextEditor : UnityEditor.Editor
    {
        private SerializedProperty _textProp;
        private SerializedProperty _fontNameProp;
        private SerializedProperty _fontSizeProp;
        private SerializedProperty _hintingProp;
        private SerializedProperty _alignmentProp;
        private SerializedProperty _wrapModeProp;
        private SerializedProperty _lineSpacingProp;
        private SerializedProperty _colorProp;
        private SerializedProperty _raycastTargetProp;
        private SerializedProperty _maskableProp;

        private bool _showRaycastOptions = false;

        private void OnEnable()
        {
            _textProp = serializedObject.FindProperty("text");
            _fontNameProp = serializedObject.FindProperty("fontName");
            _fontSizeProp = serializedObject.FindProperty("fontSize");
            _hintingProp = serializedObject.FindProperty("hinting");
            _alignmentProp = serializedObject.FindProperty("alignment");
            _wrapModeProp = serializedObject.FindProperty("wrapMode");
            _lineSpacingProp = serializedObject.FindProperty("lineSpacing");
            _colorProp = serializedObject.FindProperty("m_Color");
            _raycastTargetProp = serializedObject.FindProperty("m_RaycastTarget");
            _maskableProp = serializedObject.FindProperty("m_Maskable");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 1. Text Field
            EditorGUILayout.PropertyField(_textProp);

            EditorGUILayout.Space(4);

            // 2. Font Selector Dropdown
            var manager = FreeTypeFontManager.Instance;
            if (manager == null)
            {
                EditorGUILayout.HelpBox("На сцене не найден FreeTypeFontManager!\nСоздайте его и добавьте шрифты.", MessageType.Warning);
                EditorGUILayout.PropertyField(_fontNameProp);
            }
            else
            {
                string[] availableFonts = manager.GetAvailableFontNames();
                if (availableFonts.Length == 0)
                {
                    EditorGUILayout.HelpBox("В FreeTypeFontManager нет добавленных шрифтов.", MessageType.Info);
                    EditorGUILayout.PropertyField(_fontNameProp);
                }
                else
                {
                    int currentIndex = Mathf.Max(0, Array.IndexOf(availableFonts, _fontNameProp.stringValue));
                    int newIndex = EditorGUILayout.Popup("Font", currentIndex, availableFonts);
                    if (newIndex >= 0 && newIndex < availableFonts.Length)
                    {
                        _fontNameProp.stringValue = availableFonts[newIndex];
                    }
                }
            }

            // 3. Style & Layout Settings
            EditorGUILayout.PropertyField(_fontSizeProp);
            EditorGUILayout.PropertyField(_colorProp);
            EditorGUILayout.PropertyField(_alignmentProp);
            EditorGUILayout.PropertyField(_wrapModeProp);
            EditorGUILayout.PropertyField(_lineSpacingProp);
            EditorGUILayout.PropertyField(_hintingProp);

            EditorGUILayout.Space(6);

            // 4. Advanced UI Options
            _showRaycastOptions = EditorGUILayout.Foldout(_showRaycastOptions, "Advanced (Raycast & Masking)", true);
            if (_showRaycastOptions)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_raycastTargetProp);
                EditorGUILayout.PropertyField(_maskableProp);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}