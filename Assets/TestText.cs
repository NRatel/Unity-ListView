using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestText : MonoBehaviour
{
    public Text t;

    void Awake()
    {
        t.text = "tttttttttttttttttttttttttttttttt";
        Debug.Log("t.preferredWidth:" + t.preferredWidth);      //在当前高度下，对应的宽度
        Debug.Log("t.preferredHeight:" + t.preferredHeight);    //在当前宽度下，对应的高度

        t.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, t.preferredWidth);
        t.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, t.preferredHeight);
    }

    // Start is called before the first frame update
    void Start()
    {
        //t.text = "123456789789123123jsdiklvfajsdlkfjasklfjaklsjfskldjfslakjfiuafjhiowj 阿斯顿发啊啊是多发放啥事啊";
        //Debug.Log("t.preferredWidth:" + t.preferredWidth);
        //Debug.Log("t.preferredHeight:" + t.preferredHeight);

        //t.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, t.preferredWidth);
        //t.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, t.preferredHeight);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
