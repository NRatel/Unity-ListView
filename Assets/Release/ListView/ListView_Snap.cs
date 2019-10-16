using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NRatel
{
    public partial class ListView: IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        //自动对齐
        private void TrySnap()
        {
            //content左边界 相对于 viewport左边界（含viewportOffset）的位移，矢量！向左为正方向
            float outFromLeftPure = -contentRT.anchoredPosition.x;
            //content右边界 相对于 viewport右边界（含viewportOffset） 的位移，矢量！向右为正方向
            float outFromRightPure = contentWidth - viewportRT.rect.width - (-contentRT.anchoredPosition.x);

            //Debug.Log("outFromLeftPure: " + outFromLeftPure.ToString());
            //Debug.Log("outFromRightPure: " + outFromRightPure.ToString());

            //越过或踏着边界不处理，保证左右两头能够完整自然滑动到底
            if (outFromLeftPure <= 0 || outFromRightPure <= 0) { return; }

            //余数，outFromLeftPure大于0， 所以余数也必大于0
            float remainder = outFromLeftPure % (cellPrefabRT.rect.width + spacingX);
            Debug.Log("remainder: " + remainder);
            //对齐点
            float snapX = 0;

            //余出来的是否超过 单位一半
            if (remainder <= (cellPrefabRT.rect.width + spacingX) / 2)
            {
                snapX = contentRT.anchoredPosition.x - remainder;   //倒回右侧邻近处
            }
            else
            {
                snapX = contentRT.anchoredPosition.x + (cellPrefabRT.rect.width + spacingX) - remainder;   //前往左侧邻近处
            }

            contentRT.anchoredPosition = new Vector2(snapX, contentRT.anchoredPosition.y);
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            
        }

        public void OnDrag(PointerEventData eventData)
        {

        }

        public void OnEndDrag(PointerEventData eventData)
        {
            
        }
    }
}

