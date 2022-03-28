using NRatel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Test : UIScrollRect, ILayoutGroup, ILayoutElement
{
    public void DoSetDirty()
    {
        Debug.Log("DoSetDirty" + Time.frameCount);
        SetDirty();

        //LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        Debug.Log("((RectTransform)this.transform).rect.size: " + ((RectTransform)this.transform).rect.size);
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        Debug.Log("CalculateLayoutInputHorizontal" + Time.frameCount);

    }
    public override void CalculateLayoutInputVertical()
    {
        base.CalculateLayoutInputVertical();
        Debug.Log("CalculateLayoutInputVertical" + Time.frameCount);
    }

    public void SetLayoutHorizontal()
    {
        Debug.Log("SetLayoutHorizontal" + Time.frameCount);
    }

    public void SetLayoutVertical()
    {
        Debug.Log("SetLayoutVertical" + Time.frameCount);
    }
}
