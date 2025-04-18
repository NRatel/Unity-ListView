using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ListCell : MonoBehaviour
{
    public Text text;

    private Image m_Image;
    static private Dictionary<int, Color> sm_ColorCache;

    private void Awake()
    {
        m_Image = GetComponent<Image>();
        sm_ColorCache = new Dictionary<int, Color>();
    }

    public void Refresh(int index)
    {
        text.text = index.ToString();

        bool exist = sm_ColorCache.TryGetValue(index, out Color color);
        if (!exist) 
        {
            color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            sm_ColorCache[index] = color;
        }
        m_Image.color = color;
    }
}
