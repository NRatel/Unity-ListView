//https://github.com/NRatel/Unity-ListView

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;

namespace NRatel
{
    // �᷽��2�֣�ˮƽ/��ֱ���� ��ʼ�Ų��Ľ��䣨4�֣�����/����/����/���ң�������ȷ��������Grid�����
    // 1��Ԫ�����췽��4�֣���������/��������/��������/�������ϣ�����������ʱǿ���޸�Contetnt����ʼ���ĵ��ê�㣩
    // 2��Ԫ���Ų��켣��4*2�֣�4�����췽��*2�ࣩ
    public class UIListView : UIScrollRect
    {
        public enum Corner
        {
            LeftOrUpper = 0,          //�����
            RightOrLower = 1,         //�һ���
        }

        public enum Alignment
        {
            LeftOrUpper = 0,
            CenterOrMiddle = 1,
            RightOrLower = 2,
        }

        [SerializeField] protected Corner m_StartCorner = Corner.LeftOrUpper;
        public Corner startCorner { get { return m_StartCorner; } set { SetProperty(ref m_StartCorner, value); } }

        [SerializeField] protected Vector2 m_Spacing = Vector2.zero;
        public Vector2 spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }

        [SerializeField] protected Alignment m_ChildAlignment = Alignment.LeftOrUpper;
        public Alignment childAlignment { get { return m_ChildAlignment; } set { SetProperty(ref m_ChildAlignment, value); } }

        [SerializeField] protected RectOffset m_Padding = new RectOffset();
        public RectOffset padding { get { return m_Padding; } set { SetProperty(ref m_Padding, value); } }

        protected DrivenRectTransformTracker m_Tracker;

        protected int m_ActualCellCountX;
        protected int m_ActualCellCountY;
        protected Vector2 m_RequiredSpace;
        protected Vector2 m_CellStartOffset;

        protected Dictionary<int, RectTransform> m_CellRTDict;                      //index-Cell�ֵ�    
        protected Stack<RectTransform> m_UnUseCellRTStack;                          //����Cell��ջ
        protected List<KeyValuePair<int, RectTransform>> m_CellRTListForSort;       //Cell�б����ڸ���Sbling����

        protected List<int> m_OldIndexes;                                           //�ɵ���������
        protected List<int> m_NewIndexes;                                           //�µ���������
        protected List<int> m_AppearIndexes;                                        //��Ҫ���ֵ���������   //ʹ��List���ǵ���������֧��Contentλ������
        protected List<int> m_DisAppearIndexes;                                     //��Ҫ��ʧ����������   //ʹ��List���ǵ���������֧��Contentλ������
        protected List<int> m_StayIndexes;                                          //���ֵ���������       //ʹ��List���ǵ���������֧��Contentλ������

        protected Rect m_CellRect;                                                  //Cell Rect�����裩
        protected Vector2 m_CellPivot;                                              //Cell���ĵ㣨���裩
        protected Func<int, RectTransform> m_OnCreateCell;                          //����Cell�ķ��������裩
        protected Action<int> m_OnShowCell;                                         //չʾCell�ķ���������/ˢ��ʱ�ص��������裩

        protected int m_CellCount;                                                  //��ʾ����

        protected virtual float m_CellStartOffsetOnMovementAxis { get { return 0f; } }

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

        //��Cellģ����ȡrect��pivot���г�ʼ��������GameObject.Instantiateʵ����Cellģ��ķ�ʽ����Cell
        public void Init(RectTransform templateCellRT, Action<int> onShowCell)
        {
            this.m_CellRect = templateCellRT.rect;
            this.m_CellPivot = templateCellRT.pivot;
            this.m_OnCreateCell = (_) => { return GameObject.Instantiate<GameObject>(templateCellRT.gameObject).GetComponent<RectTransform>(); };
            this.m_OnShowCell = onShowCell;
        }

        //��Cellģ����ȡrect��pivot���г�ʼ��������ָ������Cell�ķ���
        public void Init(RectTransform templateCellRT, Func<int, RectTransform> onCreateCell, Action<int> onShowCell)
        {
            this.m_CellRect = templateCellRT.rect;
            this.m_CellPivot = templateCellRT.pivot;
            this.m_OnCreateCell = onCreateCell;
            this.m_OnShowCell = onShowCell;
        }

        //��rect��pivot��ʼ��������ָ������Cell�ķ���
        public void Init(Rect cellRect, Vector2 cellPivot, Func<int, RectTransform> onCreateCell, Action<int> onShowCell)
        {
            this.m_CellRect = cellRect;
            this.m_CellPivot = cellPivot;
            this.m_OnCreateCell = onCreateCell;
            this.m_OnShowCell = onShowCell;
        }

        /// <summary>
        /// ��ʼ��ʾһ���µ�GridView��
        /// Ҳ������Ԫ�������仯ʱ��ȫ��ˢ��
        /// Ҳ�����ڴ�0���
        /// </summary>
        /// <param name="count">Ҫ��ʾ������</param>
        /// <param name="stayPos">�����仯ʱ�Ƿ�������λ�ò��䣬���������仯������봫false</param>
        public void StartShow(int count, bool stayPos = true)
        {
            Debug.Assert(m_OnCreateCell != null, "���ȳ�ʼ��");

            this.m_CellCount = count;

            if (!stayPos) { ResetContentRT(); }

            RefreshAll();
        }

        /// <summary>
        /// ����Ԫ������δ��ʱ�������ݱ仯����ȫ��ˢ��
        /// ��ע�⣬����֪ĳ�������仯��������δ�䣬Ӧʹ�� TryRefreshCellRT ˢ�µ�����
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

        //����ˢ��������ӦCellRT����δ����ʾ�����
        public void TryRefreshCellRT(int index)
        {
            if (!m_CellRTDict.ContainsKey(index)) { return; }
            m_OnShowCell?.Invoke(index);                        //Cell����/ˢ�»ص�
        }

        //ˢ��������ӦCellRT
        public void RefreshCellRT(int index)
        {
            Debug.Assert(m_CellRTDict.ContainsKey(index));
            m_OnShowCell?.Invoke(index);                        //Cell����/ˢ�»ص�
        }

        //���Ի�ȡ������ӦCellRT����δ����ʾ�򷵻�false
        public bool TryGetCellRT(int index, out RectTransform cellRT)
        {
            return m_CellRTDict.TryGetValue(index, out cellRT);
        }

        //ȡ������ӦCellRT
        public RectTransform GetCellRT(int index)
        {
            Debug.Assert(m_CellRTDict.ContainsKey(index));
            return m_CellRTDict[index];
        }

        //������ӦCell��ǰ�Ƿ�������ʾ
        public bool IsCellRTShowing(int index)
        {
            return m_CellRTDict.ContainsKey(index);
        }

        /// <summary>
        /// ������ӦCell��ת��0����Cell��λ��
        /// </summary>
        /// <param name="index">Ŀ������</param>
        /// <param name="immediately">�Ƿ�������ת</param>
        public void JumpTo(int index, bool immediately = false)
        {
            Vector2 cellPos0 = GetCellPos(0);
            Vector2 cellPosI = GetCellPos(index);
            Vector2 deltaXY = new Vector2(Mathf.Abs(cellPosI.x - cellPos0.x), Mathf.Abs(cellPosI.y - cellPos0.y)); //index���0λ�ã�x��y �����
            Vector2 limitXY = new Vector2(Mathf.Max(m_Content.rect.size.x - m_Viewport.rect.size.x, 0), Mathf.Max(m_Content.rect.size.y - m_Viewport.rect.size.y, 0)); //x��y ���ƴ�С��Mathf.Maxͬʱ���ݡ�Content��ViewportС���͡�Content��Viewport���������
            Vector2 jumpToXY = new Vector2(Mathf.Min(deltaXY.x, limitXY.x), Mathf.Min(deltaXY.y, limitXY.y)); //���������ƴ�С

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

        //�����������ʼ���䣬����Content��ê�㡢���ĵ㡢λ�úʹ�С
        protected void ResetContentRT()
        {
            // �����������ʼ��������ê�㡢���ĵ�
            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                int cornerX = (int)m_StartCorner % 2;  //0���� 1��
                m_Content.anchorMin = new Vector2(cornerX, 0);
                m_Content.anchorMax = new Vector2(cornerX, 1);
                m_Content.pivot = new Vector2(cornerX, 0.5f);
            }
            else
            {
                int cornerY = (int)m_StartCorner / 2;  //0���ϣ� 1��
                m_Content.anchorMin = new Vector2(0, 1 - cornerY);
                m_Content.anchorMax = new Vector2(1, 1 - cornerY);
                m_Content.pivot = new Vector2(0.5f, 1 - cornerY);
            }

            // λ�ù�0
            m_Content.anchoredPosition = Vector2.zero;

            // ��С����Ϊ��Viewport��ͬ
            m_Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_Viewport.rect.width);
            m_Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_Viewport.rect.height);
        }

        protected void ResetTracker()
        {
            m_Tracker.Clear();
        }

        //�����߾�
        protected virtual void FixPadding() { }

        //�������
        protected virtual void FixSpacing() { }

        //����ֱ������������Ȼ�������ϣ�
        protected void CalcCellCountOnNaturalAxis()
        {
            this.m_ActualCellCountX = m_MovementAxis == MovementAxis.Horizontal ? m_CellCount : 1;
            this.m_ActualCellCountY = m_MovementAxis == MovementAxis.Vertical ? m_CellCount : 1;
        }

        //����ʵ����Ҫ�Ŀռ��С������padding�� �� ������ռ��ϵ�һ��Ԫ�����ڵ�λ��
        protected void CalculateRequiredSpace()
        {
            Vector2 requiredSpace = new Vector2(
                m_ActualCellCountX * m_CellRect.size.x + (m_ActualCellCountX - 1) * spacing.x,
                m_ActualCellCountY * m_CellRect.size.y + (m_ActualCellCountY - 1) * spacing.y
            );
            this.m_RequiredSpace = requiredSpace;
        }

        //���û����᷽���Content��С
        protected virtual void SetContentSizeOnMovementAxis()
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

        //����Cell��ʼOffset
        //ע�⣺ʹ Ԫ�ض��뷽ʽֻӰ�� �ǻ����᷽��
        //��Ϊ�����᷽�� Content��С��Ԫ��������������ͬ��UGUI��Layout,�������������ɴ�С��
        protected void CalculateCellStartOffset()
        {
            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                m_CellStartOffset = new Vector2(
                    padding.left + m_CellStartOffsetOnMovementAxis, 
                    GetCellStartOffset((int)MovementAxis.Vertical, m_RequiredSpace.y)
                );
            }
            else
            {
                m_CellStartOffset = new Vector2(
                    GetCellStartOffset((int)MovementAxis.Horizontal, m_RequiredSpace.x), 
                    padding.top + m_CellStartOffsetOnMovementAxis
                );
            }
        }

        protected virtual void SetContentStartPos()
        {
            m_Content.anchoredPosition = Vector2.zero;
        }

        //����Ӧ���ֵ�������Ӧ��ʧ������ �� δ�������
        protected void CalcIndexes()
        {
            int cornerX = (int)m_StartCorner % 2;  //0���� 1��
            int cornerY = (int)m_StartCorner / 2;  //0���ϣ� 1��

            int outCountFromStart = 0;  //��ȫ������ʼ�߽������
            int outCountFromEnd = 0;    //��ȫ���������߽������

            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                //content��ʼ�߽� ����� viewport��ʼ�߽��λ�ƿ�ȣ�
                float outWidthFromStart = -m_Content.anchoredPosition.x * (cornerX == 0 ? 1 : -1);
                float startPadding = cornerX == 0 ? padding.left : padding.right;
                //������������Ҫ����ȡ������������Ϊ��û�������Ա�֤���������ڵ���ȷ�ԡ�
                outCountFromStart = Mathf.FloorToInt((outWidthFromStart - startPadding + spacing.x) / (m_CellRect.size.x + spacing.x));
            }
            else
            {
                //content��ʼ�߽� ����� viewport��ʼ�߽��λ�ƿ�ȣ�
                float outHeightFromStart = -m_Content.anchoredPosition.y * (cornerY == 0 ? -1 : 1);
                float startPadding = cornerY == 0 ? padding.top : padding.bottom;
                //������������Ҫ����ȡ������������Ϊ��û�������Ա�֤���������ڵ���ȷ�ԡ�
                outCountFromEnd = Mathf.FloorToInt((outHeightFromStart - startPadding + spacing.y) / (m_CellRect.size.y + spacing.y));
            }

            //Ӧ����ʾ�Ŀ�ʼ�����ͽ�������
            int startIndex = (outCountFromStart); // ʡ������+1��-1�� �ӻ�������һ����ʼ��������0��ʼ;
            int endIndex = (m_CellCount - 1 - outCountFromEnd);

            //Debug.Log("startIndex, endIndex: " + startIndex + ", " + endIndex);

            for (int index = startIndex; index <= endIndex; index++)
            {
                m_NewIndexes.Add(index);
            }

            ////�¾������б��������
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

            //�ҳ����ֵġ���ʧ�ĺ�δ���
            //���ֵģ������б��У����������б��С�
            m_AppearIndexes.Clear();
            foreach (int index in m_NewIndexes)
            {
                if (m_OldIndexes.IndexOf(index) < 0)
                {
                    //Debug.Log("���֣�" + index);
                    m_AppearIndexes.Add(index);
                }
            }

            //��ʧ�ģ������б��У����������б��С�
            m_DisAppearIndexes.Clear();
            foreach (int index in m_OldIndexes)
            {
                if (m_NewIndexes.IndexOf(index) < 0)
                {
                    //Debug.Log("��ʧ��" + index);
                    m_DisAppearIndexes.Add(index);
                }
            }

            //���ֵģ��������б��У��������б��е�
            m_StayIndexes.Clear();
            foreach (int index in m_NewIndexes)
            {
                if (m_OldIndexes.IndexOf(index) >= 0)
                {
                    //Debug.Log("���֣�" + index);
                    m_StayIndexes.Add(index);
                }
            }

            //�� m_OldIndexes ���浱ǰ֡�������ݡ�
            //���������б���֤��������
            List<int> temp = m_OldIndexes;
            m_OldIndexes = m_NewIndexes;
            m_NewIndexes = temp;
            m_NewIndexes.Clear();
        }

        //����ʧ����ʧ
        protected void DisAppearCells()
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

        //�ó��ֵĳ���
        protected void AppearCells()
        {
            foreach (int index in m_AppearIndexes)
            {
                if (!IsValidIndex(index)) { continue; }

                RectTransform cellRT = GetOrCreateCell(index);
                m_CellRTDict[index] = cellRT;
                cellRT.anchoredPosition = GetCellPos(index);        //����Cellλ��

                int validIndex = ConvertIndexToValid(index);
                m_OnShowCell?.Invoke(validIndex);                   //Cell����/ˢ�»ص�
            }
        }

        //ˢ�±��ֵ�
        protected void RefreshStayCells()
        {
            foreach (int index in m_StayIndexes)
            {
                RectTransform cellRT = m_CellRTDict[index];
                cellRT.anchoredPosition = GetCellPos(index);        //����Cellλ��
                m_OnShowCell?.Invoke(index);                        //Cell����/ˢ�»ص�
            }
        }

        //���㲢����Cells��SblingIndex
        //����ʱ�������µ�Cell����ʱ
        //Cell�����ص�ʱ����
        //�������󣬿�ȥ���Խ�ʡ����
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
                //��index����
                return x.Key - y.Key;
            });

            foreach (KeyValuePair<int, RectTransform> kvp in m_CellRTListForSort)
            {
                //�����������
                //kvp.Value.SetAsLastSibling();
                //�����������
                kvp.Value.SetAsFirstSibling();
            }
        }

        //�Ƿ���Ч������ֻ����ʾ������ʾ���б��У�Ĭ��Ϊ 0~cellCount ֮�䣩
        protected virtual bool IsValidIndex(int index)
        {
            return index >= 0 && index < m_CellCount;
        }

        //ת����������Ч��Ĭ�����账��
        protected virtual int ConvertIndexToValid(int index)
        {
            return index;
        }

        //���� ��ʼ�Ų�Cell����ʼλ�ã�����Ϊ��Cell�� ��ʣ����óߴ硱����ζ��룩
        protected float GetCellStartOffset(int axis, float requiredSpaceWithoutPadding)
        {
            float requiredSpace = requiredSpaceWithoutPadding + (axis == 0 ? padding.horizontal : padding.vertical);  //��������Ԫ����Ҫ���ܳߴ� + �߾�
            float availableSpace = m_Content.rect.size[axis];       //������ Content ��ʵ�ʿ��óߴ�
            float surplusSpace = availableSpace - requiredSpace;    //ʣ����óߴ磨�����Ǹ��ģ�
            float alignmentOnAxis = GetAlignmentOnAxis(axis);       //��ȡС����ʽ����Ԫ�ض��뷽ʽ

            //ˮƽ�������ʼ����ֱ������Ͽ�ʼ��
            // Ҫ����ʣ��ߴ硣��ˮƽ����Ϊ����
            // �����뷽ʽΪ������ alignmentOnAxis Ϊ 0�� ���Ϊ padding.left + 0�����Դﵽ����Ч����
            // �����뷽ʽΪ���У��� alignmentOnAxis Ϊ 0.5�� ���Ϊ padding.left + 0.5*ʣ����룬���Դﵽ����Ч����
            // �����뷽ʽΪ���ң��� alignmentOnAxis Ϊ 1�� ���Ϊ padding.left + 1*ʣ����룬���Դﵽ����Ч����
            float cellStartOffset = (axis == 0 ? padding.left : padding.top) + surplusSpace * alignmentOnAxis;

            return cellStartOffset;
        }

        // Returns the alignment on the specified axis as a fraction where 0 is left/top, 0.5 is middle, and 1 is right/bottom.
        // ��С����ʽ����ָ�����ϵĶ��뷽ʽ������0Ϊ��/�ϣ�0.5Ϊ�У�1Ϊ��/�¡���ˮƽ����0��0.5�У�1�ң�����ֱ����0�ϣ�0.5�У�1�£�
        // ���� "axis"��The axis to get alignment along. 0 is horizontal and 1 is vertical.    //��������0��ˮƽ�ģ�1�Ǵ�ֱ�ġ�
        // ����ֵ��The alignment as a fraction where 0 is left/top, 0.5 is middle, and 1 is right/bottom. //С����ʽ�Ķ��뷽ʽ
        protected float GetAlignmentOnAxis(int axis)
        {
            return (axis == (int)m_MovementAxis) ? 0.5f : (int)childAlignment * 0.5f;
        }

        protected Vector2 GetCellPos(int index)
        {
            //һ����������תλ������
            int posIndexX;   //Xλ������
            int posIndexY;   //Yλ������
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

            //����������ʼ�������ת��
            if (m_MovementAxis == MovementAxis.Horizontal && m_StartCorner == Corner.RightOrLower) { posIndexX = m_ActualCellCountX - 1 - posIndexX; }   //����Ǵ�������
            if (m_MovementAxis == MovementAxis.Vertical && m_StartCorner == Corner.RightOrLower) { posIndexY = m_ActualCellCountY - 1 - posIndexY; }   //����Ǵ�������

            //������������
            Vector2 scaleFactor = Vector2.one;  //������Ԫ������

            // x�᣺��ʼλ��+���*���ĵ�ƫ��*����ϵ�� (x������������)(�����ϵ�����)
            float anchoredPosX = (m_CellStartOffset.x + (m_CellRect.size.x + spacing.x) * posIndexX) + m_CellRect.size.x * m_CellPivot.x * scaleFactor.x;

            // y�᣺-��ʼλ��-���*(1-���ĵ�ƫ��)*����ϵ�� (y�����򸺷���)(�����ϵ�����)
            float anchoredPosY = -(m_CellStartOffset.y + (m_CellRect.size.y + spacing.y) * posIndexY) - m_CellRect.size.y * (1f - m_CellPivot.y) * scaleFactor.y;

            Debug.Log($"index: {index}, posIndexX: {posIndexX}, posIndexY: {posIndexY}, anchoredPosX: {anchoredPosX}, anchoredPosY: {anchoredPosY}, m_StartOffset.x: {m_CellStartOffset.x}");

            return new Vector2(anchoredPosX, anchoredPosY);
        }

        protected void SetProperty<T>(ref T currentValue, T newValue)
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))  //������Ч��δ��
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

                //�����������ê���λ��
                m_Tracker.Add(this, cellRT, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta);

                //ǿ������Cell��anchor
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
