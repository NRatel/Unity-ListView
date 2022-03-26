using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NRatel
{
    // 轴方向和开始排布的角落，可以确定：
    // 1、滑动方向（Contetnt的起始中心点和锚点）（4种）
    // 2、元素排布轨迹（2*4种）
    public class UIGridView : UIScrollRect
    {
        public enum Corner
        {
            UpperLeft = 0,      //左上
            UpperRight = 1,     //右上
            LowerLeft = 2,      //左下
            LowerRight = 3      //右下
        }

        public enum Constraint
        {
            Flexible = 0,           // 不限定行或列数（灵活自适应）
            FixedColumnCount = 1,   // 限定列数（水平轴时）
            FixedRowCount = 2       // 限定行数（竖直轴时）
        }

        #region 排布参数
        [SerializeField] protected Corner m_StartCorner = Corner.UpperLeft;
        public Corner startCorner { get { return m_StartCorner; } set { SetProperty(ref m_StartCorner, value); } }

        [SerializeField] protected Constraint m_Constraint = Constraint.Flexible;
        public Constraint constraint { get { return m_Constraint; } set { SetProperty(ref m_Constraint, value); } }

        [SerializeField] protected int m_ConstraintCount = 2;
        public int constraintCount { get { return m_ConstraintCount; } set { SetProperty(ref m_ConstraintCount, Mathf.Max(1, value)); } }

        [SerializeField] protected Vector2 m_Spacing = Vector2.zero;
        public Vector2 spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }
        #endregion

        #region 对齐参数
        [SerializeField] protected TextAnchor m_ChildAlignment = TextAnchor.UpperLeft;
        public TextAnchor childAlignment { get { return m_ChildAlignment; } set { SetProperty(ref m_ChildAlignment, value); } }

        [SerializeField] protected AlignmentOffset m_Offset = new AlignmentOffset();
        public AlignmentOffset offset { get { return m_Offset; } set { SetProperty(ref m_Offset, value); } }
        #endregion

        private Vector2Int m_CellCountOnAxis;

        public Vector2 GetCellSize()
        {
            return new Vector2(100, 100);
        }

        public int GetCellCount()
        {
            return 10;
        }

        public void Refresh()
        {
            CalcCellCountOnAxis();

        }

        //一、按直观（水平向），计算行列数
        public void CalcCellCountOnAxis()
        {
            int cellCount = GetCellCount();

            float width = rectTransform.rect.size.x;
            float height = rectTransform.rect.size.y;
            
            Vector2Int cellCountOnAxis = Vector2Int.one;

            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                Debug.Assert(m_Constraint == Constraint.FixedColumnCount || m_Constraint == Constraint.Flexible); //由编辑器限制选项

                if (m_Constraint == Constraint.FixedColumnCount)
                {
                    cellCountOnAxis.x = m_ConstraintCount;
                    if (cellCount > cellCountOnAxis.x)   //多于一列时
                        cellCountOnAxis.y = cellCount / cellCountOnAxis.x + (cellCount % cellCountOnAxis.x > 0 ? 1 : 0);
                }
                else if (m_Constraint == Constraint.Flexible)
                {
                    Vector2 cellSize = GetCellSize();
                    cellCountOnAxis.x = Mathf.Max(1, Mathf.FloorToInt((width - offset.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
                    cellCountOnAxis.y = Mathf.CeilToInt(GetCellCount() / (float)cellCountOnAxis.x - 0.001f);
                }
            }  
            else
            {
                Debug.Assert(m_Constraint == Constraint.FixedRowCount || m_Constraint == Constraint.Flexible); //由编辑器限制选项

                if (m_Constraint == Constraint.FixedRowCount)
                {
                    cellCountOnAxis.y = m_ConstraintCount;
                    cellCountOnAxis.x = Mathf.CeilToInt(GetCellCount() / (float)cellCountOnAxis.y - 0.001f);
                }
                else if (m_Constraint == Constraint.Flexible)
                {
                    Vector2 cellSize = GetCellSize();
                    cellCountOnAxis.y = Mathf.Max(1, Mathf.FloorToInt((height - offset.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
                    cellCountOnAxis.x = Mathf.CeilToInt(GetCellCount() / (float)cellCountOnAxis.y - 0.001f);
                }
            }

            m_CellCountOnAxis = cellCountOnAxis;
        }

        protected void SetProperty<T>(ref T currentValue, T newValue)
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))  //过滤无效和未变
                return;
            currentValue = newValue;
            Refresh();
        }
    }

    public class AlignmentOffset
    {
        private float m_Left;       //水平居左时
        private float m_OffsetX;    //水平居中时
        private float m_Right;      //水平居右时
        private float m_Top;        //竖直居上时
        private float m_OffsetY;    //竖直居中时
        private float m_Bottomt;    //竖直居下时

        public float left { get { return m_Left; } set { m_Left = value; } }
        public float offsetX { get { return m_OffsetX; } set { m_OffsetX = value; } }
        public float right { get { return m_Right; } set { m_Right = value; } }
        public float top { get { return m_Top; } set { m_Top = value; } }
        public float offsetY { get { return m_OffsetY; } set { m_OffsetY = value; } }
        public float bottom { get { return m_Bottomt; } set { m_Bottomt = value; } }

        //水平方向最终值（同时只会有一个生效）
        public float horizontal
        {
            get
            {
                float v = 0;
                if (m_Left != 0) { v = m_Left; }
                if (m_OffsetX != 0) { v = m_OffsetX; }
                if (m_Right != 0) { v = m_Right; }
                return v;
            }
        }

        //竖直方向最终值（同时只会有一个生效）
        public float vertical
        {
            get
            {
                float v = 0;
                if (m_Top != 0) { v = m_Top; }
                if (m_OffsetY != 0) { v = m_OffsetY; }
                if (m_Bottomt != 0) { v = m_Bottomt; }
                return v;
            }
        }
    }
}
