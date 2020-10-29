using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//要点
// 1、paddingLeft、paddingRight 不需要手动设置了。
// 因为，要保证第一个和最后一个Cell可以处于Viewport的中心。
// 所以，需要强制 paddingLeft = paddingRight = (Viewport宽度 - Cell宽度)/2。
// 这个计算方式对“Cell宽度 <= Viewport宽度”和“Cell宽度 > Viewport宽度”的情况都是适用的。
// 但需要注意，当“Cell宽度 > Viewport宽度”时，paddingLeft 和 paddingRight 均为负。此时左右边界不能完全显示出，PageView只有设置成 Loop 才有意义。

// 2、spacingX 有两种处理方式
// 方式一、仍由用户指定
// 方式二、强制指定为 Viewport宽度 - Cell宽度，即 每个Cell独占一页
// 
//    考虑情况
//    情况一、Cell宽度 <= Viewport宽度，即正常情况。此时可以强制，
//          paddingLeft = paddingRight = (Viewport宽度 - Cell宽度)/2；
//          spacingX = Viewport宽度 - Cell宽度。
//    情况二、Cell宽度 > Viewport宽度，不太正常，看怎么处理了，
//          处理方式一、同情况一中处理，结果：spacingX为负，相邻会重叠压住。（用户自行对该情况负责）  
//          处理方式二、paddingLeft = paddingRight = (Viewport宽度 - Cell宽度)/2； spacingX = 0。能够保证
//          处理方式三、强制检查，禁止这种情况发生。

// 3、自动Snap: 放手后Snap。由当前位置和放手速度共同决定。
//    若放手时中心页未变，则根据放手速度确定是否翻到前一页或下一页。

// 4、spacingX

// 5、loop: 可配置是否循环翻页

// 6、carousel: 可配置轮播

namespace NRatel
{
    //2020.4.2 考虑不用List的方式，而是直接实现滑动接口并用dotween处理移动
    public class PageView : ListView
    {
        ////调整边距
        //protected override void FixPadding()
        //{
        //    paddingLeft = paddingRight = (viewportRT.rect.width - cellPrefabRT.rect.width) / 2;
        //}

        ////调整间距
        //protected override void FixSpacingX()
        //{
        //    spacingX = viewportRT.rect.width - cellPrefabRT.rect.width;
        //}
    }
}


