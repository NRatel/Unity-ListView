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
    public class PageView : ListViewV2  //, IBeginDragHandler, IEndDragHandler
    {
        [Header("Page Settings")]
        [SerializeField] private bool loop = true;
        [SerializeField] private bool carousel = false;
        [SerializeField] private bool fixSpacingX = false;
        [SerializeField] private float snapSpeed = 15f;
        [SerializeField] private float carouselInterval = 3f;
        [SerializeField] private float velocityThreshold = 500f;

        private bool isDragging;
        private Coroutine snapCoroutine;
        private Coroutine carouselCoroutine;

        public event Action onSnapCompleted;

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
            if (isSnapping) { TryHandleLoopPos(); } // 新增循环位置处理
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
            if (loop) { contentRT.anchoredPosition = new Vector2(0 + contentStartOffsetX, 0); ; }
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
            isDragging = true;
            TryStopSnap();
            TryStopCarousel();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
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

        //private IEnumerator SnapRoutine()
        //{
        //    //todo
        //    //移动 contentRT.anchoredPosition 的 x值，使当前 Content中将离viewport中心最近的那个cell，在移动后处于viewport中心。
        //    yield return null;

        //    //结束后，回调事件并开始轮播
        //    onSnapCompleted?.Invoke();
        //    TryStartCarousel();
        //}

        // 新增字段
        private int currentPage = 0;
        private bool isSnapping = false;

        // Snap协程实现
        private IEnumerator SnapRoutine()
        {
            isSnapping = true;

            float viewportWidth = viewportRT.rect.width;
            float cellWidth = cellPrefabRT.rect.width;
            float cellStep = cellWidth + spacingX;

            // 计算视口中心在Content空间中的位置
            float viewportCenterInContent = -contentRT.anchoredPosition.x + viewportWidth / 2f;

            // 确定目标索引
            int targetIndex = loop ?
                Mathf.RoundToInt((viewportCenterInContent - cellWidth / 2f) / cellStep) :
                Mathf.RoundToInt((viewportCenterInContent - paddingLeft - cellWidth / 2f) / cellStep);

            targetIndex = Mathf.Clamp(targetIndex, 0, cellCount - 1);

            // 计算目标位置
            float cellCenter = loop ?
                targetIndex * cellStep + cellWidth / 2f :
                paddingLeft + targetIndex * cellStep + cellWidth / 2f;

            Vector2 targetPos = new Vector2(
                viewportWidth / 2f - cellCenter,
                contentRT.anchoredPosition.y
            );

            // 平滑移动动画
            while (Vector2.Distance(contentRT.anchoredPosition, targetPos) > 0.1f)
            {
                contentRT.anchoredPosition = Vector2.Lerp(
                    contentRT.anchoredPosition,
                    targetPos,
                    snapSpeed * Time.deltaTime
                );
                yield return null;
            }

            contentRT.anchoredPosition = targetPos;
            currentPage = ValidateIndex(targetIndex);
            isSnapping = false;

            TryHandleLoopPos();
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
            //todo
            //每隔N秒，翻到下一个
            yield return null;
        }
        #endregion
    }
}

