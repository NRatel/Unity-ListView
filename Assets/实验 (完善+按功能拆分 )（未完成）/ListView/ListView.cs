using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// 暴露接口
/// </summary>

//ListView不保存数据，也不负责直接创建和显示Cell，可配合对象池处理。
//ListView仅依赖Cell的与布局相关的属性。
//有需求：支持编辑器下布局预览。  
namespace NRatel
{
    public partial class ListView
    {
        [SerializeField] private float paddingLeft = 0;              //左边界宽度
        [SerializeField] private float paddingRight = 0;             //右边界宽度
        [SerializeField] private float spacingX = 0;                 //水平间距

        public Action<int> onCellAppear;
        public Action<int> onCellDisAppear;
        
        public void SetCellLayoutInfo(float cellWidth, float cellHeight, Vector2 cellAnchorMin, Vector2 cellAnchorMax, Vector2 cellPivot)
        {
            this.cellWidth = cellWidth;
            this.cellHeight = cellHeight;
            this.cellAnchorMin = cellAnchorMin;
            this.cellAnchorMax = cellAnchorMax;
            this.cellPivot = cellPivot;
        }

        public void SetCellLayoutInfo(RectTransform cellRT)
        {
            SetCellLayoutInfo(cellRT.rect.width, cellRT.rect.height, cellRT.anchorMin, cellRT.anchorMax, cellRT.pivot);
        }

        public void Show(int cellCount)
        {
            this.cellCount = cellCount;
            
            CalcAndSetContentSize();
            CalcIndexes();
            DisAppearCells();
            AppearCells();
        }
    }
}