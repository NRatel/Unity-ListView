using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NRatel;

public class TestUIGridView : MonoBehaviour
{
    public RectTransform template;
    public UIGridView m_UIGridView;
    public Button m_BtnRefresh;

    void Awake()
    {
        m_BtnRefresh.onClick.AddListener(() => {
            StartShow();
        });
    }

    void Start()
    {
        StartShow();
    }

    void StartShow()
    {
        m_UIGridView.StartShow(template, 100, (index, cellRT) =>
        {
            cellRT.GetComponent<Cell>().SetIndex(index);
        });
    }
}
