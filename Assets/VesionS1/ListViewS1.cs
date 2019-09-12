using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ListViewS1 : MonoBehaviour
{
    [SerializeField]
    public int cellCount = 5;           //计划生成的Cell数量

    [SerializeField]
    public RectTransform cellPrefabRT;  //Cell预设 的 RectTransform

    [SerializeField]
    public float paddingLeft = 10;      //左边距

    [SerializeField]
    public float paddingRight = 10;     //右边距

    [SerializeField]
    public float spacingX = 10;         //X向间距

    [SerializeField]
    public float berthRight = 10;       //开始停靠的右边距（必须 <= paddingRight，否则最后一个不能完全展开）

    [SerializeField]
    public float berthAniWidth = 10;    //停靠动画变化的宽度

    [SerializeField]
    public float berthAniHeight = 100;  //停靠动画变化的高度

    private float viewportOffset;       //设置视口容差（即左右两侧的Cell出现消失参考点），避免复用露馅 //为正时 参考位置向viewport的外围增加。// 默认值为一个spacing。

    private ScrollRect scrollRect;      //ScrollRect
    private RectTransform contentRT;    //Content 的 RectTransform
    private RectTransform viewportRT;   //viewPort 的 RectTransform

    private float contentWidth;         //Content的总宽度
    private float pivotOffsetX;         //由Cell的pivot决定的起始偏移值

    private Dictionary<int, RectTransform> cellRTDict;   //index-Cell字典   
    private List<KeyValuePair<int, RectTransform>> cellRTListForSort;          //Cell列表用于辅助Sbling排序
    private Stack<RectTransform> unUseCellRTStack;       //空闲Cell堆栈

    private List<int> oldIndexes;       //旧的索引集合
    private List<int> newIndexes;       //新的索引集合

    private List<int> appearIndexes;    //将要出现的索引集合
    private List<int> disAppearIndexes; //将要消失的索引集合

    private void Awake()
    {
        cellRTDict = new Dictionary<int, RectTransform>();
        cellRTListForSort = new List<KeyValuePair<int, RectTransform>>();
        unUseCellRTStack = new Stack<RectTransform>();

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
    }

    private void Start()
    {
        //注册滑动事件
        scrollRect.onValueChanged.AddListener(onScrollValueChanged);
        CalcAndSetContentSize();

        if (cellCount < 0) { return; }

        CalcIndexes();
        DisAppearCells();
        AppearCells();
        CalcAndSetCellsSblingIndex();
        CalcAndSetCellsPos();
    }

    private void onScrollValueChanged(Vector2 value)
    {
        if (cellCount < 0) { return; }

        CalcIndexes();
        DisAppearCells();
        AppearCells();
        CalcAndSetCellsSblingIndex();
        CalcAndSetCellsPos();
    }

    private void CalcAndSetContentSize()
    {
        //计算和设置Content总宽度
        //当cellCount小于等于0时，Content总宽度 = 0
        //当cellCount大于0时，Content总宽度 = 左边界间隙 + 所有Cell的宽度总和 + 相邻间距总和 + 右边界间隙
        contentWidth = cellCount <= 0 ? 0 : paddingLeft + cellPrefabRT.rect.width * cellCount + spacingX * (cellCount - 1) + paddingRight;
        contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
    }

    private void CalcIndexes()
    {
        //由于右边Cell需要折叠放置，所以要多设一些viewportOffset，让其晚一些隐藏
        float viewportOffsetLeft = viewportOffset;
        float viewportOffsetRight = 2 * (cellPrefabRT.rect.width + spacingX);

        //content左边界，相对于viewport左边界的位移，矢量！向左滑为正方向
        float deltaLeft = -contentRT.anchoredPosition.x - viewportOffsetLeft;
        //content右边界，相对于viewport右边界的位移，矢量！向右滑为正方向
        float deltaRight = contentWidth - viewportRT.rect.width - (-contentRT.anchoredPosition.x) - viewportOffsetRight;

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
        int startIndex = (outFromLeft); // 省略了，先+1再-1。 从滑出的下一个开始，索引从0开始;
        //应该显示的结束索引
        int endIndex = (cellCount - 1 - outFromRight);

        //Debug.Log("startIndex, endIndex: " + startIndex + ", " + endIndex);

        for (int index = startIndex; index <= endIndex; index++)
        {
            newIndexes.Add(index);
        }

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

    private void DisAppearCells()
    {
        foreach (int index in disAppearIndexes)
        {
            RectTransform cellRT = cellRTDict[index];
            cellRTDict.Remove(index);
            cellRT.gameObject.SetActive(false);
            unUseCellRTStack.Push(cellRT);
        }
    }

    private void AppearCells()
    {
        foreach (int index in appearIndexes)
        {
            RectTransform cellRT = GetOrCreateCell(index);
            cellRTDict[index] = cellRT;

            cellRT.GetComponent<Cell>().SetIndex(index);
        }
    }

    private void CalcAndSetCellsSblingIndex()
    {
        if (cellRTDict.Count <= 0)
        {
            //无Cell不排序
            return;
        }
        if (appearIndexes.Count <= 0 && disAppearIndexes.Count <= 0)
        {
            //无变化不重新排序
            return;
        }

        //设置SiblingIndex
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
        int minIndex = cellRTListForSort[0].Key;
        int maxIndex = cellRTListForSort[cellRTListForSort.Count - 1].Key;
        foreach (KeyValuePair<int, RectTransform> kvp in cellRTListForSort)
        {
            //kvp.Value.SetSiblingIndex(kvp.Key - minIndex);    //索引大的在上
            kvp.Value.SetSiblingIndex(maxIndex - kvp.Key);    //索引大的在下
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

    private void CalcAndSetCellsPos()
    {
        foreach (KeyValuePair<int, RectTransform> kvp in cellRTDict)
        {
            int index = kvp.Key;
            //Cell的X坐标 = 左边界间隙 + 由Cell的pivot决定的起始偏移值 + 前面已有Cell的宽度总和 + 前面已有的间距总和
            float x = paddingLeft + pivotOffsetX + cellPrefabRT.rect.width * index + spacingX * index;

            //Cell anchoredPosition所在坐标系中的 视口右边界位置。（向右为正方向）
            //即，当Content的pivot、anchor的X都为0时，
            //在Content上来看，视口右边界相对于Content左边界 的位置位移 - （Cell宽度 - 由Cell的pivot决定的起始偏移值）
            float viewportRightX = -contentRT.anchoredPosition.x + viewportRT.rect.width - (cellPrefabRT.rect.width - pivotOffsetX);

            //Cell开始停靠的位置
            float berthX = viewportRightX - berthRight;
            if (x > berthX) { x = berthX; }

            //上一个Cell的X坐标
            float preX = paddingLeft + pivotOffsetX + cellPrefabRT.rect.width * (index - 1) + spacingX * (index - 1);

            //上一个Cell，触发当前Cell的停靠动画的位置（相对于停靠位置，往左 2/3个Cell宽度的地方）
            float triggerAniX = berthX - (2.0f / 3 * cellPrefabRT.rect.width);

            float y = 0;
            if (preX <= triggerAniX)
            {
                //x、y均保持不变
                //x = x;
                //y = y;
            }
            else if (preX < berthX)
            {
                //x、y移动时正比变化
                x = x + berthAniWidth / (berthX - triggerAniX) * (preX - triggerAniX);
                y = y - berthAniHeight / (berthX - triggerAniX) * (preX - triggerAniX);
            }
            else
            {
                //x到达最右、y到达最下
                x = berthX + berthAniWidth;
                y = 0 - berthAniHeight;
            }

            kvp.Value.anchoredPosition = new Vector2(x, y);
        }
    }
}