using NRatel.Fundamental;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NRatel
{
    public class ListViewS1 : ListViewV2
    {
        [SerializeField]
        public float berthRight = 0;       //开始停靠的右边距（必须 <= paddingRight，否则最后一个不能完全展开）
        [SerializeField]
        public float berthAniWidth = 0;    //停靠动画变化的宽度
        [SerializeField]
        public float berthAniHeight = 0;   //停靠动画变化的高度

        protected override void Start()
        {
            FixViewportOffset();

            base.Start();

            if (cellCount < 0) { return; }
            ReCalcAndSetCellsPos();
        }

        protected override void OnScrollValueChanged(Vector2 value)
        {
            base.OnScrollValueChanged(value);

            if (cellCount < 0) { return; }
            ReCalcAndSetCellsPos();
        }

        //设置视口容差
        protected override void FixViewportOffset()
        {
            viewportOffsetLeft = spacingX;
            viewportOffsetRight = 2 * (cellPrefabRT.rect.width + spacingX);
        }

        //重新计算Cell位置。调用时机：滑动时每帧
        private void ReCalcAndSetCellsPos()
        {
            foreach (KeyValuePair<int, RectTransform> kvp in cellRTDict)
            {
                int index = kvp.Key;
                //Cell的X坐标 = 左边界间隙 + 由Cell的pivot决定的起始偏移值 + 前面已有Cell的宽度总和 + 前面已有的间距总和
                float x = base.CalcCellPosX(index);

                //Cell anchoredPosition所在坐标系中的 视口右边界位置。（向右为正方向）
                //即，当Content的pivot、anchor的X都为0时，
                //在Content上来看，视口右边界相对于Content左边界 的位置位移 - （Cell宽度 - 由Cell的pivot决定的起始偏移值）
                float viewportRightX = -contentRT.anchoredPosition.x + viewportRT.rect.width - (cellPrefabRT.rect.width - pivotOffsetX);

                //Cell开始停靠的位置
                float berthX = viewportRightX - berthRight;
                if (x > berthX) { x = berthX; }

                //上一个Cell的X坐标
                float preX = paddingLeft + pivotOffsetX + cellPrefabRT.rect.width * (index - 1) + spacingX * (index - 1);

                //上一个Cell，触发当前Cell的停靠动画的位置（相对于停靠位置，往左 2/3个Cell宽度的地方）（可调）
                float triggerAniX = berthX - (2.0f / 3 * cellPrefabRT.rect.width);

                float y = 0;
                float progress = 0;
                if (preX < triggerAniX)
                {
                    progress = 0;
                }
                else if (preX <= berthX)
                {
                    progress = (preX - triggerAniX) / (berthX - triggerAniX);   //停靠曲线:正比直线
                    progress = Mathf.Clamp(progress, 0, 1);
                }
                else
                {
                    progress = 1;
                }
                x = x + berthAniWidth * progress;
                y = y - berthAniHeight * progress;

                kvp.Value.anchoredPosition = new Vector2(x, y);
            }
        }
    }
}