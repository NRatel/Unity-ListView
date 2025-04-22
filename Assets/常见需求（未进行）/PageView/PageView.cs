using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using NRatel.Fundamental;
using System;

//要点：
// 1、自动吸附对齐（Snap）
//      滑动放手后，要自动将Cell吸附对齐到Viewport中心，没有自动吸附对齐，就没有Page的概念。
//      滑动放手后，将吸附到哪一页，可由“放手时位置”由“放手时速度”共同决定。
//      如将策略定为：
//          若放手时中心页已变，则吸附到新页。
//          若放手时中心页未变，则根据放手速度矢量决定是否翻页（超过阈值则翻页）。

// 2、是否循环翻页（Loop）
//      可配置 是/否

// 3、是否自动轮播（Carousel）
//      可配置 是/否，轮播翻页速度、页停留时间

// 4、页宽。
//      将 Cell宽度 作为页宽，而非 Viewport宽度。
//      因为，需要支持 Viewport 中同时出现多个Cell。此时最靠近Viewport中心的那个与Viewport中心对齐。
//      注意，无需支持 Cell宽度 > Viewport宽度。因为此时 Viewport中无法完整展示一个Cell，也难翻到下一页（暂无实际需求）。

// 5、paddingLeft、paddingRight。
//      根据是否开启loop，设定不同。
//      ①、不开启loop时，
//          必须固定为：paddingLeft = paddingRight = (Viewport宽度 - Cell宽度)/2。
//          因为要保证第一个和最后一个Cell能够处于Viewport的中心。
//          这个计算方式对“Cell宽度 <= Viewport宽度”和“Cell宽度 > Viewport宽度”的情况都是适用的。
//      ②、开启loop时，
//          保持用户指定值。但应满足：？
//          因为循环显示，所有没有“边界”，自然就没有“边距”可言。
//          但实际上，它还影响首页初始位置。

// 6、spacingX
//      有两种选择
//      ①、保持用户指定值。但应满足：spacingX <= Viewport宽度 - Cell宽度。
//      ②、直接强设为：spacingX = Viewport宽度 - Cell宽度，即 每个Cell独占一页

// 7、cell 数量要求
//      根据是否开启loop，要求不同。
//      ①、不开启loop时，
//          无要求
//      ②、开启loop时，
//          需保证：同一时刻下，Viewport中不会出现多个同一Cell。

namespace NRatel
{
    public class PageView : ListViewV2, IBeginDragHandler, IEndDragHandler
    {
        [Header("Page Settings")]
        [SerializeField] private bool loop = true;                      //开启循环？
        [SerializeField] private bool cellOccupyPage = false;           //使Cell占用一页（强设将spacingX）

        [Header("Snap Settings")]
        [SerializeField] private float snapSpeed = 500f;                //Snap速度
        [SerializeField] private float snapWaitScrollVelocityX = 50f;   //开启惯性时，等待基本停稳才开始Snap

        [Header("Carousel Settings")]
        [SerializeField] private bool carousel = false;                 //开启轮播？
        [SerializeField] private float carouselInterval = 3f;           //轮播启动间隔
        [SerializeField] private float carouselSpeed = 500f;            //轮播时移动的速度

        public event Action onSnapCompleted;

        private int curPage = 0;

        private Coroutine snapCoroutine;
        private Coroutine carouselCoroutine;

        //核心内容宽度
        private float actualConetontWidth { get { return cellPrefabRT.rect.width * cellCount + spacingX * (cellCount - 1); } }

        //开启loop时，扩展后宽度
        private float expandedContentWidth { get { return actualConetontWidth * 2; } }

        //Content初始偏移，使 content左边界与viewport左边界对齐
        private float contentStartOffsetX { get { return -(expandedContentWidth - actualConetontWidth) / 2f; } }

        //循环阈值
        private float loopThreshold { get { return (expandedContentWidth - actualConetontWidth) / 4f; } }


        protected override void Start()
        {
            base.Start();
            TryStartCarousel();
        }

        #region Override
        protected override void OnScrollValueChanged(Vector2 delta)
        {
            TryHandleLoopPos();                     // 新增循环位置处理
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

        //调整视口容差
        protected override void FixViewportOffset()
        {
            if (loop)
            {
                viewportOffsetLeft = Mathf.Abs(contentStartOffsetX);
                viewportOffsetRight = viewportOffsetLeft;
            }
            else
            {
                base.FixViewportOffset(); 
            }
        }

        //计算并设置Content大小
        protected override void CalcAndSetContentSize()
        {
            if (loop)
            {
                contentWidth = expandedContentWidth;
                contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
            }
            else
            {
                base.CalcAndSetContentSize();
            }
        }

        //设置初始位置
        protected override void SetContentStartPos()
        {
            if (loop) { contentRT.anchoredPosition = new Vector2(contentStartOffsetX, contentRT.anchoredPosition.y); }
            else { base.SetContentStartPos(); }
        }

        //loop时，认为任意索引都是有效的
        protected override bool IsValidIndex(int index)
        {
            if (loop) { return true; }
            else { return base.IsValidIndex(index); }
        }

        //loop时，将任意索引数转到 [0~cellCount-1] 中
        protected override int ValidateIndex(int index)
        {
            if (loop) { return (index % cellCount + cellCount) % cellCount; }
            else { return base.ValidateIndex(index); }
        }

        //计算Cell的X坐标
        protected override float CalcCellPosX(int index)
        {
            return base.CalcCellPosX(index) + (loop ? -contentStartOffsetX : 0);
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

            // 获取当前位置
            float curContentPosX = contentRT.anchoredPosition.x;

            //初始 Content坐标 contentStartPosX

            //1、列表核心内容的左边界 处于 Viewport左边界时，Content的X坐标
            float leftContentPosX = contentStartOffsetX;
            //2、列表核心内容的右边界 处于 Viewport右边界时，Content的X坐标
            float rightContentPosX = contentStartOffsetX - (actualConetontWidth - viewportRT.rect.width);

            //向右滑动时，左边界坐标向左偏移loopThreshold
            if (curContentPosX > leftContentPosX + loopThreshold)
            {
                contentRT.anchoredPosition -= Vector2.right * (actualConetontWidth + spacingX);
            }
            //向左滑动时，右边界坐标向右偏移loopThreshold
            else if (curContentPosX < rightContentPosX - loopThreshold)
            {
                contentRT.anchoredPosition += Vector2.right * (actualConetontWidth + spacingX);
            }
        }
        #endregion

        #region Snap
        private void TryStartSnap()
        {
            snapCoroutine = StartCoroutine(SnapRoutine());
        }

        private void TryStopSnap()
        {
            if (snapCoroutine == null) { return; }

            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }

        private IEnumerator SnapRoutine()
        {
            //如果开启惯性/边缘回弹，则需先等待其基本停稳
            //考虑：是否应该强制关闭回弹？或者开启轮播时应该关闭回弹？
            if (scrollRect.inertia || scrollRect.movementType == ScrollRect.MovementType.Elastic)
            {
                yield return new WaitUntil(() => 
                {
                    //Debug.Log("scrollRect.velocity.x: " + scrollRect.velocity.x);
                    return Mathf.Abs(scrollRect.velocity.x) < snapWaitScrollVelocityX; 
                });
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
            #endregion

            // 只需将 content 反向移动 minDistance。
            // 但注意 loop 会重置位置，
            // 因此不能“直接计算出目标位置，然后插值”
            // 而是要“每帧持续增加偏移，直到加够量”

            // 计算计划移动距离
            float planMoveDistanceX = -minDistance;
            Debug.Log($"【SnapRoutine】Snap 开始，目标索引:{minDistanceIndex}, 移动距离: {planMoveDistanceX}");

            yield return DoMoveContentPosX(planMoveDistanceX, snapSpeed);

            Debug.Log($"【SnapRoutine】Snap 结束");

            curPage = minDistanceIndex;
            onSnapCompleted?.Invoke();
            TryStartCarousel();
        }
        #endregion

        #region Carousel
        private void TryStartCarousel()
        {
            if (!carousel) { return; }
            carouselCoroutine = StartCoroutine(CarouselRoutine());
        }

        private void TryStopCarousel()
        {
            if (!carousel) { return; }
            if (carouselCoroutine == null) { return; }

            StopCoroutine(carouselCoroutine);
            carouselCoroutine = null;
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
                if (curPage < cellCount - 1)
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

            Debug.Log($"【CarouselRoutine】Carousel开始, 移动距离: {planMoveDistanceX}");

            yield return DoMoveContentPosX(planMoveDistanceX, carouselSpeed * speedRate);

            Debug.Log($"【CarouselRoutine】Carousel 结束");

            //执行对齐，对齐结束后将继续启动轮播
            TryStartSnap();
        }
        #endregion

        private float movedDistanceX = 0f;
        private IEnumerator DoMoveContentPosX(float planMoveDistanceX, float speed)
        {
            //先停止任何惯性速度
            scrollRect.velocity = Vector2.zero;

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

