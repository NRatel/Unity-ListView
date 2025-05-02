//https://github.com/NRatel/Unity-ListView

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NRatel
{
    // 轴方向（2种，水平/竖直）和 开始排布的角落（4种，上左/上右/下左/下右），可以确定出以下Grid结果：
    // 1、元素延伸方向（4种，从左往右/从右往左/从上往下/从下往上）（将在运行时强制修改Contetnt的起始中心点和锚点）
    // 2、元素排布轨迹（4*2种，4个延伸方向*2侧）
    public class UIGridView : UIScrollRect
    {
        public enum Corner
        {
            LeftUpper = 0,          //左上
            RightUpper = 1,         //右上
            LeftLower = 2,          //左下
            RightLower = 3          //右下
        }

        public enum Constraint
        {
            Flexible = 0,           // 不限定行或列数（灵活自适应）
            FixedRowCount = 1,      // 限定行数（水平滑动时）
            FixedColumnCount = 2    // 限定列数（竖直滑动时）
        }

        public enum Alignment
        {
            LeftOrUpper = 0,
            CenterOrMiddle = 1,
            RightOrLower = 2,
        }

        public MovementAxis startAxis { get { return (MovementAxis)(1 - (int)m_MovementAxis); } }  //开始排列轴，与m_MovementAxis垂直

        [SerializeField] protected Corner m_StartCorner = Corner.LeftUpper;
        public Corner startCorner { get { return m_StartCorner; } set { SetProperty(ref m_StartCorner, value); } }

        [SerializeField] protected Constraint m_Constraint = Constraint.Flexible;
        public Constraint constraint { get { return m_Constraint; } set { SetProperty(ref m_Constraint, value); } }

        [SerializeField] protected int m_ConstraintCount = 2;
        public int constraintCount { get { return m_ConstraintCount; } set { SetProperty(ref m_ConstraintCount, Mathf.Max(1, value)); } }

        [SerializeField] protected Vector2 m_Spacing = Vector2.zero;
        public Vector2 spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }

        [SerializeField] protected Alignment m_ChildAlignment = Alignment.LeftOrUpper;
        public Alignment childAlignment { get { return m_ChildAlignment; } set { SetProperty(ref m_ChildAlignment, value); } }

        [SerializeField] protected RectOffset m_Padding = new RectOffset();
        public RectOffset padding { get { return m_Padding; } set { SetProperty(ref m_Padding, value); } }

        protected DrivenRectTransformTracker m_Tracker;

        private int m_CellsPerMainAxis;
        private int m_ActualCellCountX;
        private int m_ActualCellCountY;
        private Vector2 m_RequiredSpace;
        private Vector2 m_CellStartOffset;

        protected Dictionary<int, RectTransform> m_CellRTDict;                      //index-Cell字典    
        protected Stack<RectTransform> m_UnUseCellRTStack;                          //空闲Cell堆栈
        protected List<KeyValuePair<int, RectTransform>> m_CellRTListForSort;       //Cell列表用于辅助Sbling排序

        protected List<int> m_OldIndexes;                                           //旧的索引集合
        protected List<int> m_NewIndexes;                                           //新的索引集合
        protected List<int> m_AppearIndexes;                                        //将要出现的索引集合   //使用List而非单个，可以支持Content位置跳变
        protected List<int> m_DisAppearIndexes;                                     //将要消失的索引集合   //使用List而非单个，可以支持Content位置跳变
        protected List<int> m_StayIndexes;                                          //保持的索引集合       //使用List而非单个，可以支持Content位置跳变

        private Rect m_CellRect;                                                    //Cell Rect（必需）
        private Vector2 m_CellPivot;                                                //Cell中心点（必需）
        private Func<int, RectTransform> m_OnCreateCell;                            //创建Cell的方法（必需）
        private Action<int> m_OnShowCell;                                           //展示Cell的方法（出现/刷新时回调）（必需）

        private int m_CellCount;                                                    //显示数量

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
        }

        /// <summary>
        /// 用于元素数量未变时（仅数据变化）的全部刷新
        /// （注意，若已知某个索引变化，且数量未变，应使用 TryRefreshCellRT 刷新单个）
        /// </summary>
        public void RefreshAll()
        {
            CalcCellCountOnNaturalAxis();
            CalculateRequiredSpace();
            SetContentSizeOnMovementAxis();
            CalculateCellStartOffset();

            CalcIndexes();
            DisAppearCells();
            AppearCells();
            RefreshStayCells();
            CalcAndSetCellsSblingIndex();
        }

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
        /// <param name="immediately">是否立刻跳转</param>
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
        private void ResetContentRT()
        {
            // 根据轴向和起始角落设置锚点、中心点
            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                int cornerX = (int)m_StartCorner % 2;  //0：左， 1右
                m_Content.anchorMin = new Vector2(cornerX, 0);
                m_Content.anchorMax = new Vector2(cornerX, 1);
                m_Content.pivot = new Vector2(cornerX, 0.5f);
            }
            else
            {
                int cornerY = (int)m_StartCorner / 2;  //0：上， 1下
                m_Content.anchorMin = new Vector2(0, 1 - cornerY);
                m_Content.anchorMax = new Vector2(1, 1 - cornerY);
                m_Content.pivot = new Vector2(0.5f, 1 - cornerY);
            }

            // 位置归0
            m_Content.anchoredPosition = Vector2.zero;

            // 大小重置为与Viewport相同
            m_Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_Viewport.rect.width);
            m_Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_Viewport.rect.height);
        }

        private void ResetTracker()
        {
            m_Tracker.Clear();
        }

        //计算直观行列数（自然坐标轴上）
        public void CalcCellCountOnNaturalAxis()
        {
            int cellCountX = 1;  //默认最小1
            int cellCountY = 1;  //默认最小1

            if (startAxis == MovementAxis.Horizontal)
            {
                Debug.Assert(m_Constraint == Constraint.FixedColumnCount || m_Constraint == Constraint.Flexible); //由编辑器限制选项

                if (m_Constraint == Constraint.FixedColumnCount)
                {
                    cellCountX = m_ConstraintCount;
                }
                else if (m_Constraint == Constraint.Flexible)
                {
                    // 自适应时：
                    if (m_CellRect.size.x + spacing.x <= 0)
                        //处理参数不合法的情况
                        cellCountX = int.MaxValue;
                    else
                    {
                        //列数 = 能放下的最大列数
                        float width = m_Content.rect.width;
                        cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (m_CellRect.size.x + spacing.x)));
                    }
                }

                if (m_CellCount > cellCountX)   //多于一列时
                    cellCountY = m_CellCount / cellCountX + (m_CellCount % cellCountX > 0 ? 1 : 0); //行数 = 整除（总数/列数） 有余数+1，没余数则不+
            }
            else
            {
                Debug.Assert(m_Constraint == Constraint.FixedRowCount || m_Constraint == Constraint.Flexible); //由编辑器限制选项

                if (m_Constraint == Constraint.FixedRowCount)
                {
                    cellCountY = m_ConstraintCount;
                }
                else if (m_Constraint == Constraint.Flexible)
                {
                    if (m_CellRect.size.y + spacing.y <= 0)
                        //处理参数不合法的情况
                        cellCountY = int.MaxValue;
                    else
                    {
                        //行数 = 能放下的最大行数
                        float height = m_Content.rect.height;
                        cellCountY = Mathf.Max(1, Mathf.FloorToInt((height - padding.vertical + spacing.y + 0.001f) / (m_CellRect.size.y + spacing.y)));
                    }
                }
                if (m_CellCount > cellCountY)   //多于一行时
                    cellCountX = m_CellCount / cellCountY + (m_CellCount % cellCountY > 0 ? 1 : 0); //列数 = 整除（总数/行数） 有余数+1，没余数则不+

            }

            //行列数约束至合法范围
            int cellsPerMainAxis;  //沿startAxis轴的格子数
            int actualCellCountX;  //实际列数
            int actualCellCountY;  //实际行数

            if (startAxis == MovementAxis.Horizontal)
            {
                cellsPerMainAxis = cellCountX;
                actualCellCountX = Mathf.Clamp(cellCountX, 1, m_CellCount);  //注意，这里Mathf.Clamp是因为上面自适应中非法时，将行列数设为了Int最大值。
                actualCellCountY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(m_CellCount / (float)cellsPerMainAxis));
            }
            else
            {
                cellsPerMainAxis = cellCountY;
                actualCellCountY = Mathf.Clamp(cellCountY, 1, m_CellCount);
                actualCellCountX = Mathf.Clamp(cellCountX, 1, Mathf.CeilToInt(m_CellCount / (float)cellsPerMainAxis));
            }

            this.m_CellsPerMainAxis = cellsPerMainAxis;
            this.m_ActualCellCountX = actualCellCountX;
            this.m_ActualCellCountY = actualCellCountY;
        }

        //计算实际需要的空间大小（不含padding） 及 在这个空间上第一个元素所在的位置
        private void CalculateRequiredSpace()
        {
            Vector2 requiredSpace = new Vector2(
                m_ActualCellCountX * m_CellRect.size.x + (m_ActualCellCountX - 1) * spacing.x,
                m_ActualCellCountY * m_CellRect.size.y + (m_ActualCellCountY - 1) * spacing.y
            );
            this.m_RequiredSpace = requiredSpace;
        }

        //设置滑动轴方向的Content大小
        private void SetContentSizeOnMovementAxis()
        {
            RectTransform.Axis axis;
            float size;
            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                axis = RectTransform.Axis.Horizontal;
                size = m_RequiredSpace.x + padding.horizontal;
            }
            else
            {
                axis = RectTransform.Axis.Vertical;
                size = m_RequiredSpace.y + padding.vertical;
            }

            m_Content.SetSizeWithCurrentAnchors(axis, size);
        }

        //计算Cell起始Offset
        //注意：使 元素对齐方式只影响 非滑动轴方向
        //因为滑动轴方向 Content大小由元素数决定。（不同于UGUI的Layout,滑动方向是自由大小）
        private void CalculateCellStartOffset()
        {
            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                m_CellStartOffset = new Vector2(padding.left, GetCellStartOffset((int)MovementAxis.Vertical, m_RequiredSpace.y));
            }
            else
            {
                m_CellStartOffset = new Vector2(GetCellStartOffset((int)MovementAxis.Horizontal, m_RequiredSpace.x), padding.top);
            }
        }

        //计算应出现的索引、应消失的索引 和 未变的索引
        private void CalcIndexes()
        {
            int cornerX = (int)m_StartCorner % 2;  //0：左， 1右
            int cornerY = (int)m_StartCorner / 2;  //0：上， 1下

            int outCountFromStart = 0;  //完全滑出起始边界的数量
            int outCountFromEnd = 0;    //完全滑出结束边界的数量

            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                //content起始边界 相对于 viewport起始边界的位移宽度：
                float outWidthFromStart = -m_Content.anchoredPosition.x * (cornerX == 0 ? 1 : -1);
                //content结束边界 相对于 viewport结束边界的位移宽度：
                float outWidthFromEnd = (m_Content.anchoredPosition.x + (m_Content.rect.width - m_Viewport.rect.width) * (cornerX == 0 ? 1 : -1)) * (cornerX == 0 ? 1 : -1);

                float startPadding = cornerX == 0 ? padding.left : padding.right;
                //滑出的列数，要向下取整，即尽量认为其没滑出，以保证可视区域内的正确性。
                int outColFromStart = Mathf.FloorToInt((outWidthFromStart - startPadding + spacing.x) / (m_CellRect.size.x + spacing.x));
                outCountFromStart = outColFromStart * m_ActualCellCountY;

                float endPadding = cornerX == 0 ? padding.right : padding.left;
                //滑出的列数，要向下取整，即尽量认为其没滑出，以保证可视区域内的正确性。
                int outColFromEnd = Mathf.FloorToInt((outWidthFromEnd - endPadding + spacing.x) / (m_CellRect.size.x + spacing.x));
                //若最后一列未满，则从总数中减去。
                int theLastColOffsetCount = m_CellCount % m_ActualCellCountY != 0 ? (m_ActualCellCountY - m_CellCount % m_ActualCellCountY) : 0;
                outCountFromEnd = outColFromEnd * m_ActualCellCountY - theLastColOffsetCount;
            }
            else
            {
                //content起始边界 相对于 viewport起始边界的位移宽度：
                float outHeightFromStart = -m_Content.anchoredPosition.y * (cornerY == 0 ? -1 : 1);
                //content结束边界 相对于 viewport结束边界的位移宽度：
                float outHeightFromEnd = (m_Content.anchoredPosition.y + (m_Content.rect.height - m_Viewport.rect.height) * (cornerY == 0 ? -1 : 1)) * (cornerY == 0 ? -1 : 1);

                float startPadding = cornerY == 0 ? padding.top : padding.bottom;
                //滑出的行数，要向下取整，即尽量认为其没滑出，以保证可视区域内的正确性。
                int outRowFromStart = Mathf.FloorToInt((outHeightFromStart - startPadding + spacing.y) / (m_CellRect.size.y + spacing.y));
                outCountFromStart = outRowFromStart * m_ActualCellCountX;

                float endPadding = cornerY == 0 ? padding.bottom : padding.top;
                //滑出的行数，要向下取整，即尽量认为其没滑出，以保证可视区域内的正确性。
                int outRowFromEnd = Mathf.FloorToInt((outHeightFromEnd - endPadding + spacing.y) / (m_CellRect.size.y + spacing.y));
                //若最后一行未满，则从总数中减去。
                int theLastRowOffsetCount = m_CellCount % m_ActualCellCountX != 0 ? (m_ActualCellCountX - m_CellCount % m_ActualCellCountX) : 0;
                outCountFromEnd = outRowFromEnd * m_ActualCellCountX - theLastRowOffsetCount;
            }

            //应该显示的开始索引和结束索引
            int startIndex = (outCountFromStart); // 省略了先+1再-1。 从滑出的下一个开始，索引从0开始;
            int endIndex = (m_CellCount - 1 - outCountFromEnd);

            //Debug.Log("startIndex, endIndex: " + startIndex + ", " + endIndex);

            for (int index = startIndex; index <= endIndex; index++)
            {
                m_NewIndexes.Add(index);
            }

            ////新旧索引列表输出调试
            //string Str1 = "";
            //foreach (int index in newIndexes)
            //{
            //    Str1 += index + ",";
            //}
            //string Str2 = "";
            //foreach (int index in oldIndexes)
            //{
            //    Str2 += index + ",";
            //}
            //Debug.Log("Str1: " + Str1);
            //Debug.Log("Str2: " + Str2);
            //Debug.Log("-------------------------");

            //找出出现的、消失的和未变的
            //出现的：在新列表中，但不在老列表中。
            m_AppearIndexes.Clear();
            foreach (int index in m_NewIndexes)
            {
                if (m_OldIndexes.IndexOf(index) < 0)
                {
                    //Debug.Log("出现：" + index);
                    m_AppearIndexes.Add(index);
                }
            }

            //消失的：在老列表中，但不在新列表中。
            m_DisAppearIndexes.Clear();
            foreach (int index in m_OldIndexes)
            {
                if (m_NewIndexes.IndexOf(index) < 0)
                {
                    //Debug.Log("消失：" + index);
                    m_DisAppearIndexes.Add(index);
                }
            }

            //保持的：既在新列表中，又在老列表中的
            m_StayIndexes.Clear();
            foreach (int index in m_NewIndexes)
            {
                if (m_OldIndexes.IndexOf(index) >= 0)
                {
                    //Debug.Log("保持：" + index);
                    m_StayIndexes.Add(index);
                }
            }

            //用 m_OldIndexes 保存当前帧索引数据。
            //复用新老列表，保证性能良好
            List<int> temp = m_OldIndexes;
            m_OldIndexes = m_NewIndexes;
            m_NewIndexes = temp;
            m_NewIndexes.Clear();
        }

        //该消失的消失
        private void DisAppearCells()
        {
            foreach (int index in m_DisAppearIndexes)
            {
                if (!IsValidIndex(index)) { continue; }

                RectTransform cellRT = m_CellRTDict[index];
                m_CellRTDict.Remove(index);
                cellRT.gameObject.SetActive(false);
                m_UnUseCellRTStack.Push(cellRT);
            }
        }

        //该出现的出现
        private void AppearCells()
        {
            foreach (int index in m_AppearIndexes)
            {
                if (!IsValidIndex(index)) { continue; }

                RectTransform cellRT = GetOrCreateCell(index);
                m_CellRTDict[index] = cellRT;
                cellRT.anchoredPosition = GetCellPos(index);        //设置Cell位置

                int validIndex = ConvertIndexToValid(index);
                m_OnShowCell?.Invoke(validIndex);                   //Cell出现/刷新回调
            }
        }

        //刷新保持的
        private void RefreshStayCells()
        {
            foreach (int index in m_StayIndexes)
            {
                RectTransform cellRT = m_CellRTDict[index];
                cellRT.anchoredPosition = GetCellPos(index);        //设置Cell位置
                m_OnShowCell?.Invoke(index);                        //Cell出现/刷新回调
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
        protected virtual bool IsValidIndex(int index)
        {
            return index >= 0 && index < m_CellCount;
        }

        //转换索引至有效（默认无需处理）
        protected virtual int ConvertIndexToValid(int index)
        {
            return index;
        }

        //计算 开始排布Cell的起始位置（核心为：计算Cell在“剩余可用尺寸”中，居何对齐）
        private float GetCellStartOffset(int axis, float requiredSpaceWithoutPadding)
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
            return (axis == 0 ? padding.left : padding.top) + surplusSpace * alignmentOnAxis;
        }

        // Returns the alignment on the specified axis as a fraction where 0 is left/top, 0.5 is middle, and 1 is right/bottom.
        // 以小数形式返回指定轴上的对齐方式，其中0为左/上，0.5为中，1为右/下。（水平方向：0左，0.5中，1右）（竖直方向：0上，0.5中，1下）
        // 参数 "axis"：The axis to get alignment along. 0 is horizontal and 1 is vertical.    //轴索引，0是水平的，1是垂直的。
        // 返回值：The alignment as a fraction where 0 is left/top, 0.5 is middle, and 1 is right/bottom. //小数形式的对齐方式
        private float GetAlignmentOnAxis(int axis)
        {
            return (axis == (int)m_MovementAxis) ? 0.5f : (int)childAlignment * 0.5f;
        }

        private Vector2 GetCellPos(int index)
        {
            //一、数据索引转位置索引
            int posIndexX;   //X位置索引
            int posIndexY;   //Y位置索引
            if (startAxis == MovementAxis.Horizontal)
            {
                //posIndexX = index % m_CellsPerMainAxis;
                //posIndexY = index / m_CellsPerMainAxis;
                posIndexX = ((index % m_CellsPerMainAxis) + m_CellsPerMainAxis) % m_CellsPerMainAxis;   //负数索引也转到 [0,m_CellsPerMainAxis) 中（循环归一化算法）
                posIndexY = Mathf.FloorToInt((float)index / m_CellsPerMainAxis);
            }
            else
            {
                //posIndexX = index / m_CellsPerMainAxis;
                //posIndexY = index % m_CellsPerMainAxis;
                posIndexX = Mathf.FloorToInt((float)index / m_CellsPerMainAxis);
                posIndexY = ((index % m_CellsPerMainAxis) + m_CellsPerMainAxis) % m_CellsPerMainAxis;   //负数索引也转到 [0,m_CellsPerMainAxis) 中（循环归一化算法）
            }

            //二、根据起始角落进行转置
            int cornerX = (int)m_StartCorner % 2;  //0：左， 1右
            int cornerY = (int)m_StartCorner / 2;  //0：上， 1下
            if (cornerX == 1) { posIndexX = m_ActualCellCountX - 1 - posIndexX; }   //如果是从右往左
            if (cornerY == 1) { posIndexY = m_ActualCellCountY - 1 - posIndexY; }   //如果是从下往上

            //三、计算坐标
            Vector2 scaleFactor = Vector2.one;  //不考虑元素缩放

            // x轴：初始位置+宽度*中心点偏移*缩放系数 (x轴是向正方向)(从左上到右下)
            float anchoredPosX = (m_CellStartOffset.x + (m_CellRect.size.x + spacing.x) * posIndexX) + m_CellRect.size.x * m_CellPivot.x * scaleFactor.x;

            // y轴：-初始位置-宽度*(1-中心点偏移)*缩放系数 (y轴是向负方向)(从左上到右下)
            float anchoredPosY = -(m_CellStartOffset.y + (m_CellRect.size.y + spacing.y) * posIndexY) - m_CellRect.size.y * (1f - m_CellPivot.y) * scaleFactor.y;

            return new Vector2(anchoredPosX, anchoredPosY);
        }

        protected void SetProperty<T>(ref T currentValue, T newValue)
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))  //过滤无效和未变
                return;
            currentValue = newValue;
            //RefreshAll();
        }

        private RectTransform GetOrCreateCell(int index)
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
