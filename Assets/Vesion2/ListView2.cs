using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ListView2 : MonoBehaviour
{
    [SerializeField]
    public int cellCount = 5;           //计划生成的Cell数量

    [SerializeField]
    public RectTransform cellPrefabRT;  //Cell预设 的 RectTransform

    [SerializeField]
    public float paddingLeft = 10;      //左边界宽度

    [SerializeField]
    public float paddingRight = 10;     //右边界宽度

    [SerializeField]
    public float spacingX = 10;         //X向间距
    
    private float viewportOffset;       //设置视口容差（即左右两侧的Cell出现消失参考点），避免复用露馅 //为正时 参考位置向viewport的外围增加。// 默认值为一个spacing。

    private ScrollRect scrollRect;      //ScrollRect
    private RectTransform contentRT;    //Content 的 RectTransform
    private RectTransform viewPortRT;   //viewPort 的 RectTransform

    private float contentWidth;         //Content的总宽度
    private float pivotOffsetX;         //由Cell的pivot决定的起始偏移值

    private Dictionary<int, GameObject> cellDict;     //index-Cell字典    
    private Stack<GameObject> unUseCellStack;       //空闲Cell堆栈

    private List<int> oldIndexes;
    private List<int> newIndexes;

    private List<int> appearIndexes;
    private List<int> disAppearIndexes;

    private void Awake()
    {
        cellDict = new Dictionary<int, GameObject>();
        unUseCellStack = new Stack<GameObject>();

        oldIndexes = new List<int>();
        newIndexes = new List<int>();
        appearIndexes = new List<int>();
        disAppearIndexes = new List<int>();

        //设置视口容差默认值
        viewportOffset = spacingX;

        //依赖的组件
        scrollRect = GetComponent<ScrollRect>();
        contentRT = scrollRect.content;
        viewPortRT = scrollRect.viewport;

        //强制设置 Content的 anchor 和 pivot
        contentRT.anchorMin = new Vector2(0, contentRT.anchorMin.y);
        contentRT.anchorMax = new Vector2(0, contentRT.anchorMax.y);
        contentRT.pivot = new Vector2(0, contentRT.pivot.y);

        //计算和设置Content总宽度
        //当cellCount小于等于0时，Content总宽度 = 0
        //当cellCount大于0时，Content总宽度 = 左边界间隙 + 所有Cell的宽度总和 + 相邻间距总和 + 右边界间隙
        contentWidth = cellCount <= 0 ? 0 : paddingLeft + cellPrefabRT.rect.width * cellCount + spacingX * (cellCount - 1) + paddingRight;
        contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);

        //计算由Cell的pivot决定的起始偏移值
        pivotOffsetX = cellPrefabRT.pivot.x * cellPrefabRT.rect.width;

        //注册滑动事件
        scrollRect.onValueChanged.AddListener(onScrollValueChanged);
    }

    private void Start()
    {
        if (cellCount < 0)
        {
            return;
        }

        CalcIndexes();
        DisAppearCells();
        AppearCells();
    }
    
    private void onScrollValueChanged(Vector2 value)
    {
        if (cellCount < 0)
        {
            return;
        }

        CalcIndexes();
        DisAppearCells();
        AppearCells();
    }

    private void CalcIndexes()
    {
        //content左边界，相对于viewport左边界的位移，矢量！向左滑为正方向
        float deltaLeft = -contentRT.anchoredPosition.x - viewportOffset;
        //content右边界，相对于viewport右边界的位移，矢量！向右滑为正方向
        float deltaRight = contentWidth - viewPortRT.rect.width - (-contentRT.anchoredPosition.x) - viewportOffset;

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

        int startIndex = (outFromLeft); // 省略了，先+1再-1。 从滑出的下一个开始，索引从0开始;
        int endIndex = (cellCount - 1 - outFromRight);

        //Debug.Log("startIndex, endIndex: " + startIndex + ", " + endIndex);

        for (int index = startIndex; index <= endIndex; index++)
        {
            newIndexes.Add(index);
        }

        //新旧索引列表输出调试
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

    private void DisAppearCells()
    {
        foreach (int index in disAppearIndexes)
        {
            GameObject cell = cellDict[index];
            cellDict.Remove(index);
            unUseCellStack.Push(cell);

            cell.SetActive(false);
        }
    }

    private void AppearCells()
    {
        foreach (int index in appearIndexes)
        {
            GameObject cell = GetOrCreateCell(index);
            //计算和设置Cell的位置
            RectTransform cellRT = cell.GetComponent<RectTransform>();
            cellRT.anchoredPosition = new Vector2(CalcCellPosX(index), cellRT.anchoredPosition.y);
            cellDict[index] = cell;

            cell.GetComponent<Cell>().SetIndex(index);
        }
    }

    private GameObject GetOrCreateCell(int index)
    {
        GameObject cell;
        if (unUseCellStack.Count > 0)
        {
            cell = unUseCellStack.Pop();
            cell.SetActive(true);
        }
        else
        {
            cell = GameObject.Instantiate<GameObject>(cellPrefabRT.gameObject);
            RectTransform cellRT = cell.GetComponent<RectTransform>();
            cellRT.SetParent(contentRT, false);
            //强制设置Cell的anchor
            cellRT.anchorMin = new Vector2(0, cellRT.anchorMin.y);
            cellRT.anchorMax = new Vector2(0, cellRT.anchorMax.y);
        }

        return cell;
    }

    private float CalcCellPosX(int index)
    {
        //X = 左边界间隙 + 由Cell的pivot决定的起始偏移值 + 前面已有Cell的宽度总和 + 前面已有的间距总和
        return paddingLeft + pivotOffsetX + cellPrefabRT.rect.width * index + spacingX * index;
    }
}