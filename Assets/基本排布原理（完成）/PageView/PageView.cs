using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using NRatel.Fundamental;
using System;
using System.Reflection;

namespace NRatel
{
    public class PageView : ListViewV2, IBeginDragHandler, IEndDragHandler
    {
        [Header("Page Settings")]
        [SerializeField] public bool loop = true;                       //开启循环？（不改源码无法支持，不要开启！！！）
        [SerializeField] public bool cellOccupyPage = false;            //使Cell占用一页（强设将spacingX）

        [Header("Snap Settings")]
        [SerializeField] public float snapSpeed = 500f;                 //Snap速度
        [SerializeField] public float snapWaitScrollSpeedX = 50f;       //开启惯性时，等待基本停稳才开始Snap

        [Header("Carousel Settings")]
        [SerializeField] public bool carousel = false;                  //开启轮播？
        [SerializeField] public float carouselInterval = 3f;            //轮播启动间隔
        [SerializeField] public float carouselSpeed = 500f;             //轮播时移动的速度

        public event Action onSnapCompleted;

        private int m_CurPage = 0;
        private Coroutine m_SnapCoroutine;
        private Coroutine m_CarouselCoroutine;

        //核心内容宽度
        private float coreConetontWidth { get { return cellPrefabRT.rect.width * cellCount + spacingX * (cellCount - 1); } }

        //开启loop时，重置宽度
        private float loopResetWidth { get { return (coreConetontWidth + spacingX) * Mathf.CeilToInt(viewportRT.rect.width / coreConetontWidth); } }

        //开启loop时，扩展后宽度
        private float expandedContentWidth { get { return coreConetontWidth + loopResetWidth * 4; } }

        protected override void Start()
        {
            base.Start();
            if (cellCount > 0) { TryStartSnap(); }
        }

        #region Override
        protected override float cellStartOffsetX { get { return (expandedContentWidth - coreConetontWidth) / 2f; } }

        protected override void OnScrollValueChanged(Vector2 delta)
        {
            if (cellCount > 0) { TryHandleLoopPos(); }
            base.OnScrollValueChanged(delta);       // 保持原有逻辑
        }

        //调整边距
        protected override void FixPadding() 
        {
            if (loop) { paddingLeft = paddingRight = 0; }
            else { paddingLeft = paddingRight = (viewportRT.rect.width - cellPrefabRT.rect.width) / 2; }
        }

        //调整间距
        protected override void FixSpacingX()
        {
            if (!cellOccupyPage) return;
            spacingX = viewportRT.rect.width - cellPrefabRT.rect.width;
        }

        //计算并设置Content大小
        protected override void CalcAndSetContentSize()
        {
            if (loop)
            {
                contentWidth = cellCount > 0 ? expandedContentWidth : 0;
                contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
            }
            else
            {
                base.CalcAndSetContentSize();
            }
        }

        //loop时，认为任意索引都是有效的，以使非 0~cellCount 的区域能够显示元素，之后再在 ConvertIndexToValid 转换
        protected override bool IsValidIndex(int index)
        {
            if (loop) { return true; }
            else { return base.IsValidIndex(index); }
        }

        //loop时，将任意索引数转到 [0~cellCount-1] 中
        protected override int ConvertIndexToValid(int index)
        {
            if (loop) { return (index % cellCount + cellCount) % cellCount; }
            else { return base.ConvertIndexToValid(index); }
        }
        #endregion

        #region Drag Handling
        public void OnBeginDrag(PointerEventData eventData)
        {
            TryStopSnap();
            TryStopCarousel();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            TryStartSnap();
        }
        #endregion

        #region Loop
        private void TryHandleLoopPos()
        {
            if (!loop) return;

            //Content初始位置
            float contentStartPosX = -cellStartOffsetX;
            //获取当前位置
            float curContentPosX = contentRT.anchoredPosition.x;
            //Content向左时，Content重置点坐标（初始位置左侧1个重置宽度）
            float leftResetPosX = contentStartPosX - loopResetWidth;
            //Content向右时，Content重置点坐标（初始位置右侧1个重置宽度）
            float rightResetPosX = contentStartPosX + loopResetWidth;

            if (curContentPosX < leftResetPosX)
            {
                contentRT.anchoredPosition += Vector2.right * loopResetWidth;
            }
            //向左滑动时
            else if (curContentPosX > rightResetPosX)
            {
                contentRT.anchoredPosition += Vector2.left * loopResetWidth;
            }
        }
        #endregion

        #region Snap
        private void TryStartSnap()
        {
            m_SnapCoroutine = StartCoroutine(SnapRoutine());
        }

        private void TryStopSnap()
        {
            if (m_SnapCoroutine == null) { return; }

            StopCoroutine(m_SnapCoroutine);
            m_SnapCoroutine = null;
        }

        private IEnumerator SnapRoutine()
        {
            //Debug.Log($"【SnapRoutine】Snap 进入");
            //如果开启回弹，则需先等待回弹结束
            if (scrollRect.movementType == ScrollRect.MovementType.Elastic)
            {
                float leftPosX = 0;
                float rightPosX = -(contentRT.rect.width - viewportRT.rect.width);
                float offsetThreshold = 0.1f;

                //当前正向左边界回弹
                if (contentRT.anchoredPosition.x > leftPosX) 
                {
                    yield return new WaitUntil(() => { return contentRT.anchoredPosition.x <= leftPosX + offsetThreshold; });
                }
                //当前正向右边界回弹
                else if (contentRT.anchoredPosition.x < rightPosX)
                {
                    yield return new WaitUntil(() => { return contentRT.anchoredPosition.x > rightPosX - offsetThreshold; });
                }
                //Debug.Log($"【SnapRoutine】等待回弹结束");
            }

            //如果开启惯性，则需先等待其基本停稳
            if (scrollRect.inertia)
            {
                yield return new WaitUntil(() => 
                {
                    //Debug.Log("scrollRect.velocity.x: " + scrollRect.velocity.x);
                    return Mathf.Abs(scrollRect.velocity.x) < snapWaitScrollSpeedX; 
                });
                //Debug.Log($"【SnapRoutine】等待惯性停稳");
            }

            #region 找离Viewport中心最近的那个Cell。
            //注意这里的相对位置计算要求 Content和Viewport没有缩放
            Debug.Assert(contentRT.localScale == Vector3.one);
            Debug.Assert(viewportRT.localScale == Vector3.one);

            float minDistance = Mathf.Infinity;
            int minDistanceIndex = -1;
            foreach (var t in cellRTDict)
            {
                //Cell距离Content左边界的距离（标量）
                //注意，这里 Cell的 pivot 影响“Cell所处Viewport中心”的概念，若不想影响，考虑加个bool选项补偿掉。
                float widthFromContentLeft = t.Value.anchoredPosition.x;
                //Cell距离Viewport左边界的距离（标量）
                float widthFromViewportLeft = widthFromContentLeft + contentRT.anchoredPosition.x;
                //Cell距离Viewport中心的距离（矢量，若>0：在中心的右边）
                float distanceToViewportCenter = widthFromViewportLeft - viewportRT.rect.width / 2f;

                //Debug.Log($"【SnapRoutine】, index: {t.Key}, distanceToViewportCenter: {distanceToViewportCenter}");

                if (Mathf.Abs(distanceToViewportCenter) < Mathf.Abs(minDistance))
                {
                    minDistance = distanceToViewportCenter;
                    minDistanceIndex = t.Key;
                }
            }

            //兼容开启弹性时，从左右全部拉出屏幕的情况
            #endregion

            // 只需将 content 反向移动 minDistance。
            // 但注意 loop 会重置位置，
            // 因此不能“直接计算出目标位置，然后插值”
            // 而是要“每帧持续增加偏移，直到加够量”

            // 计算计划移动距离
            float planMoveDistanceX = -minDistance;
            //Debug.Log($"【SnapRoutine】Snap 开始，目标索引:{minDistanceIndex}, 移动距离: {planMoveDistanceX}");

            yield return DoMoveContentPosX(planMoveDistanceX, snapSpeed);

            //Debug.Log($"【SnapRoutine】Snap 结束");

            m_CurPage = minDistanceIndex;
            onSnapCompleted?.Invoke();
            TryStartCarousel();
        }
        #endregion

        #region Carousel
        private void TryStartCarousel()
        {
            if (!carousel) { return; }
            m_CarouselCoroutine = StartCoroutine(CarouselRoutine());
        }

        private void TryStopCarousel()
        {
            if (!carousel) { return; }
            if (m_CarouselCoroutine == null) { return; }

            StopCoroutine(m_CarouselCoroutine);
            m_CarouselCoroutine = null;
        }

        private IEnumerator CarouselRoutine()
        {
            // 等待轮播间隔
            yield return new WaitForSeconds(carouselInterval);

            // 计算计划移动距离 和 速度倍率
            float planMoveDistanceX;
            float speedRate = 1f;
            if (loop)
            {
                // 开启循环时，总是向后翻到下一页，但是要注意 conent位置会被重置
                // 这就意味着，逻辑不能是“移动到1页后的目标位置”，而是“位置增加量为1页”。
                planMoveDistanceX = -(cellPrefabRT.rect.width + spacingX);
            }
            else 
            {
                if (m_CurPage < cellCount - 1)
                {
                    // 未开启循环时，若当前处于非最后一页，则翻到下一页
                    planMoveDistanceX = -(cellPrefabRT.rect.width + spacingX);
                }
                else 
                {
                    // 未开启循环时，若当前处于最后一页，则迅速翻回到第一页
                    planMoveDistanceX = (cellPrefabRT.rect.width + spacingX) * (cellCount - 1);
                    speedRate = cellCount - 1;
                }
            }

            //Debug.Log($"【CarouselRoutine】Carousel开始, 移动距离: {planMoveDistanceX}");

            yield return DoMoveContentPosX(planMoveDistanceX, carouselSpeed * speedRate);

            //Debug.Log($"【CarouselRoutine】Carousel 结束");

            //执行对齐，对齐结束后将继续启动轮播
            TryStartSnap();
        }
        #endregion

        private float movedDistanceX = 0f;
        private IEnumerator DoMoveContentPosX(float planMoveDistanceX, float speed)
        {
            //先停止任何惯性速度
            scrollRect.StopMovement();  //m_Velocity = Vector2.zero

            //重置累计字段
            movedDistanceX = 0f;

            // 速度标量转向量
            float velocity = speed * Mathf.Sign(planMoveDistanceX);

            // 平滑增加位移
            while (Mathf.Abs(movedDistanceX) < Mathf.Abs(planMoveDistanceX))
            {
                float addX = velocity * Time.deltaTime; //若要忽略时间缩放，改用 Time.unscaledDeltaTime;

                // 检查是否会超过目标距离
                if (Mathf.Abs(movedDistanceX + addX) >= Mathf.Abs(planMoveDistanceX))
                {
                    // 直接设置到精确位置，并break
                    float remainingDistance = planMoveDistanceX - movedDistanceX;
                    contentRT.anchoredPosition += new Vector2(remainingDistance, 0);
                    movedDistanceX = planMoveDistanceX;
                    break;
                }
                else
                {
                    movedDistanceX += addX;
                    contentRT.anchoredPosition += new Vector2(addX, 0);
                }

                yield return null;
            }
        }
    }
}

