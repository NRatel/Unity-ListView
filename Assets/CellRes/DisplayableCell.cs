using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayableCell : MonoBehaviour
{
    [SerializeField] private Image m_Image;
    [SerializeField] private Text m_Text;

    static protected Dictionary<int, Color> sm_ColorCache;

    protected int m_Index;

    static DisplayableCell()
    {
        sm_ColorCache = new Dictionary<int, Color>();
    }

    public virtual void Refresh(int index)
    {
        m_Index = index;
        m_Text.text = index.ToString();

        bool exist = sm_ColorCache.TryGetValue(index, out Color color);
        if (!exist) 
        {
            color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            sm_ColorCache[index] = color;
        }
        m_Image.color = color;
    }
}
