using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ListView2 : MonoBehaviour
{
    [SerializeField]
    public int cellCount = 0;               //计划生成的Cell数量

    [SerializeField]
    public RectTransform cellPrefabRT;      //Cell预设 的 RectTransform

    [SerializeField]
    public float paddingLeft = 10;          //左边界宽度

    [SerializeField]
    public float paddingRight = 10;         //右边界宽度

    [SerializeField]
    public float spacingX = 10;             //X向间距

    protected float viewportOffset;         //设置视口容差（即左右两侧的Cell出现消失参考点），避免复用露馅 //为正时 参考位置向viewport的外围增加。// 默认值为一个spacing。

    protected ScrollRect scrollRect;        //ScrollRect
    protected RectTransform contentRT;      //Content 的 RectTransform
    protected RectTransform viewportRT;     //viewport 的 RectTransform

    protected float contentWidth;           //Content的总宽度
    protected float pivotOffsetX;           //由Cell的pivot决定的起始偏移值

    protected Dictionary<int, RectTransform> cellRTDict;    //index-Cell字典    
    protected Stack<RectTransform> unUseCellRTStack;        //空闲Cell堆栈
    protected List<KeyValuePair<int, RectTransform>> cellRTListForSort;     //Cell列表用于辅助Sbling排序

    protected List<int> oldIndexes;         //旧的索引集合
    protected List<int> newIndexes;         //新的索引集合

    protected List<int> appearIndexes;      //将要出现的索引集合   //使用List而非单个，可以支持Content位置跳变
    protected List<int> disAppearIndexes;   //将要消失的索引集合   //使用List而非单个，可以支持Content位置跳变


    protected virtual void Awake()
    {
        cellRTDict = new Dictionary<int, RectTransform>();
        unUseCellRTStack = new Stack<RectTransform>();
        cellRTListForSort = new List<KeyValuePair<int, RectTransform>>();

        oldIndexes = new List<int>();
        newIndexes = new List<int>();
        appearIndexes = new List<int>();
        disAppearIndexes = new List<int>();

        //设置视口容差默认值
        viewportOffset = spacingX;

        //依赖的组件
        scrollRect = GetComponent<ScrollRect>();
        contentRT = scrollRect.content;
        viewportRT = scrollRect.viewport;

        //强制设置 Content的 anchor 和 pivot
        contentRT.anchorMin = new Vector2(0, contentRT.anchorMin.y);
        contentRT.anchorMax = new Vector2(0, contentRT.anchorMax.y);
        contentRT.pivot = new Vector2(0, contentRT.pivot.y);

        //计算由Cell的pivot决定的起始偏移值
        pivotOffsetX = cellPrefabRT.pivot.x * cellPrefabRT.rect.width;

        //注册滑动事件
        scrollRect.onValueChanged.AddListener(onScrollValueChanged);
    }

    protected virtual void Start()
    {
        if (cellCount < 0)
        {
            return;
        }

        CalcAndSetContentSize();

        CalcIndexes();
        DisAppearCells();
        AppearCells();
    }

    protected virtual void onScrollValueChanged(Vector2 value)
    {
        if (cellCount < 0)
        {
            return;
        }

        CalcIndexes();
        DisAppearCells();
        AppearCells();
    }

    protected virtual void CalcAndSetContentSize()
    {
        //计算和设置Content总宽度
        //当cellCount大于0时，Content总宽度 = 左边界间隙 + 所有Cell的宽度总和 + 相邻间距总和 + 右边界间隙
        contentWidth = paddingLeft + cellPrefabRT.rect.width * cellCount + spacingX * (cellCount - 1) + paddingRight;
        contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
    }

    protected virtual void CalcIndexes()
    {
        //content左边界 相对于viewport左边界 的位移，矢量！向左滑为正方向
        float deltaLeft = -contentRT.anchoredPosition.x - viewportOffset;
        //content右边界 相对于viewport右边界 的位移，矢量！向右滑为正方向
        float deltaRight = contentWidth - viewportRT.rect.width - (-contentRT.anchoredPosition.x) - viewportOffset;

        //Debug.Log("deltaLeft, deltaRight: " + deltaLeft + ", " + deltaRight);

        //计算完全滑出左边界和完全滑出右边的数量。
        //对于滑出的，要向下取整，即尽量认为其没滑出。
        int outFromLeft = 0;    //完全滑出左边界的数量
        int outFromRight = 0;   //完全滑出右边界的数量
        if (deltaLeft > 0)
        {
            outFromLeft = Mathf.FloorToInt((deltaLeft - paddingLeft + spacingX) / (cellPrefabRT.rect.width + spacingX));
            outFromLeft = Mathf.Clamp(outFromLeft, 0, cellCount);
        }
        if (deltaRight > 0)
        {
            outFromRight = Mathf.FloorToInt((deltaRight - paddingRight + spacingX) / (cellPrefabRT.rect.width + spacingX));
            outFromRight = Mathf.Clamp(outFromRight, 0, cellCount);
        }

        //Debug.Log("outFromLeft, outFromRight: " + outFromLeft + ", " + outFromRight);

        //应该显示的开始索引
        int startIndex = (outFromLeft); // 省略了 先+1再-1。 从滑出的下一个开始，索引从0开始;
        //应该显示的结束索引
        int endIndex = (cellCount - 1 - outFromRight);

        //Debug.Log("startIndex, endIndex: " + startIndex + ", " + endIndex);

        for (int index = startIndex; index <= endIndex; index++)
        {
            newIndexes.Add(index);
        }

        ////新旧索引列表输出调试
        //string Str1 = "";
        //foreach (int index in newIndexes)
        //{
        //    Str1 += index + ",";
        //}
        //string Str2 = "";
        //foreach (int index in oldIndexes)
        //{
        //    Str2 += index + ",";
        //}
        //Debug.Log("Str1: " + Str1);
        //Debug.Log("Str2: " + Str2);
        //Debug.Log("-------------------------");

        //找出出现的和消失的
        //出现的：在新列表中，但不在老列表中。
        appearIndexes.Clear();
        foreach (int index in newIndexes)
        {
            if (oldIndexes.IndexOf(index) < 0)
            {
                //Debug.Log("出现：" + index);
                appearIndexes.Add(index);
            }
        }

        //消失的：在老列表中，但不在新列表中。
        disAppearIndexes.Clear();
        foreach (int index in oldIndexes)
        {
            if (newIndexes.IndexOf(index) < 0)
            {
                //Debug.Log("消失：" + index);
                disAppearIndexes.Add(index);
            }
        }

        //oldIndexes保存当前帧索引数据。
        List<int> temp;
        temp = oldIndexes;
        oldIndexes = newIndexes;
        newIndexes = temp;
        newIndexes.Clear();
    }

    //该消失的消失
    protected virtual void DisAppearCells()
    {
        foreach (int index in disAppearIndexes)
        {
            RectTransform cellRT = cellRTDict[index];
            cellRTDict.Remove(index);
            cellRT.gameObject.SetActive(false);
            unUseCellRTStack.Push(cellRT);
        }
    }

    //该出现的出现
    protected virtual void AppearCells()
    {
        foreach (int index in appearIndexes)
        {
            RectTransform cellRT = GetOrCreateCell(index);
            cellRTDict[index] = cellRT;
        }

        //有Cell出现时
        if (appearIndexes.Count > 0)
        {
            CalcAndSetCellsSblingIndex();
            CalcAndSetCellsPos();
            SetCellsData();
        }
    }

    //计算并设置Cell的SblingIndex
    //调用时机：有新的Cell出现时
    //Cell可能重叠时必须
    //若无需求，可去掉以节省性能
    protected virtual void CalcAndSetCellsSblingIndex()
    {
        cellRTListForSort.Clear();
        foreach (KeyValuePair<int, RectTransform> kvp in cellRTDict)
        {
            cellRTListForSort.Add(kvp);
        }
        cellRTListForSort.Sort((x, y) =>
        {
            //按index升序
            return x.Key - y.Key;
        });

        foreach (KeyValuePair<int, RectTransform> kvp in cellRTListForSort)
        {
            //索引大的在上
            //kvp.Value.SetAsLastSibling();
            //索引大的在下
            kvp.Value.SetAsFirstSibling();
        }
    }

    //计算并设置Cell的位置
    //调用时机：有新的Cell出现时
    protected virtual void CalcAndSetCellsPos()
    {
        foreach (KeyValuePair<int, RectTransform> kvp in cellRTDict)
        {
            int index = kvp.Key;
            RectTransform cellRT = kvp.Value;

            //X = 左边界间隙 + 由Cell的pivot决定的起始偏移值 + 前面已有Cell的宽度总和 + 前面已有的间距总和
            float x = paddingLeft + pivotOffsetX + cellPrefabRT.rect.width * index + spacingX * index;

            cellRT.anchoredPosition = new Vector2(x, cellRT.anchoredPosition.y);
        }
    }

    //设置Cell数据
    protected virtual void SetCellsData()
    {
        foreach (KeyValuePair<int, RectTransform> kvp in cellRTDict)
        {
            int index = kvp.Key;
            RectTransform cellRT = kvp.Value;
            //在这里对Cell初始化
            cellRT.GetComponent<Cell>().SetIndex(index);
        }
    }

    private RectTransform GetOrCreateCell(int index)
    {
        RectTransform cellRT;
        if (unUseCellRTStack.Count > 0)
        {
            cellRT = unUseCellRTStack.Pop();
            cellRT.gameObject.SetActive(true);
        }
        else
        {
            cellRT = GameObject.Instantiate<GameObject>(cellPrefabRT.gameObject).GetComponent<RectTransform>();
            cellRT.SetParent(contentRT, false);
            //强制设置Cell的anchor
            cellRT.anchorMin = new Vector2(0, cellRT.anchorMin.y);
            cellRT.anchorMax = new Vector2(0, cellRT.anchorMax.y);
        }

        return cellRT;
    }
}