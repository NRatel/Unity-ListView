using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridCell : MonoBehaviour
{
    public Text text;
    public Button btnClick;

    private int m_Index;
    private Action<int> m_OnClick;

    private void Awake()
    {
        btnClick.onClick.AddListener(() =>
        {
            m_OnClick?.Invoke(m_Index);
        });
    }

    void Start()
    {
        GetComponent<Image>().color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
    }

    public void Init(Action<int> onClick)
    {
        m_OnClick = onClick;
    }

    public void Refresh(int index)
    {
        m_Index = index;
        text.text = index.ToString();
    }
}
