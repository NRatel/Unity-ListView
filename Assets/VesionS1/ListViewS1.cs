using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ListViewS1 : ListView2
{
    [SerializeField]
    public float berthRight = 10;       //开始停靠的右边距（必须 <= paddingRight，否则最后一个不能完全展开）

    [SerializeField]
    public float berthAniWidth = 10;    //停靠动画变化的宽度

    [SerializeField]
    public float berthAniHeight = 10;   //停靠动画变化的高度

    protected override void onScrollValueChanged(Vector2 value)
    {
        base.onScrollValueChanged(value);

        if (cellCount < 0)
        {
            return;
        }

        //滑动时每帧更新位置
        CalcAndSetCellsPos();
    }

    protected override void CalcIndexes()
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
    
    //调用时机：滑动时每帧
    protected override void CalcAndSetCellsPos()
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
            float progress = 0;
            if (preX < triggerAniX)
            {
                progress = 0;
            }
            else if (preX <= berthX)
            {
                progress = (preX - triggerAniX) / (berthX - triggerAniX);   //曲线:正比方式
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