using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NRatel.Fundamental
{
    /// <summary>
    /// 实现复用
    /// </summary>
    public class ListViewV2 : MonoBehaviour
    {
        [SerializeField]
        public RectTransform cellPrefabRT;      //Cell预设 的 RectTransform

        [SerializeField]
        public int cellCount = 0;               //计划生成的Cell数量

        [SerializeField]
        public float paddingLeft = 10;          //左边界宽度

        [SerializeField]
        public float paddingRight = 10;         //右边界宽度

        [SerializeField]
        public float spacingX = 10;             //X向间距

        //视口容差（即左右两侧的Cell出现消失参考点），可避免复用时露馅  //为正时 参考位置向viewport的外围增加。
        protected float viewportOffsetLeft = 0;     //左侧视口容差
        protected float viewportOffsetRight = 0;    //右侧视口容差

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

        //仅用于子类功能扩展
        //默认情况下 Cell 从 Content 的 paddingLeft 处开始显示
        protected virtual float cellStartOffsetX { get { return 0f; } }

        protected virtual void Awake()
        {
            cellRTDict = new Dictionary<int, RectTransform>();
            unUseCellRTStack = new Stack<RectTransform>();
            cellRTListForSort = new List<KeyValuePair<int, RectTransform>>();

            oldIndexes = new List<int>();
            newIndexes = new List<int>();
            appearIndexes = new List<int>();
            disAppearIndexes = new List<int>();

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
            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        protected virtual void Start()
        {
            if (cellCount <= 0) { return; }

            FixPadding();
            FixSpacingX();
            FixViewportOffset();

            CalcAndSetContentSize();
            SetContentStartPos();

            CalcIndexes();
            DisAppearCells();
            AppearCells();
            CalcAndSetCellsSblingIndex();
        }

        protected virtual void OnScrollValueChanged(Vector2 delta)
        {
            if (cellCount <= 0) { return; }

            CalcIndexes();
            DisAppearCells();
            AppearCells();
            CalcAndSetCellsSblingIndex();
        }

        //调整边距
        protected virtual void FixPadding() { }

        //调整间距
        protected virtual void FixSpacingX() { }

        //调整视口容差
        protected virtual void FixViewportOffset()
        {
            viewportOffsetLeft = spacingX;
            viewportOffsetRight = spacingX;
        }

        //计算并设置Content大小
        protected virtual void CalcAndSetContentSize()
        {
            //计算和设置Content总宽度
            //当cellCount大于0时，Content总宽度 = 左边界间隙 + 所有Cell的宽度总和 + 相邻间距总和 + 右边界间隙
            contentWidth = cellCount > 0 ? paddingLeft + cellPrefabRT.rect.width * cellCount + spacingX * (cellCount - 1) + paddingRight : 0;
            contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
        }

        //设置起始位置
        protected virtual void SetContentStartPos()
        {
            contentRT.anchoredPosition = new Vector2(-cellStartOffsetX, contentRT.anchoredPosition.y);
        }

        //计算 应出现的索引 和 应消失的索引
        protected virtual void CalcIndexes()
        {
            float contentStartPosX = -cellStartOffsetX;

            //始终以viewpoert左边界为参考原点观察。则有：
            //content左边界 相对于 viewport左边界（含viewportOffset） 的位移为：
            float outWidthFromLeft = contentStartPosX  - (contentRT.anchoredPosition.x + viewportOffsetLeft);
            //content右边界 相对于 viewport右边界（含viewportOffset） 的位移为：
            float outWidthFromRight = contentStartPosX + contentRT.anchoredPosition.x + contentWidth - (viewportRT.rect.width + viewportOffsetRight);

            //计算完全滑出左边界和完全滑出右边的数量。 要向下取整，即尽量认为其没滑出，以保证可视区域内的正确性。

            //完全滑出左边界的数量（可为负）
            int outCountFromLeft = Mathf.FloorToInt((outWidthFromLeft - paddingLeft + spacingX) / (cellPrefabRT.rect.width + spacingX));

            //完全滑出右边界的数量（可为负）
            int outCountFromRight = Mathf.FloorToInt((outWidthFromRight - paddingRight + spacingX) / (cellPrefabRT.rect.width + spacingX));

            //Debug.Log($"outWidthFromLeft: {outWidthFromLeft}, outWidthFromRight: {outWidthFromRight}");
            //Debug.Log($"outCountFromLeft: {outCountFromLeft}, outCountFromRight: {outCountFromRight}");

            //应该显示的开始索引和结束索引
            int startIndex = (outCountFromLeft); // 省略了 先+1再-1。 从滑出的下一个开始，索引从0开始;
            int endIndex = (cellCount - 1 - outCountFromRight);

            //Debug.Log($"startIndex: {startIndex}, endIndex: {endIndex}");

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
                if (!IsValidIndex(index)) { continue; }

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
                if (!IsValidIndex(index)) { continue; }

                RectTransform cellRT = GetOrCreateCell(index);
                cellRTDict[index] = cellRT;

                //设置Cell位置
                cellRT.anchoredPosition = new Vector2(CalcCellPosX(index), cellRT.anchoredPosition.y);

                //设置Cell数据，对Cell进行初始化
                int validIndex = ConvertIndexToValid(index);
                cellRT.GetComponent<DisplayableCell>().Refresh(validIndex);
            }
        }

        //计算并设置Cells的SblingIndex
        //调用时机：有新的Cell出现时
        //Cell可能重叠时必须
        //若无需求，可去掉以节省性能
        protected virtual void CalcAndSetCellsSblingIndex()
        {
            if (appearIndexes.Count <= 0) { return; }

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

        //是否有效索引（只将显示索引显示到列表中，默认为 0~cellCount 之间）
        protected virtual bool IsValidIndex(int index)
        {
            return index >= 0 && index < cellCount;
        }

        //转换索引至有效（默认无需处理）
        protected virtual int ConvertIndexToValid(int index)
        {
            return index;
        }

        //计算Cell的X坐标
        protected virtual float CalcCellPosX(int index)
        {
            //X = 左边界间隙 + 由Cell的pivot决定的起始偏移值 + 前面已有Cell的宽度总和 + 前面已有的间距总和
            float x = paddingLeft + cellStartOffsetX + pivotOffsetX + cellPrefabRT.rect.width * index + spacingX * index;

            //Debug.Log("index, cellPosX" + index + "," + x);
            return x;
        }

        //获取或创建Cell
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
}