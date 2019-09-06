using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    public Text text;

    void Start()
    {
        GetComponent<Image>().color = new Color(Random.value, Random.value, Random.value);
    }

    public void SetIndex(int index)
    {
        Debug.Log("index: " + index);
        text.text = index.ToString();
    }
}
