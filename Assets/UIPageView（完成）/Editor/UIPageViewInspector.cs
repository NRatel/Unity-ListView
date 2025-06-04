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
        SerializedProperty m_SnapWaitInertiaSpeed;

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
            m_SnapWaitInertiaSpeed = serializedObject.FindProperty("m_SnapWaitInertiaSpeed");

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
            EditorGUILayout.LabelField("Type", EditorStyles.boldLabel);

            // 基础分页属性
            EditorGUILayout.PropertyField(m_CellOccupyPage, new GUIContent("Cell Occupy Page", "使每个Cell占用一整页（viewport在滑动方向的大小）"));

            EditorGUILayout.Space();

            // 分页吸附效果
            EditorGUILayout.LabelField("Snap", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true); //禁止交互
            EditorGUILayout.PropertyField(m_Snap, new GUIContent("Auto Snap", "启用自动吸附/对齐，暂固定为勾选，否则将退化为 ListView"));
            EditorGUI.EndDisabledGroup();
            if (m_Snap.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SnapSpeed, new GUIContent("Snap Speed", "吸附/对齐速度"));
                EditorGUILayout.PropertyField(m_SnapWaitInertiaSpeed, new GUIContent("Wait Inertia Speed", "开启惯性时，等待基本停稳才开始Snap，停稳阈值"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // 轮播功能
            EditorGUILayout.LabelField("Carousel", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Carousel, new GUIContent("Auto Carousel", "启用自动轮播"));
            if (m_Carousel.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_CarouselInterval, new GUIContent("Interval", "轮播间隔 (秒)"));
                EditorGUILayout.PropertyField(m_CarouselSpeed, new GUIContent("Switch Speed", "轮播翻页速度"));
                EditorGUILayout.PropertyField(m_Reverse, new GUIContent("Reverse", "反向轮播"));
                
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
