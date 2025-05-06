using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NRatel
{
    public class UIPageView : UIListView
    {
        [SerializeField] private bool m_CellOccupyPage = false;           //使Cell占用一页（强设将spacing.x）

        [SerializeField] private bool m_Snap = false;                     //开启Snap？
        [SerializeField] private float m_SnapSpeed = 500f;                //Snap速度
        [SerializeField] private float m_SnapWaitScrollSpeed = 50f;       //开启惯性时，等待基本停稳才开始Snap

        [SerializeField] private bool m_Carousel = false;                 //开启轮播？
        [SerializeField] private float m_CarouselInterval = 3f;           //轮播启动间隔
        [SerializeField] private float m_CarouselSpeed = 500f;            //轮播时移动的速度

        private int m_CurPage = 0;
        private Coroutine m_SnapCoroutine;
        private Coroutine m_CarouselCoroutine;

        public event Action onSnapCompleted;

        #region Override
        //调整边距（注意只调整滑动方向）
        protected override void FixPadding()
        {
            if (m_Loop) 
            {
                base.FixPadding();
            }
            else
            {
                if (m_MovementAxis == MovementAxis.Horizontal)
                {
                    int fixedPaddingX = Mathf.FloorToInt((m_Viewport.rect.width - m_CellRect.width) / 2);
                    padding.left = padding.right = fixedPaddingX; 
                }
                else 
                {
                    int fixedPaddingY = Mathf.FloorToInt((m_Viewport.rect.height - m_CellRect.height) / 2);
                    padding.top = padding.bottom = fixedPaddingY; 
                }  
            }
        }

        //调整间距（注意只调整滑动方向）
        protected override void FixSpacing()
        {
            if (!m_CellOccupyPage) { return; }

            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                float fixedSpacingX = m_Viewport.rect.width - m_CellRect.width;
                spacing = new Vector2(fixedSpacingX, spacing.y); 
            }
            else
            {
                float fixedSpacingY = m_Viewport.rect.height - m_CellRect.height;
                spacing = new Vector2(spacing.x, fixedSpacingY); 
            }
        }

        protected override void OnStartShow()
        {
            if (m_CellCount > 0) { TryStartCarousel(); }
        }
        #endregion

        #region Drag Handling
        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);

            TryStopSnap();
            TryStopCarousel();
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);

            TryStartSnap();
        }
        #endregion

        #region Snap
        private void TryStartSnap()
        {
            if (!m_Snap) { return; }
            m_SnapCoroutine = StartCoroutine(SnapRoutine());
        }

        private void TryStopSnap()
        {
            if (!m_Snap) { return; }
            if (m_SnapCoroutine == null) { return; }

            StopCoroutine(m_SnapCoroutine);
            m_SnapCoroutine = null;
        }

        private IEnumerator SnapRoutine()
        {
            //Debug.Log($"【SnapRoutine】Snap 进入");
            //如果开启回弹，则需先等待回弹结束
            //loop模式下，回弹理论上不会生效
            if (movementType == MovementType.Elastic)
            {
                if (m_MovementAxis == MovementAxis.Horizontal)
                {
                    float leftPos;
                    float rightPos;
                    float offsetThreshold = 0.1f;

                    if (m_StartCorner == StartCorner.LeftOrUpper) 
                    {
                        leftPos = 0;
                        rightPos = -(m_Content.rect.width - m_Viewport.rect.width);
                    }
                    else 
                    {
                        leftPos = m_Content.rect.width - m_Viewport.rect.width;
                        rightPos = 0;
                    }

                    //当前，Content正从右往左边界回弹
                    if (m_Content.anchoredPosition.x > leftPos)
                    {
                        yield return new WaitUntil(() => { return m_Content.anchoredPosition.x <= leftPos + offsetThreshold; });
                    }
                    //当前，Content正从左往右边界回弹
                    else if (m_Content.anchoredPosition.x < rightPos)
                    {
                        yield return new WaitUntil(() => { return m_Content.anchoredPosition.x >= rightPos - offsetThreshold; });
                    }
                }
                else 
                {
                    float upPos = 0;
                    float downPos = m_Content.rect.height - m_Viewport.rect.height;
                    float offsetThreshold = 0.1f;

                    if (m_StartCorner == StartCorner.LeftOrUpper)
                    {
                        upPos = 0;
                        downPos = m_Content.rect.height - m_Viewport.rect.height;
                    }
                    else
                    {
                        upPos = -(m_Content.rect.height - m_Viewport.rect.height);
                        downPos = 0;
                    }

                    //当前，Content正从下往上边界回弹
                    if (m_Content.anchoredPosition.y < upPos)
                    {
                        yield return new WaitUntil(() => { return m_Content.anchoredPosition.y <= upPos + offsetThreshold; });
                    }
                    //当前，Content正从上往下边界回弹
                    else if (m_Content.anchoredPosition.y > downPos)
                    {
                        yield return new WaitUntil(() => { return m_Content.anchoredPosition.y >= downPos - offsetThreshold; });
                    }
                }
                Debug.Log($"【SnapRoutine】等待回弹结束");
            }

            //如果开启惯性，则需先等待其基本停稳
            if (inertia)
            {
                yield return new WaitUntil(() =>
                {
                    //Debug.Log("scrollRect.velocity: " + scrollRect.velocity);
                    if (m_MovementAxis == MovementAxis.Horizontal) { return Mathf.Abs(velocity.x) < m_SnapWaitScrollSpeed; }
                    else { return Mathf.Abs(velocity.y) < m_SnapWaitScrollSpeed; }
                });
                Debug.Log($"【SnapRoutine】等待惯性移动结束");
            }

            #region 找离Viewport中心最近的那个Cell。
            //注意这里的相对位置计算要求 Content和Viewport没有缩放
            Debug.Assert(m_Content.localScale == Vector3.one);
            Debug.Assert(m_Viewport.localScale == Vector3.one);

            float minDistance = Mathf.Infinity;
            int minDistanceIndex = -1;
            foreach (var t in m_CellRTDict)
            {
                float distanceToViewportCenter;
                if (m_MovementAxis == MovementAxis.Horizontal)
                {
                    //Cell距离Content左边界的位移（向右为正方向）
                    //注意，这里 Cell的 pivot 影响“Cell所处Viewport中心”的概念
                    //若不想影响，可以考虑加个bool选项补偿掉（暂无此需求）。
                    float distanceFromContentLeft = t.Value.anchoredPosition.x;
                    //Cell距离Viewport左边界的位移（向右为正方向）
                    float distanceFromViewportLeft = distanceFromContentLeft + m_Content.anchoredPosition.x;
                    //Cell距离Viewport中心的位移（向右为正方向，若>0：则在中心的右边）
                    distanceToViewportCenter = distanceFromViewportLeft - m_Viewport.rect.width / 2f;
                }
                else 
                {
                    //Cell距离Content上边界的距离（向上为正方向）
                    //注意，这里 Cell的 pivot 影响“Cell所处Viewport中心”的概念，
                    //若不想影响，可以考虑加个bool选项补偿掉（暂无此需求）。
                    float distanceFromContentUp = t.Value.anchoredPosition.y;
                    //Cell距离Viewport上边界的距离（向上为正方向）
                    float distanceFromViewportUp = distanceFromContentUp + m_Content.anchoredPosition.y;
                    //Cell距离Viewport中心的距离（向上为正方向，若<0：在中心的下边）
                    distanceToViewportCenter = distanceFromViewportUp + m_Viewport.rect.height / 2f;
                }

                //Debug.Log($"【SnapRoutine】, index: {t.Key}, distanceToViewportCenter: {distanceToViewportCenter}");

                if (Mathf.Abs(distanceToViewportCenter) < Mathf.Abs(minDistance))
                {
                    minDistance = distanceToViewportCenter;
                    minDistanceIndex = t.Key;
                }
            }
            #endregion

            // 只需将 content 反向移动 minDistance。
            // 但注意 loop 会重置位置，
            // 因此不能“直接计算出目标位置，然后插值”
            // 而是要“每帧持续增加偏移，直到加够量”

            // 计算计划移动距离
            float planMoveDistance = m_MovementAxis == MovementAxis.Horizontal ? -minDistance : minDistance;
            //Debug.Log($"【SnapRoutine】Snap 开始，目标索引:{minDistanceIndex}, 移动距离: {planMoveDistanceX}");

            yield return DoMoveContentPosOnMovementAxis(planMoveDistance, m_SnapSpeed);

            //Debug.Log($"【SnapRoutine】Snap 结束");

            m_CurPage = minDistanceIndex;
            onSnapCompleted?.Invoke();
            TryStartCarousel();
        }
        #endregion

        #region Carousel
        private void TryStartCarousel()
        {
            if (!m_Carousel) { return; }
            m_CarouselCoroutine = StartCoroutine(CarouselRoutine());
        }

        private void TryStopCarousel()
        {
            if (!m_Carousel) { return; }
            if (m_CarouselCoroutine == null) { return; }

            StopCoroutine(m_CarouselCoroutine);
            m_CarouselCoroutine = null;
        }

        private IEnumerator CarouselRoutine()
        {
            // 等待轮播间隔
            yield return new WaitForSeconds(m_CarouselInterval);

            float pageSize = m_MovementAxis == MovementAxis.Horizontal ? m_CellRect.width + spacing.x : m_CellRect.height + spacing.y;
            int direction = m_MovementAxis == MovementAxis.Horizontal ? (m_StartCorner == StartCorner.LeftOrUpper ? -1 : 1) : (m_StartCorner == StartCorner.LeftOrUpper ? 1 : -1);

            // 计算计划移动距离 和 速度倍率
            float planMoveDistance;
            float speedRate = 1f;
            if (m_Loop)
            {
                // 开启循环时，总是向后翻到下一页，但是要注意 conent位置会被重置
                // 这就意味着，逻辑不能是“移动到1页后的目标位置”，而是“位置增加量为1页”。
                // 注意这里的正负符号，决定了翻页方向
                planMoveDistance = pageSize * direction;
            }
            else
            {
                if (m_CurPage < m_CellCount - 1)
                {
                    // 未开启循环时，若当前处于非最后一页，则翻到下一页
                    planMoveDistance = pageSize * direction;
                }
                else
                {
                    // 未开启循环时，若当前处于最后一页，则迅速翻回到第一页
                    planMoveDistance = pageSize * (m_CellCount - 1) * -direction;
                    speedRate = m_CellCount - 1;
                }
            }

            //Debug.Log($"【CarouselRoutine】Carousel开始, 移动距离: {planMoveDistanceX}");

            yield return DoMoveContentPosOnMovementAxis(planMoveDistance, m_CarouselSpeed * speedRate);

            //Debug.Log($"【CarouselRoutine】Carousel 结束");

            //执行对齐，对齐结束后将继续启动轮播
            TryStartSnap();
        }
        #endregion

        private float m_MovedDistance = 0f;
        private IEnumerator DoMoveContentPosOnMovementAxis(float planMoveDistance, float speed)
        {
            //重置累计字段
            m_MovedDistance = 0f;

            //先停止任何惯性速度
            StopMovement();

            //速度标量转向量
            float velocity = speed * Mathf.Sign(planMoveDistance);

            //移动正方向
            Vector2 moveDirection = m_MovementAxis == MovementAxis.Horizontal ? Vector2.right : Vector2.down;

            // 平滑增加位移
            while (Mathf.Abs(m_MovedDistance) < Mathf.Abs(planMoveDistance))
            {
                float addDistance = velocity * Time.deltaTime;  //若要忽略时间缩放，改用 Time.unscaledDeltaTime;

                // 检查是否会超过目标距离
                if (Mathf.Abs(m_MovedDistance + addDistance) >= Mathf.Abs(planMoveDistance))
                {
                    // 直接设置到精确位置，并break
                    float remainingDistance = planMoveDistance - m_MovedDistance;
                    m_Content.anchoredPosition += (moveDirection * remainingDistance);
                    m_MovedDistance = planMoveDistance;
                    break;
                }
                else
                {
                    m_MovedDistance += addDistance;
                    m_Content.anchoredPosition += (moveDirection * addDistance);
                }

                yield return null;
            }
        }
    }
}