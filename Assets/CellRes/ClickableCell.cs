using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClickableCell : DisplayableCell
{
    [SerializeField] private Button m_BtnClick;

    private Action<int> m_OnClick;

    private void Awake()
    {
        m_BtnClick.onClick.AddListener(() =>
        {
            m_OnClick?.Invoke(m_Index);
        });
    }

    public void Init(Action<int> onClick)
    {
        m_OnClick = onClick;
    }
}
