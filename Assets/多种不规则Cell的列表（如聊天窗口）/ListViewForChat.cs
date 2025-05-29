using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

//一些注意点
//1、它仍然是一个单行/单列的需求。（一般都是单列竖向滑动的）
//2、Cell的大小由内容决定。
//      所以，没办法不遍历而预先计算Content大小。
//      所以，每次新增或减少Cell。都要重新计算Content大小。
//      即使遍历，要么能在文本显示之前能够直接通过文本内容得到大小，设置数据时直接带上大小（待探索是否可行）；
//      要么就得全部利用Cell模板上的 Text，计算一次（然后反存入数据中）。
//3、Cell的大小计算，不要利用UGUI的 Layout+ContentSizeFilter,这种方式需要至少等到帧末才能处理完。应该自己主动计算和设置Cell大小
//4、如果要用preferredWidth 和 preferredHeight 的方式，是可以在文本设置后立马取到的，不用等到帧结束。
//5、可以支持不同类型的Cell（反正大小不一致了）。
//6、找当前应该显示的Cell的计算，要换一种方式了。但还是不能遍历。
//      考虑：
//      初始时，从第一个开始遍历，找到不该显示的立即return掉，遍历次数由viewport大小及Cell大小决定。计算量不大）
//      滑动时，可从“当前显示的索引集合”处向两边遍历，找到不该显示的立即return掉，遍历次数由相邻两帧之间Content的位移量决定。
//          一般地，正常指触滑动时，两帧之间位移量会很小。这样做没问题。
//          但特别地，如果有跳转到最顶/最下、跳转到某索引处、拖拉进度条等需求时，两帧之间位移量会很大。可能需求另寻它法了。
//----------------------------------------------------------
//7、也可以考虑回不用ScrollRect的方式处理（不要Content、所有Cell的父物体直接在Viewport下）
//      这样可以避免比那里计算Content.
//      但
public class ListViewForChat : MonoBehaviour
{
}
