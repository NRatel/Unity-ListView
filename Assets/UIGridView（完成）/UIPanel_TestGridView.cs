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
        //���������б�
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
        m_UIGridView.Init(m_CellRTTemplate, OnCreateCell, OnShowCell);
        m_UIGridView.StartShow(m_DataList.Count, false);
    }

    private RectTransform OnCreateCell(int index)
    {
        RectTransform cellRT = GameObject.Instantiate<GameObject>(m_CellRTTemplate.gameObject).GetComponent<RectTransform>();
        cellRT.GetComponent<GridCell>().Init((_clickedIndex) =>
        {
            TestData testData = m_DataList[_clickedIndex];
            Debug.Log(string.Format("��ǰ�����������{0}, ����Id��{1}, �����ַ�����{2}", _clickedIndex, testData.id, testData.str));
        });
        return cellRT;
    }

    private void OnShowCell(int index)
    {
        RectTransform cellRT = m_UIGridView.GetCellRT(index);
        cellRT.GetComponent<GridCell>().Refresh(index);
    }
}
