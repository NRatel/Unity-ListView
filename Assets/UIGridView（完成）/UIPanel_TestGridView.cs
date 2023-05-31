using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NRatel;

public class TestData
{
    public int id;
    public string str;
}

public class UIPanel_TestGridView : MonoBehaviour
{
    [SerializeField]
    private UIGridView m_UIGridView;

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
        for (int i = 0; i < 100; i++) 
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
        m_UIGridView.StartShow(m_CellRTTemplate, m_DataList.Count, OnCellCreated, OnCellAppear);
    }

    private void OnCellCreated(int index)
    {
        RectTransform cellRT = m_UIGridView.GetCellRT(index);

        cellRT.GetComponent<GridCell>().Init((_clickedIndex) =>
        {
            TestData testData = m_DataList[_clickedIndex];

            Debug.Log(string.Format("当前点击，索引：{0}, 测试Id：{1}, 测试字符串：{2}", _clickedIndex, testData.id, testData.str));
        });
    }

    private void OnCellAppear(int index)
    {
        RectTransform cellRT = m_UIGridView.GetCellRT(index);

        cellRT.GetComponent<GridCell>().Refresh(index);
    }
}
