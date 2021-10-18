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
        private ScrollRect scrollRect;              //ScrollRect
        private RectTransform viewportRT;           //viewport 的 RectTransform
        private float viewportOffsetLeft = 10;      //左侧视口容差
        private float viewportOffsetRight = 10;     //右侧视口容差
        private RectTransform contentRT;            //Content 的 RectTransform
        private float contentWidth;                 //Content的总宽度
        
        private float cellWidth;                    //Cell宽度
        private float cellHeight;                   //Cell高度
        private Vector2 cellAnchorMin;              //Cell锚点最小点
        private Vector2 cellAnchorMax;              //Cell锚点最大点
        private Vector2 cellPivot;                  //Cell中心点
        private int cellCount = 0;                  //Cell数量
        
        private List<int> oldIndexes;               //旧的索引集合
        private List<int> newIndexes;               //新的索引集合
        private List<int> appearIndexes;            //将要出现的索引集合   //使用List而非单个，可以支持Content位置跳变
        private List<int> disAppearIndexes;         //将要消失的索引集合   //使用List而非单个，可以支持Content位置跳变

        private void Awake()
        {
            InitCore();
            InitPool();
        }

        private void Start()
        {
            TryStartPreview();
        }

        private void InitCore()
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

        //计算应出现的索引和应消失的索引
        private void CalcIndexes()
        {
            float outWidthFromLeft = 0 + contentRT.anchoredPosition.x + viewportOffsetLeft;
            float outWidthFromRight = 0 + contentRT.anchoredPosition.x + contentWidth - (viewportRT.rect.width + viewportOffsetRight);
            int outCountFromLeft = 0;
            int outCountFromRight = 0;
            if (outWidthFromLeft < 0)
            {
                outCountFromLeft = Mathf.FloorToInt((-outWidthFromLeft - paddingLeft + spacingX) / (cellWidth + spacingX));
                outCountFromLeft = Mathf.Clamp(outCountFromLeft, 0, cellCount);
            }
            if (outWidthFromRight > 0)
            {
                outCountFromRight = Mathf.FloorToInt((outWidthFromRight - paddingRight + spacingX) / (cellWidth + spacingX));
                outCountFromRight = Mathf.Clamp(outCountFromRight, 0, cellCount);
            }
            int startIndex = (outCountFromLeft);
            int endIndex = (cellCount - 1 - outCountFromRight);

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

        //先让该消失的消失
        private void DisAppearCells()
        {
            foreach (int index in disAppearIndexes)
            {
                onCellDisAppear?.Invoke(index);
            }
        }

        //再让该出现的出现
        private void AppearCells()
        {
            foreach (int index in appearIndexes)
            {
                onCellAppear?.Invoke(index);
            }
        }

        //计算Cell位置
        private float GetCellPosX(int index)
        {
            float anchorOffsetX = (cellAnchorMin.x + cellAnchorMax.x) / 2 * contentWidth;
            float pivotOffsetX = cellPivot.x * cellWidth;
            return paddingLeft + cellWidth * index + spacingX * index + pivotOffsetX - anchorOffsetX;
        }
    }
}