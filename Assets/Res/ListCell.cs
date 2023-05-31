using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ListCell : MonoBehaviour
{
    public Text text;

    void Start()
    {
        GetComponent<Image>().color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
    }

    public void Refresh(int index)
    {
        text.text = index.ToString();
    }
}
