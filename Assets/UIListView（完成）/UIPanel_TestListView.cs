using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NRatel;

public class UIPanel_TestListView : MonoBehaviour
{
    public class TestData
    {
        public int id;
        public string str;
    }

    [SerializeField]
    private UIListView m_UIListView;

    [SerializeField]
    private RectTransform m_CellRTTemplate;
    
    [SerializeField]
    private Button m_BtnRefresh;

    private List<TestData> m_DataList;

    void Awake()
    {
        m_BtnRefresh.onClick.AddListener(() => {
            StartShow();
        });
    }

    void Start()
    {
        //创建数据列表
        m_DataList = new List<TestData>();
        for (int i = 0; i < 10; i++) 
        {
            m_DataList.Add(new TestData() 
            { 
                id = i,
                str = "hello " + i 
            }); 
        }

        StartShow();
    }

    void StartShow()
    {
        m_UIListView.Init(m_CellRTTemplate, OnCreateCell, OnShowCell);
        m_UIListView.StartShow(m_DataList.Count, false);
    }

    private RectTransform OnCreateCell(int index)
    {
        RectTransform cellRT = GameObject.Instantiate<GameObject>(m_CellRTTemplate.gameObject).GetComponent<RectTransform>();
        cellRT.GetComponent<ClickableCell>().Init((_clickedIndex) =>
        {
            TestData testData = m_DataList[_clickedIndex];
            Debug.Log(string.Format("当前点击，索引：{0}, 测试Id：{1}, 测试字符串：{2}", _clickedIndex, testData.id, testData.str));
        });
        return cellRT;
    }

    private void OnShowCell(int index)
    {
        RectTransform cellRT = m_UIListView.GetCellRT(index);
        cellRT.GetComponent<ClickableCell>().Refresh(index);
    }
}
