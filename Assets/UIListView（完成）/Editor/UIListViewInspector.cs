using UnityEngine;
using UnityEditor;

namespace NRatel
{
    [CustomEditor(typeof(UIListView), true)]
    [CanEditMultipleObjects]
    public class UIListViewInspector : UIScrollRectInspector
    {
        SerializedProperty m_Padding;
        SerializedProperty m_Spacing;
        SerializedProperty m_StartCorner;
        SerializedProperty m_ChildAlignment;
        SerializedProperty m_Loop;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            m_Padding = serializedObject.FindProperty("m_Padding");
            m_Spacing = serializedObject.FindProperty("m_Spacing");
            m_StartCorner = serializedObject.FindProperty("m_StartCorner");
            m_ChildAlignment = serializedObject.FindProperty("m_ChildAlignment");
            m_Loop = serializedObject.FindProperty("m_Loop");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (DrawHeader("Scroll", "Scroll"))
            {
                BeginContent();
                base.OnInspectorGUI();
                EndContent();
            }

            if (DrawHeader("Layout", "Layout"))
            {
                BeginContent();
                DrawLayout();
                EndContent();
            }

            if (DrawHeader("Loop", "Loop"))
            {
                BeginContent();
                DrawOthers();
                EndContent();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLayout()
        {
            EditorGUILayout.PropertyField(m_Padding, true);
            EditorGUILayout.PropertyField(m_Spacing, true);

            string[] horizontalStartCornerOptions = { "Left", "Right" };
            string[] verticalStartCornerOptions = { "Upper", "Lower" };
            string[] startCornerOptions = m_MovementAxis.enumValueIndex == 0 ? horizontalStartCornerOptions : verticalStartCornerOptions;
            m_StartCorner.enumValueIndex = EditorGUILayout.Popup("Start Corner", m_StartCorner.enumValueIndex, startCornerOptions);
            
            string[] horizontalAlignmentOptions = { "Left", "Center", "Right" };
            string[] verticalAlignmentOptions = { "Upper", "Middle", "Lower" };
            string[] alignmentOptions = m_MovementAxis.enumValueIndex == 0 ? verticalAlignmentOptions : horizontalAlignmentOptions;
            m_ChildAlignment.enumValueIndex = EditorGUILayout.Popup("Child Alignment", m_ChildAlignment.enumValueIndex, alignmentOptions);
        }

        private void DrawOthers()
        {
            EditorGUILayout.PropertyField(m_Loop, new GUIContent("Loop", "Enable infinite scrolling"));
        }

        private void BeginContent()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(10f));
            GUILayout.Space(10f);
            GUILayout.BeginVertical();
            GUILayout.Space(2f);
        }

        private void EndContent()
        {
            GUILayout.Space(3f);
            GUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(3f);
        }

        private bool DrawHeader(string text, string key)
        {
            bool state = EditorPrefs.GetBool(key, true);

            GUILayout.Space(3f);
            if (!state) GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
            GUILayout.BeginHorizontal();
            GUI.changed = true;

            text = "<b><size=11>" + text + "</size></b>";
            if (state) text = "\u25BC " + text;
            else text = "\u25BA " + text;
            if (!GUILayout.Toggle(true, text, "dragtab", GUILayout.MinWidth(20f))) state = !state;

            if (GUI.changed) EditorPrefs.SetBool(key, state);

            GUILayout.Space(2f);
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
            if (!state) GUILayout.Space(3f);
            return state;
        }

    }
}
