using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;

//ListView只受Cell宽高、数量影响。
//ListView不保存数据，也不负责创建和显示Cell。
//使用处，利用回调处理Cell的显示和隐藏。
//要支持编辑器下布局预览。
namespace NRatel
{
    //[ExecuteInEditMode]
    public partial class ListView : MonoBehaviour
    {
        [SerializeField] private float cellWidth = 0;                //左边界宽度
        [SerializeField] private float cellHeight = 0;               //左边界宽度
        [SerializeField] private float paddingLeft = 0;              //左边界宽度
        [SerializeField] private float paddingRight = 0;             //右边界宽度
        [SerializeField] private float spacingX = 0;                 //水平间距

        public Action<int> onCellAppear;
        public Action<int> onCellDisAppear;

        private int cellCount = 0;                  //Cell数量
        private float viewportOffsetLeft = 10;      //左侧视口容差
        private float viewportOffsetRight = 10;     //右侧视口容差
        private ScrollRect scrollRect;              //ScrollRect
        private RectTransform contentRT;            //Content 的 RectTransform
        private RectTransform viewportRT;           //viewport 的 RectTransform
        private float contentWidth;                 //Content的总宽度

        private List<int> oldIndexes;               //旧的索引集合
        private List<int> newIndexes;               //新的索引集合
        private List<int> appearIndexes;            //将要出现的索引集合   //使用List而非单个，可以支持Content位置跳变
        private List<int> disAppearIndexes;         //将要消失的索引集合   //使用List而非单个，可以支持Content位置跳变

        private void Awake()
        {
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

            scrollRect.onValueChanged.AddListener(ScrollAndCalc);
        }
            
        public void Init(int cellCount)
        {
            this.cellCount = cellCount;

            CalcAndSetContentSize();

            CalcIndexes();
            DisAppearCells();
            AppearCells();
        }

        private void ScrollAndCalc(Vector2 delta)
        {
            CalcIndexes();
            DisAppearCells();
            AppearCells();
        }

        //计算并设置Content大小
        private void CalcAndSetContentSize()
        {
            contentWidth = paddingLeft + cellWidth * cellCount + spacingX * (cellCount - 1) + paddingRight;
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
                outFromLeftCount = Mathf.FloorToInt((outFromLeft - paddingLeft + spacingX) / (cellWidth + spacingX));
                outFromLeftCount = Mathf.Clamp(outFromLeftCount, 0, cellCount);
            }
            if (outFromRight > 0)
            {
                outFromRightCount = Mathf.FloorToInt((outFromRight - paddingRight + spacingX) / (cellHeight + spacingX));
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
                onCellDisAppear?.Invoke(index);
            }
        }

        //该出现的出现
        private void AppearCells()
        {
            foreach (int index in appearIndexes)
            {
                onCellAppear?.Invoke(index);
            }
        }

        private float GetCellPosX(int index, Vector2 cellAnchorMin, Vector2 cellAnchorMax, Vector2 cellPivot)
        {
            float anchorOffsetX = (cellAnchorMin.x + cellAnchorMax.x) /2 * contentWidth;
            float pivotOffsetX = cellPivot.x * cellWidth;
            return paddingLeft + cellWidth * index + spacingX * index + pivotOffsetX - anchorOffsetX;
        }
    }
}