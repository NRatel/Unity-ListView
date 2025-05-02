using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NRatel
{
    public class UIPageView : UIListView
    {
        [Header("Page Settings")]
        [SerializeField] public bool loop = true;                      //开启循环？
        [SerializeField] public bool cellOccupyPage = false;           //使Cell占用一页（强设将spacing.x）

        [Header("Snap Settings")]
        [SerializeField] public bool snap = false;                     //开启Snap？
        [SerializeField] public float snapSpeed = 500f;                //Snap速度
        [SerializeField] public float snapWaitScrollSpeedX = 50f;      //开启惯性时，等待基本停稳才开始Snap

        [Header("Carousel Settings")]
        [SerializeField] public bool carousel = false;                 //开启轮播？
        [SerializeField] public float carouselInterval = 3f;           //轮播启动间隔
        [SerializeField] public float carouselSpeed = 500f;            //轮播时移动的速度

        public event Action onSnapCompleted;

        private int m_CurPage = 0;
        private Coroutine m_SnapCoroutine;
        private Coroutine m_CarouselCoroutine;

        //核心内容宽度
        private float coreConetontWidth { get { return m_CellRect.width * m_CellCount + spacing.x * (m_CellCount - 1); } }

        //开启loop时，重置宽度
        private float loopResetWidth { get { return (coreConetontWidth + spacing.x) * Mathf.CeilToInt(m_Viewport.rect.width / coreConetontWidth); } }

        //开启loop时，扩展后宽度
        private float expandedContentWidth { get { return coreConetontWidth + loopResetWidth * 4; } }

        protected override void Start()
        {
            base.Start();
            if (m_CellCount > 0) { TryStartCarousel(); }
        }

        #region Override
        //开启loop时，将在初始时偏移Content沿滑动方向的位置，使基本居中，故需反向调整 首个Cell在Content上的初始位置
        //protected override float m_CellStartOffsetOnMovementAxis { get { return (expandedContentWidth - coreConetontWidth) / 2f; } }

        protected override void OnScrollValueChanged(Vector2 delta)
        {
            if (m_CellCount > 0) { TryHandleLoopPos(); }
            base.OnScrollValueChanged(delta);       // 保持原有逻辑
        }

        //调整边距（注意只调整滑动方向）
        protected override void FixPadding()
        {
            if (loop) 
            {
                if (m_MovementAxis == MovementAxis.Horizontal) { padding.left = padding.right = 0; }
                else { padding.top = padding.bottom = 0; }
            }
            else
            {
                int fixedPadding = Mathf.FloorToInt((m_Viewport.rect.width - m_CellRect.width) / 2);
                if (m_MovementAxis == MovementAxis.Horizontal) { padding.left = padding.right = fixedPadding; }
                else { padding.top = padding.bottom = fixedPadding; }  
            }
        }

        //调整间距（注意只调整滑动方向）
        protected override void FixSpacing()
        {
            if (!cellOccupyPage) return;

            float fixedSpacing = m_Viewport.rect.width - m_CellRect.width;
            if (m_MovementAxis == MovementAxis.Horizontal) { spacing = new Vector2(fixedSpacing, spacing.y); }
            else { spacing = new Vector2(spacing.x, fixedSpacing); }
        }

        //计算并设置Content大小
        protected override void SetContentSizeOnMovementAxis()
        {
            if (loop)
            {
                RectTransform.Axis axis;
                float size;
                if (m_MovementAxis == MovementAxis.Horizontal)
                {
                    axis = RectTransform.Axis.Horizontal;
                    size = m_CellCount > 0 ? expandedContentWidth : 0;
                }
                else
                {
                    axis = RectTransform.Axis.Vertical;
                    size = m_CellCount > 0 ? expandedContentWidth : 0;  //todo
                }

                m_Content.SetSizeWithCurrentAnchors(axis, size);
            }
            else 
            {
                base.SetContentSizeOnMovementAxis();
            }
        }

        protected override void SetContentStartPos()
        {
            m_Content.anchoredPosition = new Vector2(-m_CellStartOffsetOnMovementAxis, m_Content.anchoredPosition.y);
        }

        //loop时，认为任意索引都是有效的，以使非 0~m_CellCount 的区域能够显示元素，之后再在 ConvertIndexToValid 转换
        protected override bool IsValidIndex(int index)
        {
            if (loop) { return true; }
            else { return base.IsValidIndex(index); }
        }

        //loop时，将任意索引数转到 [0~m_CellCount-1] 中
        protected override int ConvertIndexToValid(int index)
        {
            if (loop) { return (index % m_CellCount + m_CellCount) % m_CellCount; }
            else { return base.ConvertIndexToValid(index); }
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

        #region Loop
        private void TryHandleLoopPos()
        {
            if (!loop) return;

            //Content初始位置
            float contentStartPosX = -m_CellStartOffsetOnMovementAxis;
            //获取当前位置
            float curContentPosX = m_Content.anchoredPosition.x;
            //Content向左时，Content重置点坐标（初始位置左侧1个重置宽度）
            float leftResetPosX = contentStartPosX - loopResetWidth;
            //Content向右时，Content重置点坐标（初始位置右侧1个重置宽度）
            float rightResetPosX = contentStartPosX + loopResetWidth;

            if (curContentPosX < leftResetPosX)
            {
                m_Content.anchoredPosition += Vector2.right * loopResetWidth;
            }
            //向左滑动时
            else if (curContentPosX > rightResetPosX)
            {
                m_Content.anchoredPosition += Vector2.left * loopResetWidth;
            }
        }
        #endregion

        #region Snap
        private void TryStartSnap()
        {
            if (!snap) { return; }
            m_SnapCoroutine = StartCoroutine(SnapRoutine());
        }

        private void TryStopSnap()
        {
            if (!snap) { return; }
            if (m_SnapCoroutine == null) { return; }

            StopCoroutine(m_SnapCoroutine);
            m_SnapCoroutine = null;
        }

        private IEnumerator SnapRoutine()
        {
            //Debug.Log($"【SnapRoutine】Snap 进入");
            //如果开启回弹，则需先等待回弹结束
            if (movementType == MovementType.Elastic)
            {
                float leftPosX = 0;
                float rightPosX = -(m_Content.rect.width - m_Viewport.rect.width);
                float offsetThreshold = 0.1f;

                //当前正向左边界回弹
                if (m_Content.anchoredPosition.x > leftPosX)
                {
                    yield return new WaitUntil(() => { return m_Content.anchoredPosition.x <= leftPosX + offsetThreshold; });
                }
                //当前正向右边界回弹
                else if (m_Content.anchoredPosition.x < rightPosX)
                {
                    yield return new WaitUntil(() => { return m_Content.anchoredPosition.x > rightPosX - offsetThreshold; });
                }
                //Debug.Log($"【SnapRoutine】等待回弹结束");
            }

            //如果开启惯性，则需先等待其基本停稳
            if (inertia)
            {
                yield return new WaitUntil(() =>
                {
                    //Debug.Log("scrollRect.velocity.x: " + scrollRect.velocity.x);
                    return Mathf.Abs(velocity.x) < snapWaitScrollSpeedX;
                });
                //Debug.Log($"【SnapRoutine】等待惯性停稳");
            }

            #region 找离Viewport中心最近的那个Cell。
            //注意这里的相对位置计算要求 Content和Viewport没有缩放
            Debug.Assert(m_Content.localScale == Vector3.one);
            Debug.Assert(m_Viewport.localScale == Vector3.one);

            float minDistance = Mathf.Infinity;
            int minDistanceIndex = -1;
            foreach (var t in m_CellRTDict)
            {
                //Cell距离Content左边界的距离（标量）
                //注意，这里 Cell的 pivot 影响“Cell所处Viewport中心”的概念，若不想影响，考虑加个bool选项补偿掉。
                float widthFromContentLeft = t.Value.anchoredPosition.x;
                //Cell距离Viewport左边界的距离（标量）
                float widthFromViewportLeft = widthFromContentLeft + m_Content.anchoredPosition.x;
                //Cell距离Viewport中心的距离（矢量，若>0：在中心的右边）
                float distanceToViewportCenter = widthFromViewportLeft - m_Viewport.rect.width / 2f;

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
                planMoveDistanceX = -(m_CellRect.width + spacing.x);
            }
            else
            {
                if (m_CurPage < m_CellCount - 1)
                {
                    // 未开启循环时，若当前处于非最后一页，则翻到下一页
                    planMoveDistanceX = -(m_CellRect.width + spacing.x);
                }
                else
                {
                    // 未开启循环时，若当前处于最后一页，则迅速翻回到第一页
                    planMoveDistanceX = (m_CellRect.width + spacing.x) * (m_CellCount - 1);
                    speedRate = m_CellCount - 1;
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
            StopMovement();  //m_Velocity = Vector2.zero

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
                    m_Content.anchoredPosition += new Vector2(remainingDistance, 0);
                    movedDistanceX = planMoveDistanceX;
                    break;
                }
                else
                {
                    movedDistanceX += addX;
                    m_Content.anchoredPosition += new Vector2(addX, 0);
                }

                yield return null;
            }
        }
    }
}