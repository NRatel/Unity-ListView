using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using NRatel.Fundamental;

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
    public class PageView : ListViewV2  //, IBeginDragHandler, IEndDragHandler
    {
        [Header("Page Settings")]
        [SerializeField] private bool loop = true;
        [SerializeField] private bool carousel = false;
        [SerializeField] private bool fixSpacingX = false;
        [SerializeField] private float snapSpeed = 15f;
        [SerializeField] private float carouselInterval = 3f;
        [SerializeField] private float velocityThreshold = 500f;

        private int currentPage;
        private bool isDragging;
        private Coroutine snapCoroutine;
        private Coroutine carouselCoroutine;

        //核心内容宽度
        private float actualConetontWidth { get { return cellPrefabRT.rect.width * cellCount + spacingX * (cellCount - 1); } }

        //开启loop时，扩展后宽度
        private float expandedContentWidth { get { return actualConetontWidth * 2; } }

        //Content初始偏移，使 content左边界与viewport左边界对齐
        private float contentStartOffsetX { get { return -(expandedContentWidth - actualConetontWidth) / 2f; } }

        //循环阈值
        //private float loopThreshold { get { return 0; } }
        private float loopThreshold { get { return (expandedContentWidth - actualConetontWidth) / 4f; } }


        protected override void Start()
        {
            base.Start();
            //TryStartCarousel();
        }

        #region Override
        protected override void OnScrollValueChanged(Vector2 delta)
        {
            TryHandleLoopPos();               // 新增循环位置处理
            base.OnScrollValueChanged(delta);   // 保持原有逻辑
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
            if (!fixSpacingX) return;
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
            contentRT.anchoredPosition = new Vector2(0 + contentStartOffsetX, 0);
        }

        //认为任意索引都是有效的
        protected override bool IsValidIndex(int index)
        {
            return true;
        }

        //使索引有效（将任意索引数转到 [0~cellCount-1] 中）
        protected override int ValidateIndex(int index)
        {
            return (index % cellCount + cellCount) % cellCount;
        }

        //计算Cell的X坐标
        protected override float CalcCellPosX(int index)
        {
            return base.CalcCellPosX(index) + (loop ? -contentStartOffsetX : 0);
        }
        #endregion

        //#region Drag Handling
        //public void OnBeginDrag(PointerEventData eventData)
        //{
        //    isDragging = true;
        //    //TryStopSnapping();
        //    //TryStopCarousel();
        //}

        //public void OnEndDrag(PointerEventData eventData)
        //{
        //    isDragging = false;
        //    //SnapToNearestPage();
        //    //TryStartCarousel();
        //}
        //#endregion

        //#region Snap
        //private void SnapToNearestPage()
        //{
        //    int direction = CalculateDirection();
        //    currentPage = GetValidPageIndex(currentPage + direction);

        //    float targetPosition = CalculatePagePosition(currentPage);
        //    snapCoroutine = StartCoroutine(SmoothSnap(targetPosition));
        //}

        //private int CalculateDirection()
        //{
        //    //float velocity = scrollRect.velocity.x;
        //    //if (Mathf.Abs(velocity) > velocityThreshold)
        //    //    return (int)Mathf.Sign(velocity);

        //    //float pageProgress = (contentRT.anchoredPosition.x - expanedContentWidthForLoop) % pageWidth / pageWidth;
        //    //return pageProgress > 0.5f ? 1 : -1;

        //    return -1;
        //}

        //private IEnumerator SmoothSnap(float targetX)
        //{
        //    Vector2 startPos = contentRT.anchoredPosition;
        //    Vector2 targetPos = new Vector2(targetX, startPos.y);
        //    float progress = 0;

        //    while (progress < 1)
        //    {
        //        progress = Mathf.Clamp01(progress + Time.deltaTime * snapSpeed);
        //        contentRT.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);
        //        yield return null;
        //    }

        //    TryHandleLoopPos();
        //}

        //private void TryStopSnapping()
        //{
        //    if (snapCoroutine == null) { return; }

        //    StopCoroutine(snapCoroutine);
        //    snapCoroutine = null;
        //}
        //#endregion

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

        private int GetValidPageIndex(int page)
        {
            if (loop) return (page % cellCount + cellCount) % cellCount;
            return Mathf.Clamp(page, 0, cellCount - 1);
        }
        #endregion

        //#region Carousel
        //private void TryStartCarousel()
        //{
        //    if (!carousel) { return; }
        //    carouselCoroutine = StartCoroutine(CarouselRoutine());
        //}

        //private void TryStopCarousel()
        //{
        //    if (!carousel) { return; }
        //    if (carouselCoroutine == null) { return; }

        //    StopCoroutine(carouselCoroutine);
        //    carouselCoroutine = null;
        //}

        //private IEnumerator CarouselRoutine()
        //{
        //    while (true)
        //    {
        //        yield return new WaitForSeconds(carouselInterval);
        //        if (!isDragging && snapCoroutine == null)
        //        {
        //            currentPage = GetValidPageIndex(currentPage + 1);
        //            SnapToNearestPage();
        //        }
        //    }
        //}
        //#endregion

        //#region Position Calculations
        //private float CalculatePagePosition(int page)
        //{
        //    //if (loop) return expanedContentWidthForLoop + page * pageWidth;
        //    //return Mathf.Clamp(page * pageWidth, 0, (cellCount - 1) * pageWidth);
        //    return -1;
        //}
        //#endregion

        //#region Utility Methods
        //public void JumpTo(int pageIndex)
        //{
        //    currentPage = GetValidPageIndex(pageIndex);
        //    contentRT.anchoredPosition = new Vector2(CalculatePagePosition(currentPage), 0);
        //    TryHandleLoopPos();
        //}
        //#endregion
    }
}

