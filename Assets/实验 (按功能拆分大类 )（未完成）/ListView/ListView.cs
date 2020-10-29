using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NRatel
{
    public partial class ListView : MonoBehaviour
    {
        [SerializeField]
        public int cellCount = 0;               //计划生成的Cell数量
        public RectTransform cellPrefabRT;      //Cell预设 的 RectTransform

        public float paddingLeft = 0;           //左边界宽度
        public float paddingRight = 0;          //右边界宽度
        public float spacingX = 0;              //X向间距

        protected float viewportOffsetLeft = 0;    //左侧视口容差     //用于 viewport的计算大小大于实际大小，消除出现问题
        protected float viewportOffsetRight = 0;   //右侧视口容差

        protected ScrollRect scrollRect;        //ScrollRect
        protected RectTransform contentRT;      //Content 的 RectTransform
        protected RectTransform viewportRT;     //viewport 的 RectTransform

        protected float contentWidth;           //Content的总宽度
        protected float pivotOffsetX;           //由Cell的pivot决定的起始偏移值

        protected Dictionary<int, RectTransform> cellRTDict;    //index-Cell字典    
        protected Stack<RectTransform> unUseCellRTStack;        //空闲Cell堆栈
           
        private List<int> oldIndexes;         //旧的索引集合
        private List<int> newIndexes;         //新的索引集合

        private List<int> appearIndexes;      //将要出现的索引集合   //使用List而非单个，可以支持Content位置跳变
        private List<int> disAppearIndexes;   //将要消失的索引集合   //使用List而非单个，可以支持Content位置跳变

        protected virtual void Awake()
        {
            cellRTDict = new Dictionary<int, RectTransform>();
            unUseCellRTStack = new Stack<RectTransform>();

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
            if (cellCount < 0) { return; }

            CalcAndSetContentSize();

            CalcIndexes();
            DisAppearCells();
            AppearCells();
        }

        protected virtual void OnScrollValueChanged(Vector2 delta)
        {
            if (cellCount < 0) { return; }

            CalcIndexes();
            DisAppearCells();
            AppearCells();
        }

        //计算并设置Content大小
        private void CalcAndSetContentSize()
        {
            //计算和设置Content总宽度
            //cellCount大于0时，Content总宽度 = 左边界间隙 + 所有Cell的宽度总和 + 相邻间距总和 + 右边界间隙
            contentWidth = paddingLeft + cellPrefabRT.rect.width * cellCount + spacingX * (cellCount - 1) + paddingRight;
            contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
        }

        //计算 应出现的索引 和 应消失的索引
        private void CalcIndexes()
        {
            /***这块后边要改成以Content所在坐标系为基准进行计算***/

            //content左边界 相对于 viewport左边界（含viewportOffset） 的位移，矢量！向左为正方向
            float outFromLeft = -contentRT.anchoredPosition.x - viewportOffsetLeft;
            //content右边界 相对于 viewport右边界（含viewportOffset） 的位移，矢量！向右为正方向
            float outFromRight = contentWidth - viewportRT.rect.width - (-contentRT.anchoredPosition.x) - viewportOffsetRight;

            //Debug.Log("deltaLeft, deltaRight: " + deltaLeft + ", " + deltaRight);

            //计算完全滑出左边界和完全滑出右边的数量。
            //对于滑出的，要向下取整，即尽量认为其没滑出。
            int outFromLeftCount = 0;    //完全滑出左边界的数量
            int outFromRightCount = 0;   //完全滑出右边界的数量
            if (outFromLeft > 0)
            {
                outFromLeftCount = Mathf.FloorToInt((outFromLeft - paddingLeft + spacingX) / (cellPrefabRT.rect.width + spacingX));
                outFromLeftCount = Mathf.Clamp(outFromLeftCount, 0, cellCount);
            }
            if (outFromRight > 0)
            {
                outFromRightCount = Mathf.FloorToInt((outFromRight - paddingRight + spacingX) / (cellPrefabRT.rect.width + spacingX));
                outFromRightCount = Mathf.Clamp(outFromRightCount, 0, cellCount);
            }

            //Debug.Log("outFromLeft, outFromRight: " + outFromLeft + ", " + outFromRight);

            //应该显示的开始索引
            int startIndex = (outFromLeftCount); // 省略了 先+1再-1。 从滑出的下一个开始，索引从0开始;
                                                 //应该显示的结束索引
            int endIndex = (cellCount - 1 - outFromRightCount);

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

        //该出现的出现
        private void AppearCells()
        {
            foreach (int index in appearIndexes)
            {
                RectTransform cellRT = GetOrCreateCell(index);
                cellRTDict[index] = cellRT;

                //设置Cell位置
                cellRT.anchoredPosition = new Vector2(CalcCellPosX(index), cellRT.anchoredPosition.y);

                //设置Cell数据，对Cell进行初始化
                cellRT.GetComponent<Cell>().SetIndex(index);
            }

            //有Cell出现时, 重新设置SblingIndex，注意只能整体遍历处理
            if (appearIndexes.Count > 0)
            {
                SortCellsSblingIndex();
            }
        }

        //获取或创建Cell
        protected RectTransform GetOrCreateCell(int index)
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

        //计算Cell的X坐标
        protected float CalcCellPosX(int index)
        {
            //X = 左边界间隙 + 由Cell的pivot决定的起始偏移值 + 前面已有Cell的宽度总和 + 前面已有的间距总和
            float x = paddingLeft + pivotOffsetX + cellPrefabRT.rect.width * index + spacingX * index;
            return x;
        }
    }
}