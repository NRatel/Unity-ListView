using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//由数据驱动
//是否采用脏标记模式？ 
//  优点：可以一次处理多个操作，节省性能
//  缺点：可能变成异步，Content不能马上计算，某操作后进行Jump会出错或无效。
//如果不使用脏标记，考虑定义操作list接口，也可以一次处理。
public partial class ListView
{
}
        