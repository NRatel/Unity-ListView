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
    public class PageView : ListViewV2
    {
        //是否开启循环翻页
        [SerializeField]
        public bool loop = false;

        //是否开启轮播
        [SerializeField]
        public bool carousel = false;

        //是否强设 spacingX
        [SerializeField]
        public bool fixSpacingX = false;

        //轮播翻页速度
        [SerializeField]
        private float carouselSpeed = 1f;

        //轮播每页停留时间
        [SerializeField]
        private float carouselStay = 1f;

        //页宽
        private float pageWidth { get { return cellPrefabRT.rect.width; } }

        //调整边距
        protected override void FixPadding()
        {
            if (!loop) { return; }
            paddingLeft = paddingRight = (viewportRT.rect.width - cellPrefabRT.rect.width) / 2;
        }

        //调整X间距
        protected override void FixSpacingX()
        {
            if (!fixSpacingX) { return; }
            spacingX = viewportRT.rect.width - cellPrefabRT.rect.width;
        }
    }
}


