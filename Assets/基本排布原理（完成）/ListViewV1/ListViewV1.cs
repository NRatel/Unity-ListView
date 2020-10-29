using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NRatel.Fundamental
{
    /// <summary>
    /// 基本布局
    /// </summary>
    public class ListViewV1 : MonoBehaviour
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

        private ScrollRect scrollRect;      //ScrollRect
        private RectTransform contentRT;    //Content 的 RectTransform
        private RectTransform viewPortRT;   //viewPort 的 RectTransform

        private List<GameObject> cells;     //所以Cell列表     
        private float contentWidth;         //Content的总宽度
        private float pivotOffsetX;         //由Cell的pivot决定的起始偏移值

        private void Awake()
        {
            cells = new List<GameObject>();

            //依赖的组件
            scrollRect = GetComponent<ScrollRect>();
            contentRT = scrollRect.content;
            viewPortRT = scrollRect.viewport;

            //计算和设置Content总宽度
            //当cellCount小于等于0时，Content总宽度 = 0
            //当cellCount大于0时，Content总宽度 = 左边界间隙 + 所有Cell的宽度总和 + 相邻间距总和 + 右边界间隙
            contentWidth = cellCount <= 0 ? 0 : paddingLeft + cellPrefabRT.rect.width * cellCount + spacingX * (cellCount - 1) + paddingRight;
            contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);

            //计算由Cell的pivot决定的起始偏移值
            pivotOffsetX = cellPrefabRT.pivot.x * cellPrefabRT.rect.width;
        }

        private void Start()
        {
            CreateCells();
            LayoutCells();
        }

        private void CreateCells()
        {
            for (int i = 0; i < cellCount; i++)
            {
                GameObject cell = GameObject.Instantiate<GameObject>(cellPrefabRT.gameObject);
                RectTransform cellRT = cell.GetComponent<RectTransform>();
                cellRT.SetParent(contentRT, false);
                //强制设置Cell的anchor
                cellRT.anchorMin = new Vector2(0, cellRT.anchorMin.y);
                cellRT.anchorMax = new Vector2(0, cellRT.anchorMax.y);
                cells.Add(cell);
                cellRT.GetComponent<Cell>().SetIndex(i);
            }
        }

        private void LayoutCells()
        {
            for (int index = 0; index < cells.Count; index++)
            {
                GameObject cell = cells[index];
                //计算和设置Cell的位置
                //X = 左边界间隙 + 由Cell的pivot决定的起始偏移值 + 前面已有Cell的宽度总和 + 前面已有的间距总和
                float x = paddingLeft + pivotOffsetX + cellPrefabRT.rect.width * index + spacingX * index;
                RectTransform cellRT = cell.GetComponent<RectTransform>();
                cellRT.anchoredPosition = new Vector2(x, cellRT.anchoredPosition.y);
            }
        }
    }

}