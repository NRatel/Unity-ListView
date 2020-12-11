using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NRatel
{
    public partial class ListView: IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public enum SnapType
        {
            None,
            SnapOneByOne,   //Content 每移动 cellPrefabRT.rect.width + spacingX 为一步，停在就近的整步处。
            SnapToCenter    //离Viewport中心最近的Cell停在 Viewport中心 处。
        }

        private enum MoveState
        {
            Free,
            Draging,
            Snaping
        }

        private MoveState moveState = MoveState.Free;

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log("OnBeginDrag");
        }

        public void OnDrag(PointerEventData eventData)
        {
            Debug.Log("OnDrag");
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log("OnEndDrag");
        }


        ////自动对齐
        //private void StartSnapOneByOne()
        //{
        //    //content左边界 相对于 viewport左边界（含viewportOffset）的位移，矢量！向左为正方向
        //    float outFromLeftPure = -contentRT.anchoredPosition.x;
        //    //content右边界 相对于 viewport右边界（含viewportOffset） 的位移，矢量！向右为正方向
        //    float outFromRightPure = contentWidth - viewportRT.rect.width - (-contentRT.anchoredPosition.x);

        //    //越过或踏着边界不处理，保证左右两头能够完整自然滑动到底
        //    if (outFromLeftPure <= 0 || outFromRightPure <= 0) { return; }

        //    //余数，outFromLeftPure大于0， 所以余数也必大于0
        //    float remainder = outFromLeftPure % (cellPrefabRT.rect.width + spacingX);
        //    Debug.Log("remainder: " + remainder);
        //    //对齐点
        //    float snapX = 0;

        //    //余出来的是否超过 单位一半
        //    if (remainder <= (cellPrefabRT.rect.width + spacingX) / 2)
        //    {
        //        snapX = contentRT.anchoredPosition.x + remainder;   //倒回右侧邻近处（注意这里向右是+）
        //    }
        //    else
        //    {
        //        snapX = contentRT.anchoredPosition.x + remainder - (cellPrefabRT.rect.width + spacingX);   //前往左侧邻近处
        //    }

        //    contentRT.anchoredPosition = new Vector2(snapX, contentRT.anchoredPosition.y);
        //}

        ////自动对齐
        //private void StartSnapToCenter()  
        //{
        //    //content左边界 相对于 viewport左边界（含viewportOffset）的位移，矢量！向左为正方向
        //    float outFromLeftPure = -contentRT.anchoredPosition.x;
        //    //content右边界 相对于 viewport右边界（含viewportOffset） 的位移，矢量！向右为正方向
        //    float outFromRightPure = contentWidth - viewportRT.rect.width - (-contentRT.anchoredPosition.x);

        //    //越过或踏着边界不处理，保证左右两头能够完整自然滑动到底
        //    if (outFromLeftPure <= 0 || outFromRightPure <= 0) { return; }
        //}
    }
}

