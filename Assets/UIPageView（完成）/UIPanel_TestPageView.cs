using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NRatel;

public class UIPanel_TestPageView : MonoBehaviour
{
    public class TestData
    {
        public int id;
        public string str;
    }

    [SerializeField]
    private UIPageView m_UIPageView;

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
        //���������б�
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
        m_UIPageView.Init(m_CellRTTemplate, OnCreateCell, OnShowCell);
        m_UIPageView.StartShow(m_DataList.Count, false);
    }

    private RectTransform OnCreateCell(int index)
    {
        RectTransform cellRT = GameObject.Instantiate<GameObject>(m_CellRTTemplate.gameObject).GetComponent<RectTransform>();
        cellRT.GetComponent<ClickableCell>().Init((_clickedIndex) =>
        {
            TestData testData = m_DataList[_clickedIndex];
            Debug.Log(string.Format("��ǰ�����������{0}, ����Id��{1}, �����ַ�����{2}", _clickedIndex, testData.id, testData.str));
        });
        return cellRT;
    }

    private void OnShowCell(int index)
    {
        RectTransform cellRT = m_UIPageView.GetCellRT(index);
        cellRT.GetComponent<ClickableCell>().Refresh(index);
    }
}
