//https://github.com/NRatel/Unity-ListView

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NRatel
{
    // 轴方向（2种，水平/竖直）和 开始排布的角落（4种，上左/上右/下左/下右），可以确定出以下Grid结果：
    // 1、元素延伸方向（4种，从左往右/从右往左/从上往下/从下往上）（将在运行时强制修改Contetnt的起始中心点和锚点）
    // 2、元素排布轨迹（4*2种，4个延伸方向*2侧）
    public class UIListView : UIScrollRect
    {
        public enum Side
        {
            LeftOrUpper = 0,          //左或上
            RightOrLower = 1,         //右或下
        }

        public enum Alignment
        {
            LeftOrUpper = 0,
            CenterOrMiddle = 1,
            RightOrLower = 2,
        }

        [SerializeField] protected Side m_StartSide = Side.LeftOrUpper;
        public Side startSide { get { return m_StartSide; } set { SetProperty(ref m_StartSide, value); } }

        [SerializeField] protected Vector2 m_Spacing = Vector2.zero;
        public Vector2 spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }

        [SerializeField] protected Alignment m_ChildAlignment = Alignment.LeftOrUpper;
        public Alignment childAlignment { get { return m_ChildAlignment; } set { SetProperty(ref m_ChildAlignment, value); } }

        [SerializeField] protected RectOffset m_Padding = new RectOffset();
        public RectOffset padding { get { return m_Padding; } set { SetProperty(ref m_Padding, value); } }

        [SerializeField] protected bool m_Loop = true;                      //开启循环？
        public bool loop { get { return m_Loop; } set { SetProperty(ref m_Loop, value); } }

        protected DrivenRectTransformTracker m_Tracker;

        protected int m_ActualCellCountX;
        protected int m_ActualCellCountY;
        protected Vector2 m_RequiredSpace;
        protected Vector2 m_CellStartOffset;

        protected Dictionary<int, RectTransform> m_CellRTDict;                      //index-Cell字典    
        protected Stack<RectTransform> m_UnUseCellRTStack;                          //空闲Cell堆栈
        protected List<KeyValuePair<int, RectTransform>> m_CellRTListForSort;       //Cell列表用于辅助Sbling排序

        protected List<int> m_OldIndexes;                                           //旧的索引集合
        protected List<int> m_NewIndexes;                                           //新的索引集合
        protected List<int> m_AppearIndexes;                                        //将要出现的索引集合   //使用List而非单个，可以支持Content位置跳变
        protected List<int> m_DisAppearIndexes;                                     //将要消失的索引集合   //使用List而非单个，可以支持Content位置跳变
        protected List<int> m_StayIndexes;                                          //保持的索引集合       //使用List而非单个，可以支持Content位置跳变

        protected Rect m_CellRect;                                                  //Cell Rect（必需）
        protected Vector2 m_CellPivot;                                              //Cell中心点（必需）
        protected Func<int, RectTransform> m_OnCreateCell;                          //创建Cell的方法（必需）
        protected Action<int> m_OnShowCell;                                         //展示Cell的方法（出现/刷新时回调）（必需）

        protected int m_CellCount;                                                  //显示数量

        #region ForLoop
        //核心内容大小（滑动轴方向）
        protected float m_CoreConetontSizeOnMovementAxis
        {
            get
            {
                if (m_MovementAxis == MovementAxis.Horizontal)
                {
                    return m_CellRect.width * m_CellCount + spacing.x * (m_CellCount - 1);
                }
                else
                {
                    return m_CellRect.height * m_CellCount + spacing.y * (m_CellCount - 1);
                }
            }
        }

        //开启loop时，重置大小（滑动轴方向）
        protected float m_LoopResetSizeOnMovementAxis
        {
            get
            {
                if (m_MovementAxis == MovementAxis.Horizontal)
                {
                    return (m_CoreConetontSizeOnMovementAxis + spacing.x) * Mathf.CeilToInt(m_Viewport.rect.width / m_CoreConetontSizeOnMovementAxis);
                }
                else
                {
                    return (m_CoreConetontSizeOnMovementAxis + spacing.y) * Mathf.CeilToInt(m_Viewport.rect.height / m_CoreConetontSizeOnMovementAxis);
                }
            }
        }

        //开启loop时，扩展后宽度
        protected float m_ExpandedContentSizeOnMovementAxis
        {
            get
            {
                return m_CoreConetontSizeOnMovementAxis + m_LoopResetSizeOnMovementAxis * 4;
            }
        }

        //开启loop时，Cell在Content的滑动轴上的起始位置偏移
        protected float m_CellStartOffsetOnMovementAxis
        {
            get
            {
                if (m_Loop)
                {
                    int sign;
                    if (m_MovementAxis == MovementAxis.Horizontal)
                    {
                        sign = (m_StartSide == Side.LeftOrUpper ? 1 : -1);
                    }
                    else
                    {
                        sign = (m_StartSide == Side.LeftOrUpper ? -1 : 1);
                    }
                    return (m_ExpandedContentSizeOnMovementAxis - m_CoreConetontSizeOnMovementAxis) / 2f * sign;
                }
                else
                {
                    return 0f;
                }
            }
        }
        #endregion

        protected override void Awake()
        {
            m_CellRTDict = new Dictionary<int, RectTransform>();
            m_UnUseCellRTStack = new Stack<RectTransform>();
            m_CellRTListForSort = new List<KeyValuePair<int, RectTransform>>();

            m_OldIndexes = new List<int>();
            m_NewIndexes = new List<int>();
            m_AppearIndexes = new List<int>();
            m_DisAppearIndexes = new List<int>();
            m_StayIndexes = new List<int>();

            this.onValueChanged.AddListener(OnScrollValueChanged);

            ResetTracker();
            ResetContentRT();
        }

        //从Cell模板上取rect和pivot进行初始化，并以GameObject.Instantiate实例化Cell模板的方式创建Cell
        public void Init(RectTransform templateCellRT, Action<int> onShowCell)
        {
            this.m_CellRect = templateCellRT.rect;
            this.m_CellPivot = templateCellRT.pivot;
            this.m_OnCreateCell = (_) => { return GameObject.Instantiate<GameObject>(templateCellRT.gameObject).GetComponent<RectTransform>(); };
            this.m_OnShowCell = onShowCell;
        }

        //从Cell模板上取rect和pivot进行初始化，自行指定创建Cell的方法
        public void Init(RectTransform templateCellRT, Func<int, RectTransform> onCreateCell, Action<int> onShowCell)
        {
            this.m_CellRect = templateCellRT.rect;
            this.m_CellPivot = templateCellRT.pivot;
            this.m_OnCreateCell = onCreateCell;
            this.m_OnShowCell = onShowCell;
        }

        //用rect和pivot初始化，自行指定创建Cell的方法
        public void Init(Rect cellRect, Vector2 cellPivot, Func<int, RectTransform> onCreateCell, Action<int> onShowCell)
        {
            this.m_CellRect = cellRect;
            this.m_CellPivot = cellPivot;
            this.m_OnCreateCell = onCreateCell;
            this.m_OnShowCell = onShowCell;
        }

        /// <summary>
        /// 开始显示一个新的GridView，
        /// 也可用于元素数量变化时的全部刷新
        /// 也可用于传0清空
        /// </summary>
        /// <param name="count">要显示的数量</param>
        /// <param name="stayPos">数量变化时是否尽量保持位置不变，若轴向发生变化，则必须传false</param>
        public void StartShow(int count, bool stayPos = true)
        {
            Debug.Assert(m_OnCreateCell != null, "请先初始化");

            this.m_CellCount = count;

            if (!stayPos) { ResetContentRT(); }

            RefreshAll();

            OnStartShow();
        }

        /// <summary>
        /// 用于元素数量未变时（仅数据变化）的全部刷新
        /// （注意，若已知某个索引变化，且数量未变，应使用 TryRefreshCellRT 刷新单个）
        /// </summary>
        public void RefreshAll()
        {
            FixPadding();
            FixSpacing();
            CalcCellCountOnNaturalAxis();
            CalculateRequiredSpace();
            SetContentSizeOnMovementAxis();
            CalculateCellStartOffset();
            SetContentStartPos();

            CalcIndexes();
            DisAppearCells();
            AppearCells();
            RefreshStayCells();
            CalcAndSetCellsSblingIndex();
        }

        //开始展示回调，给子类使用
        protected virtual void OnStartShow() { }

        //尝试刷新索引对应CellRT，若未在显示则忽略
        public void TryRefreshCellRT(int index)
        {
            if (!m_CellRTDict.ContainsKey(index)) { return; }
            m_OnShowCell?.Invoke(index);                        //Cell出现/刷新回调
        }

        //刷新索引对应CellRT
        public void RefreshCellRT(int index)
        {
            Debug.Assert(m_CellRTDict.ContainsKey(index));
            m_OnShowCell?.Invoke(index);                        //Cell出现/刷新回调
        }

        //尝试获取索引对应CellRT，若未在显示则返回false
        public bool TryGetCellRT(int index, out RectTransform cellRT)
        {
            return m_CellRTDict.TryGetValue(index, out cellRT);
        }

        //取索引对应CellRT
        public RectTransform GetCellRT(int index)
        {
            Debug.Assert(m_CellRTDict.ContainsKey(index));
            return m_CellRTDict[index];
        }

        //索引对应Cell当前是否正在显示
        public bool IsCellRTShowing(int index)
        {
            return m_CellRTDict.ContainsKey(index);
        }

        /// <summary>
        /// 索引对应Cell跳转到0索引Cell的位置
        /// </summary>
        /// <param name="index">目标索引</param>
        /// <param name="immediately">是否立刻生效，否则要等到LateUpdate中计算</param>
        public void JumpTo(int index, bool immediately = false)
        {
            Vector2 cellPos0 = GetCellPos(0);
            Vector2 cellPosI = GetCellPos(index);
            Vector2 deltaXY = new Vector2(Mathf.Abs(cellPosI.x - cellPos0.x), Mathf.Abs(cellPosI.y - cellPos0.y)); //index相对0位置，x、y 距离差
            Vector2 limitXY = new Vector2(Mathf.Max(m_Content.rect.size.x - m_Viewport.rect.size.x, 0), Mathf.Max(m_Content.rect.size.y - m_Viewport.rect.size.y, 0)); //x、y 限制大小，Mathf.Max同时兼容“Content比Viewport小”和“Content比Viewport大”两种情况
            Vector2 jumpToXY = new Vector2(Mathf.Min(deltaXY.x, limitXY.x), Mathf.Min(deltaXY.y, limitXY.y)); //不超过限制大小

            //Debug.Log("deltaXY: " + deltaXY);
            //Debug.Log("limitXY: " + limitXY);
            //Debug.Log("jumpToXY: " + jumpToXY);

            m_Content.anchoredPosition = new Vector2(
                m_MovementAxis == MovementAxis.Horizontal ? jumpToXY.x : m_Content.anchoredPosition.x,
                m_MovementAxis == MovementAxis.Horizontal ? m_Content.anchoredPosition.y : jumpToXY.y
            );

            if (immediately) { OnScrollValueChanged(Vector2.zero); }
        }

        protected virtual void OnScrollValueChanged(Vector2 delta)
        {
            if (m_CellCount <= 0) { return; }

            CalcIndexes();
            DisAppearCells();
            AppearCells();
            CalcAndSetCellsSblingIndex();
        }

        //根据轴向和起始角落，重置Content的锚点、中心点、位置和大小
        protected void ResetContentRT()
        {
            // 根据轴向和起始角落设置锚点、中心点
            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                int sideX = (int)m_StartSide % 2;  //0：左， 1右
                m_Content.anchorMin = new Vector2(sideX, 0);
                m_Content.anchorMax = new Vector2(sideX, 1);
                m_Content.pivot = new Vector2(sideX, 0.5f);
            }
            else
            {
                int sideY = (int)m_StartSide % 2;  //0：上， 1下（注意与UIGridView的Corner计算不同）
                m_Content.anchorMin = new Vector2(0, 1 - sideY);
                m_Content.anchorMax = new Vector2(1, 1 - sideY);
                m_Content.pivot = new Vector2(0.5f, 1 - sideY);
            }

            // 位置归0
            m_Content.anchoredPosition = Vector2.zero;

            // 大小重置为与Viewport相同
            m_Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_Viewport.rect.width);
            m_Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_Viewport.rect.height);
        }

        protected void ResetTracker()
        {
            m_Tracker.Clear();
        }

        //调整边距
        protected virtual void FixPadding()
        {
            if (!m_Loop) { return; }

            if (m_MovementAxis == MovementAxis.Horizontal) { padding.left = padding.right = 0; }
            else { padding.top = padding.bottom = 0; }
        }

        //调整间距
        protected virtual void FixSpacing() { }

        //计算直观行列数（自然坐标轴上）
        protected void CalcCellCountOnNaturalAxis()
        {
            this.m_ActualCellCountX = m_MovementAxis == MovementAxis.Horizontal ? m_CellCount : 1;
            this.m_ActualCellCountY = m_MovementAxis == MovementAxis.Vertical ? m_CellCount : 1;
        }

        //计算实际需要的空间大小（不含padding） 及 在这个空间上第一个元素所在的位置
        protected void CalculateRequiredSpace()
        {
            Vector2 requiredSpace = new Vector2(
                m_ActualCellCountX * m_CellRect.size.x + (m_ActualCellCountX - 1) * spacing.x,
                m_ActualCellCountY * m_CellRect.size.y + (m_ActualCellCountY - 1) * spacing.y
            );
            this.m_RequiredSpace = requiredSpace;
        }

        //设置滑动轴方向的Content大小
        protected virtual void SetContentSizeOnMovementAxis()
        {
            RectTransform.Axis axis;
            float size;
            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                axis = RectTransform.Axis.Horizontal;
                size = m_Loop ? m_ExpandedContentSizeOnMovementAxis : m_RequiredSpace.x + padding.horizontal;
            }
            else
            {
                axis = RectTransform.Axis.Vertical;
                size = m_Loop ? m_ExpandedContentSizeOnMovementAxis : m_RequiredSpace.y + padding.vertical; //todo!!!
            }

            m_Content.SetSizeWithCurrentAnchors(axis, size);
        }

        //计算Cell起始Offset
        //注意：使 元素对齐方式只影响 非滑动轴方向
        //因为滑动轴方向 Content大小由元素数决定。（不同于UGUI的Layout,滑动方向是自由大小）
        protected void CalculateCellStartOffset()
        {
            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                m_CellStartOffset = new Vector2(
                    padding.left + Mathf.Abs(m_CellStartOffsetOnMovementAxis),
                    GetCellStartOffset((int)MovementAxis.Vertical, m_RequiredSpace.y)
                );
            }
            else
            {
                m_CellStartOffset = new Vector2(
                    GetCellStartOffset((int)MovementAxis.Horizontal, m_RequiredSpace.x),
                    padding.top + Mathf.Abs(m_CellStartOffsetOnMovementAxis)
                );
            }
        }

        protected virtual void SetContentStartPos()
        {
            m_Content.anchoredPosition = Vector2.zero;

            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                m_Content.anchoredPosition = new Vector2(-m_CellStartOffsetOnMovementAxis, m_Content.anchoredPosition.y);
            }
            else
            {
                m_Content.anchoredPosition = new Vector2(m_Content.anchoredPosition.x, -m_CellStartOffsetOnMovementAxis);
            }
        }

        #region Loop
        protected override void TryAdjustContentAnchoredPosition()
        {
            if (!m_Loop) return;

            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                //Content初始位置
                float contentStartPosX = -m_CellStartOffsetOnMovementAxis;
                //获取当前位置
                float curContentPosX = m_Content.anchoredPosition.x;
                //Content向左时，Content重置点坐标（初始位置左侧1个重置宽度）
                float leftResetPosX = contentStartPosX - m_LoopResetSizeOnMovementAxis;
                //Content向右时，Content重置点坐标（初始位置右侧1个重置宽度）
                float rightResetPosX = contentStartPosX + m_LoopResetSizeOnMovementAxis;

                if (curContentPosX < leftResetPosX)
                {
                    m_Content.anchoredPosition += Vector2.right * m_LoopResetSizeOnMovementAxis;

                    //更新，以使本帧 LateUpdate中 计算的 m_Velocity 不会因位置剧变而剧变
                    m_PrevPosition += Vector2.right * m_LoopResetSizeOnMovementAxis;

                    //更新，以使 OnDrag 中，Content位置跟随鼠标移动时，不反复触发此“位置超过一页的重置逻辑”，否则下一帧m_PrevPosition又将执行一次偏移（上一行代码），还是会导致速度剧变
                    m_ContentStartPosition += Vector2.right * m_LoopResetSizeOnMovementAxis;
                }
                else if (curContentPosX > rightResetPosX)
                {
                    m_Content.anchoredPosition += Vector2.left * m_LoopResetSizeOnMovementAxis;

                    //更新，以使本帧 LateUpdate中 计算的 m_Velocity 不会因位置剧变而剧变
                    m_PrevPosition += Vector2.left * m_LoopResetSizeOnMovementAxis;

                    //更新，以使 OnDrag 中，Content位置跟随鼠标移动时，不反复触发此“位置超过一页的重置逻辑”，否则下一帧m_PrevPosition又将执行一次偏移（上一行代码），还是会导致速度剧变
                    m_ContentStartPosition += Vector2.left * m_LoopResetSizeOnMovementAxis;

                    //Debug.Log($"1111111111111 Time.frameCount: {Time.frameCount}, m_Draging: {m_Dragging}, m_Content.anchoredPosition.x: {m_Content.anchoredPosition.x}, m_PrevPosition.x: {m_PrevPosition.x}");
                }
            }
            else
            {
                float contentStartPosY = -m_CellStartOffsetOnMovementAxis;
                float curContentPosY = m_Content.anchoredPosition.y;

                float topResetPosY = contentStartPosY + m_LoopResetSizeOnMovementAxis;
                float bottomResetPosY = contentStartPosY - m_LoopResetSizeOnMovementAxis;

                if (curContentPosY > topResetPosY)
                {
                    m_Content.anchoredPosition += Vector2.down * m_LoopResetSizeOnMovementAxis;
                    m_PrevPosition += Vector2.down * m_LoopResetSizeOnMovementAxis;
                    m_ContentStartPosition += Vector2.down * m_LoopResetSizeOnMovementAxis;
                }
                else if (curContentPosY < bottomResetPosY)
                {
                    m_Content.anchoredPosition += Vector2.up * m_LoopResetSizeOnMovementAxis;
                    m_PrevPosition += Vector2.up * m_LoopResetSizeOnMovementAxis;
                    m_ContentStartPosition += Vector2.up * m_LoopResetSizeOnMovementAxis;
                }
            }
        }
        #endregion

        //计算应出现的索引、应消失的索引 和 未变的索引
        protected void CalcIndexes()
        {
            int sideX = (int)m_StartSide % 2;  //0：左， 1右
            int sideY = (int)m_StartSide % 2;  //0：上， 1下（注意与UIGridView的Corner计算不同）

            int outCountFromStart;  //完全滑出起始边界的数量
            int outCountFromEnd;    //完全滑出结束边界的数量

            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                float contentStartPosX = -m_CellStartOffsetOnMovementAxis;

                //content起始边界 相对于 viewport起始边界的位移宽度：
                float outWidthFromStart = (contentStartPosX - m_Content.anchoredPosition.x) * (sideX == 0 ? 1 : -1);
                //content结束边界 相对于 viewport结束边界的位移宽度：
                float outWidthFromEnd = (contentStartPosX + m_Content.anchoredPosition.x + (m_Content.rect.width - m_Viewport.rect.width) * (sideX == 0 ? 1 : -1)) * (sideX == 0 ? 1 : -1);

                float startPadding = sideX == 0 ? padding.left : padding.right;
                float endPadding = sideX == 0 ? padding.right : padding.left;

                //完全滑出左边界的数量（可为负），要向下取整，即尽量认为其没滑出，以保证可视区域内的正确性。（可为负）
                outCountFromStart = Mathf.FloorToInt((outWidthFromStart - startPadding + spacing.x) / (m_CellRect.size.x + spacing.x));
                //完全滑出右边界的数量（可为负），要向下取整，即尽量认为其没滑出，以保证可视区域内的正确性。（可为负）
                outCountFromEnd = Mathf.FloorToInt((outWidthFromEnd - endPadding + spacing.x) / (m_CellRect.size.x + spacing.x));
            }
            else
            {
                float contentStartPosY = -m_CellStartOffsetOnMovementAxis;

                //content起始边界 相对于 viewport起始边界的位移宽度：
                float outHeightFromStart = (contentStartPosY - m_Content.anchoredPosition.y) * (sideY == 0 ? -1 : 1);
                //content结束边界 相对于 viewport结束边界的位移宽度：
                float outHeightFromEnd = (contentStartPosY + m_Content.anchoredPosition.y + (m_Content.rect.height - m_Viewport.rect.height) * (sideY == 0 ? -1 : 1)) * (sideY == 0 ? -1 : 1);

                float startPadding = sideY == 0 ? padding.top : padding.bottom;
                float endPadding = sideY == 0 ? padding.bottom : padding.top;

                //完全滑出上边界的数量（可为负），要向下取整，即尽量认为其没滑出，以保证可视区域内的正确性。
                outCountFromStart = Mathf.FloorToInt((outHeightFromStart - startPadding + spacing.y) / (m_CellRect.size.y + spacing.y));
                //完全滑出下边界的数量（可为负），要向下取整，即尽量认为其没滑出，以保证可视区域内的正确性。（可为负）
                outCountFromEnd = Mathf.FloorToInt((outHeightFromEnd - endPadding + spacing.y) / (m_CellRect.size.y + spacing.y));
            }

            //应该显示的开始索引和结束索引
            int startIndex = (outCountFromStart); // 省略了先+1再-1。 从滑出的下一个开始，索引从0开始;
            int endIndex = (m_CellCount - 1 - outCountFromEnd);

            //Debug.Log("startIndex, endIndex: " + startIndex + ", " + endIndex);

            for (int index = startIndex; index <= endIndex; index++)
            {
                if (!IsValidIndex(index)) { continue; }
                m_NewIndexes.Add(index);
            }

            //找出出现的、消失的和未变的
            //出现的：在新列表中，但不在老列表中。
            m_AppearIndexes.Clear();
            foreach (int index in m_NewIndexes)
            {
                if (m_OldIndexes.IndexOf(index) < 0)
                {
                    m_AppearIndexes.Add(index);
                }
            }

            //消失的：在老列表中，但不在新列表中。
            m_DisAppearIndexes.Clear();
            foreach (int index in m_OldIndexes)
            {
                if (m_NewIndexes.IndexOf(index) < 0)
                {
                    m_DisAppearIndexes.Add(index);
                }
            }

            //保持的：既在新列表中，又在老列表中的
            m_StayIndexes.Clear();
            foreach (int index in m_NewIndexes)
            {
                if (m_OldIndexes.IndexOf(index) >= 0)
                {
                    m_StayIndexes.Add(index);
                }
            }

            ////输出调试
            //string str1 = "", str2 = "", str3 = "", str4 = "", str5 = "";
            //foreach (int index in m_NewIndexes) { str1 += index + ","; }
            //foreach (int index in m_OldIndexes) { str2 += index + ","; }
            //foreach (int index in m_AppearIndexes) { str3 += index + ","; }
            //foreach (int index in m_DisAppearIndexes) { str4 += index + ","; }
            //foreach (int index in m_StayIndexes) { str5 += index + ","; }
            //Debug.Log("m_NewIndexes: " + str1);
            //Debug.Log("m_OldIndexes: " + str2);
            //Debug.Log("m_AppearIndexes: " + str3);
            //Debug.Log("m_DisAppearIndexes: " + str4);
            //Debug.Log("m_StayIndexes: " + str5);
            //Debug.Log("-------------------------");

            //用 m_OldIndexes 保存当前帧索引数据。
            //复用新老列表，保证性能良好
            List<int> temp = m_OldIndexes;
            m_OldIndexes = m_NewIndexes;
            m_NewIndexes = temp;
            m_NewIndexes.Clear();
        }

        //该消失的消失
        protected void DisAppearCells()
        {
            foreach (int index in m_DisAppearIndexes)
            {
                //if (!IsValidIndex(index)) { continue; }   //不要限制，列表可能由长变短
                int validIndex = ConvertIndexToValid(index);
                //Debug.Log($"DisAppearCells index：{index}， validIndex：{validIndex}");
                bool exist = m_CellRTDict.TryGetValue(validIndex, out RectTransform cellRT);
                if (!exist) { continue; }
                m_CellRTDict.Remove(validIndex);
                cellRT.gameObject.SetActive(false);
                m_UnUseCellRTStack.Push(cellRT);
            }
        }

        //该出现的出现
        protected void AppearCells()
        {
            foreach (int index in m_AppearIndexes)
            {
                if (!IsValidIndex(index)) { continue; }
                int validIndex = ConvertIndexToValid(index);
                //Debug.Log($"AppearCells index：{index}， validIndex：{validIndex}");
                RectTransform cellRT = GetOrCreateCell(validIndex);
                m_CellRTDict[validIndex] = cellRT;
                cellRT.anchoredPosition = GetCellPos(index);        //设置Cell位置
                m_OnShowCell?.Invoke(validIndex);                   //Cell出现/刷新回调
            }
        }

        //刷新保持的
        protected void RefreshStayCells()
        {
            foreach (int index in m_StayIndexes)
            {
                if (!IsValidIndex(index)) { continue; }
                int validIndex = ConvertIndexToValid(index);
                //Debug.Log($"RefreshStayCells index：{index}， validIndex：{validIndex}");
                RectTransform cellRT = m_CellRTDict[validIndex];
                cellRT.anchoredPosition = GetCellPos(index);        //设置Cell位置
                m_OnShowCell?.Invoke(validIndex);                   //Cell出现/刷新回调
            }
        }

        //计算并设置Cells的SblingIndex
        //调用时机：有新的Cell出现时
        //Cell可能重叠时必须
        //若无需求，可去掉以节省性能
        protected virtual void CalcAndSetCellsSblingIndex()
        {
            if (m_AppearIndexes.Count <= 0) { return; }

            m_CellRTListForSort.Clear();
            foreach (KeyValuePair<int, RectTransform> kvp in m_CellRTDict)
            {
                m_CellRTListForSort.Add(kvp);
            }
            m_CellRTListForSort.Sort((x, y) =>
            {
                //按index升序
                return x.Key - y.Key;
            });

            foreach (KeyValuePair<int, RectTransform> kvp in m_CellRTListForSort)
            {
                //索引大的在上
                //kvp.Value.SetAsLastSibling();
                //索引大的在下
                kvp.Value.SetAsFirstSibling();
            }
        }

        //是否有效索引（只将显示索引显示到列表中，默认为 0~cellCount 之间）
        //loop时，认为任意索引都是有效的，以使非 0~cellCount 的区域能够显示元素，之后再在 ConvertIndexToValid 转换
        protected virtual bool IsValidIndex(int index)
        {
            if (m_Loop) { return true; }
            else { return index >= 0 && index < m_CellCount; }
        }

        //转换索引至有效（默认无需处理）
        //loop时，将任意索引数转到 [0~cellCount-1] 中
        protected virtual int ConvertIndexToValid(int index)
        {
            if (m_Loop) { return (index % m_CellCount + m_CellCount) % m_CellCount; }
            else { return index; }
        }

        //计算 开始排布Cell的起始位置（核心为：Cell在 “剩余可用尺寸”中如何对齐）
        protected float GetCellStartOffset(int axis, float requiredSpaceWithoutPadding)
        {
            float requiredSpace = requiredSpaceWithoutPadding + (axis == 0 ? padding.horizontal : padding.vertical);  //该轴上子元素需要的总尺寸 + 边距
            float availableSpace = m_Content.rect.size[axis];       //该轴上 Content 的实际可用尺寸
            float surplusSpace = availableSpace - requiredSpace;    //剩余可用尺寸（可以是负的）
            float alignmentOnAxis = GetAlignmentOnAxis(axis);       //获取小数形式的子元素对齐方式

            //水平方向从左开始，竖直方向从上开始。
            // 要计入剩余尺寸。以水平方向为例，
            // 若对齐方式为居左，则 alignmentOnAxis 为 0， 结果为 padding.left + 0，可以达到居左效果；
            // 若对齐方式为居中，则 alignmentOnAxis 为 0.5， 结果为 padding.left + 0.5*剩余距离，可以达到居中效果；
            // 若对齐方式为居右，则 alignmentOnAxis 为 1， 结果为 padding.left + 1*剩余距离，可以达到居右效果。
            float cellStartOffset = (axis == 0 ? padding.left : padding.top) + surplusSpace * alignmentOnAxis;

            return cellStartOffset;
        }

        // Returns the alignment on the specified axis as a fraction where 0 is left/top, 0.5 is middle, and 1 is right/bottom.
        // 以小数形式返回指定轴上的对齐方式，其中0为左/上，0.5为中，1为右/下。（水平方向：0左，0.5中，1右）（竖直方向：0上，0.5中，1下）
        // 参数 "axis"：The axis to get alignment along. 0 is horizontal and 1 is vertical.    //轴索引，0是水平的，1是垂直的。
        // 返回值：The alignment as a fraction where 0 is left/top, 0.5 is middle, and 1 is right/bottom. //小数形式的对齐方式
        protected float GetAlignmentOnAxis(int axis)
        {
            return (axis == (int)m_MovementAxis) ? 0.5f : (int)childAlignment * 0.5f;
        }

        protected Vector2 GetCellPos(int index)
        {
            //一、数据索引转位置索引
            int posIndexX;   //X位置索引
            int posIndexY;   //Y位置索引
            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                posIndexX = index;
                posIndexY = 0;
            }
            else
            {
                posIndexX = 0;
                posIndexY = index;
            }

            //二、根据起始角落进行转置
            if (m_MovementAxis == MovementAxis.Horizontal && m_StartSide == Side.RightOrLower) { posIndexX = m_ActualCellCountX - 1 - posIndexX; }   //如果是从右往左
            if (m_MovementAxis == MovementAxis.Vertical && m_StartSide == Side.RightOrLower) { posIndexY = m_ActualCellCountY - 1 - posIndexY; }   //如果是从下往上

            //三、计算坐标
            Vector2 scaleFactor = Vector2.one;  //不考虑元素缩放

            // x轴：初始位置+宽度*中心点偏移*缩放系数 (x轴是向正方向)(从左上到右下)
            float anchoredPosX = (m_CellStartOffset.x + (m_CellRect.size.x + spacing.x) * posIndexX) + m_CellRect.size.x * m_CellPivot.x * scaleFactor.x;

            // y轴：-初始位置-宽度*(1-中心点偏移)*缩放系数 (y轴是向负方向)(从左上到右下)
            float anchoredPosY = -(m_CellStartOffset.y + (m_CellRect.size.y + spacing.y) * posIndexY) - m_CellRect.size.y * (1f - m_CellPivot.y) * scaleFactor.y;

            //Debug.Log($"index: {index}, posIndexX: {posIndexX}, posIndexY: {posIndexY}, anchoredPosX: {anchoredPosX}, anchoredPosY: {anchoredPosY}, m_StartOffset.x: {m_CellStartOffset.x}");

            return new Vector2(anchoredPosX, anchoredPosY);
        }

        protected void SetProperty<T>(ref T currentValue, T newValue)
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))  //过滤无效和未变
                return;
            currentValue = newValue;
            //RefreshAll();
        }

        protected RectTransform GetOrCreateCell(int index)
        {
            RectTransform cellRT;
            if (m_UnUseCellRTStack.Count > 0)
            {
                cellRT = m_UnUseCellRTStack.Pop();
                cellRT.gameObject.SetActive(true);
            }
            else
            {
                cellRT = m_OnCreateCell(index);
                cellRT.SetParent(m_Content, false);

                //驱动子物体的锚点和位置
                m_Tracker.Add(this, cellRT, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta);

                //强制设置Cell的anchor
                cellRT.anchorMin = Vector2.up;
                cellRT.anchorMax = Vector2.up;

                cellRT.sizeDelta = m_CellRect.size;
            }

            return cellRT;
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            base.OnDisable();
        }

        //protected override void OnRectTransformDimensionsChange()
        //{
        //    base.OnRectTransformDimensionsChange();
        //    //RefreshAll();
        //}

        //#if UNITY_EDITOR
        //        protected override void OnValidate()
        //        {
        //            base.OnValidate();
        //            //RefreshAll();
        //        }
        //#endif
    }
}
