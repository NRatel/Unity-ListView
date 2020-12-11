using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;

namespace NRatel
{
    //[ExecuteInEditMode]
    public partial class ListView : MonoBehaviour
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

        protected List<int> oldIndexes;         //旧的索引集合
        protected List<int> newIndexes;         //新的索引集合

        protected List<int> appearIndexes;      //将要出现的索引集合   //使用List而非单个，可以支持Content位置跳变
        protected List<int> disAppearIndexes;   //将要消失的索引集合   //使用List而非单个，可以支持Content位置跳变

        private void Awake()
        {
            cellRTDict = new Dictionary<int, RectTransform>();
            unUseCellRTStack = new Stack<RectTransform>();
            cellRTListForSort = new List<KeyValuePair<int, RectTransform>>();

            oldIndexes = new List<int>();
            newIndexes = new List<int>();
            appearIndexes = new List<int>();
            disAppearIndexes = new List<int>();
            
            scrollRect = GetComponent<ScrollRect>();
            contentRT = scrollRect.content;
            viewportRT = scrollRect.viewport;
            
            contentRT.anchorMin = new Vector2(0, contentRT.anchorMin.y);
            contentRT.anchorMax = new Vector2(0, contentRT.anchorMax.y);
            contentRT.pivot = new Vector2(0, contentRT.pivot.y);
            
            pivotOffsetX = cellPrefabRT.pivot.x * cellPrefabRT.rect.width;

            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        private void Start()
        {
            if (cellCount < 0)
            {
                return;
            }

            CalcAndSetContentSize();

            CalcIndexes();
            DisAppearCells();
            AppearCells();
            CalcAndSetCellsSblingIndex();
        }

        private void OnScrollValueChanged(Vector2 delta)
        {
            if (cellCount < 0)
            {
                return;
            }

            CalcIndexes();
            DisAppearCells();
            AppearCells();
            CalcAndSetCellsSblingIndex();
        }

        //计算并设置Content大小
        private void CalcAndSetContentSize()
        {
            contentWidth = paddingLeft + cellPrefabRT.rect.width * cellCount + spacingX * (cellCount - 1) + paddingRight;
            contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
        }

        //计算 应出现的索引 和 应消失的索引
        private void CalcIndexes()
        {
            float outFromLeft = -(contentRT.anchoredPosition.x + viewportOffsetLeft);
            float outFromRight = contentWidth + contentRT.anchoredPosition.x - (viewportRT.rect.width + viewportOffsetRight);
            int outFromLeftCount = 0;
            int outFromRightCount = 0;

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

            int startIndex = (outFromLeftCount);
            int endIndex = (cellCount - 1 - outFromRightCount);

            for (int index = startIndex; index <= endIndex; index++)
            {
                newIndexes.Add(index);
            }

            appearIndexes.Clear();
            foreach (int index in newIndexes)
            {
                if (oldIndexes.IndexOf(index) < 0)
                {
                    appearIndexes.Add(index);
                }
            }
            disAppearIndexes.Clear();
            foreach (int index in oldIndexes)
            {
                if (newIndexes.IndexOf(index) < 0)
                {
                    disAppearIndexes.Add(index);
                }
            }
            
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
                cellRT.anchoredPosition = new Vector2(CalcCellPosX(index), cellRT.anchoredPosition.y);
                cellRT.GetComponent<Cell>().SetIndex(index);
            }
        }

        //计算Cell的X坐标
        private float CalcCellPosX(int index)
        {
            float x = paddingLeft + pivotOffsetX + cellPrefabRT.rect.width * index + spacingX * index;
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
                cellRT.anchorMin = new Vector2(0, cellRT.anchorMin.y);
                cellRT.anchorMax = new Vector2(0, cellRT.anchorMax.y);
            }

            return cellRT;
        }
    }
}