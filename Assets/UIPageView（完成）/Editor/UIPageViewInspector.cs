using UnityEngine;
using UnityEditor;

namespace NRatel
{
    [CustomEditor(typeof(UIPageView), true)]
    [CanEditMultipleObjects]
    public class UIPageViewInspector : UIListViewInspector
    {
        SerializedProperty m_CellOccupyPage;

        SerializedProperty m_Snap;
        SerializedProperty m_SnapSpeed;
        SerializedProperty m_SnapWaitScrollSpeed;

        SerializedProperty m_Carousel;
        SerializedProperty m_CarouselInterval;
        SerializedProperty m_CarouselSpeed;
        SerializedProperty m_Reverse;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_CellOccupyPage = serializedObject.FindProperty("m_CellOccupyPage");

            m_Snap = serializedObject.FindProperty("m_Snap");
            m_SnapSpeed = serializedObject.FindProperty("m_SnapSpeed");
            m_SnapWaitScrollSpeed = serializedObject.FindProperty("m_SnapWaitScrollSpeed");

            m_Carousel = serializedObject.FindProperty("m_Carousel");
            m_CarouselInterval = serializedObject.FindProperty("m_CarouselInterval");
            m_CarouselSpeed = serializedObject.FindProperty("m_CarouselSpeed");
            m_Reverse = serializedObject.FindProperty("m_Reverse");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            if (DrawHeader("Page", "Page"))
            {
                BeginContent();
                DrawPage();
                EndContent();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPage()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Basic", EditorStyles.boldLabel);

            // 基础分页属性
            EditorGUILayout.PropertyField(m_CellOccupyPage, new GUIContent("Cell Occupy Page", "Each cell occupies a full page"));

            EditorGUILayout.Space();

            // 分页吸附效果
            EditorGUILayout.LabelField("Snap", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Snap, new GUIContent("Auto Snap", "Enable automatic page snap"));
            if (m_Snap.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SnapSpeed, new GUIContent("Snap Speed", "Page switching animation speed"));
                EditorGUILayout.PropertyField(m_SnapWaitScrollSpeed, new GUIContent("Wait Scroll Speed", "Minimum drag speed to trigger page turn"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // 轮播功能
            EditorGUILayout.LabelField("Carousel", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Carousel, new GUIContent("Auto Carousel", "Enable automatic page rotation"));
            if (m_Carousel.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_CarouselInterval, new GUIContent("Interval", "Time between page switches (seconds)"));
                EditorGUILayout.PropertyField(m_CarouselSpeed, new GUIContent("Switch Speed", "Carousel animation speed"));
                EditorGUILayout.PropertyField(m_Reverse, new GUIContent("Reverse", "Reverse carouse direction"));
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
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
