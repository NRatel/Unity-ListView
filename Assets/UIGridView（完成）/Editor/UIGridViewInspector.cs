using UnityEngine;
using UnityEditor;

namespace NRatel
{
    [CustomEditor(typeof(UIGridView), true)]
    [CanEditMultipleObjects]
    public class UIGridViewInspector : UIScrollRectInspector
    {
        SerializedProperty m_Padding;
        //SerializedProperty m_CellSize;
        SerializedProperty m_Spacing;
        SerializedProperty m_StartCorner;
        //SerializedProperty m_StartAxis;
        SerializedProperty m_ChildAlignment;
        SerializedProperty m_Constraint;
        SerializedProperty m_ConstraintCount;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            m_Padding = serializedObject.FindProperty("m_Padding");
            //m_CellSize = serializedObject.FindProperty("m_CellSize");
            m_Spacing = serializedObject.FindProperty("m_Spacing");
            m_StartCorner = serializedObject.FindProperty("m_StartCorner");
            //m_StartAxis = serializedObject.FindProperty("m_StartAxis");
            m_ChildAlignment = serializedObject.FindProperty("m_ChildAlignment");
            m_Constraint = serializedObject.FindProperty("m_Constraint");
            m_ConstraintCount = serializedObject.FindProperty("m_ConstraintCount");
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

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLayout()
        {
            EditorGUILayout.PropertyField(m_Padding, true);
            EditorGUILayout.PropertyField(m_Spacing, true);
            EditorGUILayout.PropertyField(m_StartCorner, true);
            
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Popup("Start Axis", 1 - m_MovementAxis.enumValueIndex, m_MovementAxis.enumDisplayNames); //È¡·´
            }
            
            string[] horizontalAlignmentOptions = { "Left", "Center", "Right" };
            string[] verticalAlignmentOptions = { "Upper", "Middle", "Lower" };
            string[] alignmentOptions = m_MovementAxis.enumValueIndex == 0 ? verticalAlignmentOptions : horizontalAlignmentOptions;
            m_ChildAlignment.enumValueIndex = EditorGUILayout.Popup("Child Alignment", m_ChildAlignment.enumValueIndex, alignmentOptions);

            string[] horizontalConstraintOptions = { m_Constraint.enumDisplayNames[0], m_Constraint.enumDisplayNames[1] };
            string[] verticalConstraintOptions = { m_Constraint.enumDisplayNames[0], m_Constraint.enumDisplayNames[2] };
            string[] constraintOptions = m_MovementAxis.enumValueIndex == 0 ? horizontalConstraintOptions : verticalConstraintOptions;
            int curConstraintIndex = m_Constraint.enumValueIndex > 0 ? 1 : 0;   //012=>01
            int newConstraintIndex = EditorGUILayout.Popup("Constraint", curConstraintIndex, constraintOptions);
            m_Constraint.enumValueIndex = newConstraintIndex > 0 ? (m_MovementAxis.enumValueIndex == 0 ? 1 : 2) : 0;    //01=>012
            if (m_Constraint.enumValueIndex > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_ConstraintCount, true);
                EditorGUI.indentLevel--;
            }
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
