using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NRatel
{
    public class UIPageView : UIListView
    {
        [Header("Page Settings")]
        [SerializeField] public bool loop = true;                      //����ѭ����
        [SerializeField] public bool cellOccupyPage = false;           //ʹCellռ��һҳ��ǿ�轫spacing.x��

        [Header("Snap Settings")]
        [SerializeField] public bool snap = false;                     //����Snap��
        [SerializeField] public float snapSpeed = 500f;                //Snap�ٶ�
        [SerializeField] public float snapWaitScrollSpeedX = 50f;      //��������ʱ���ȴ�����ͣ�Ȳſ�ʼSnap

        [Header("Carousel Settings")]
        [SerializeField] public bool carousel = false;                 //�����ֲ���
        [SerializeField] public float carouselInterval = 3f;           //�ֲ��������
        [SerializeField] public float carouselSpeed = 500f;            //�ֲ�ʱ�ƶ����ٶ�

        public event Action onSnapCompleted;

        private int m_CurPage = 0;
        private Coroutine m_SnapCoroutine;
        private Coroutine m_CarouselCoroutine;

        //�������ݿ��
        private float coreConetontWidth { get { return m_CellRect.width * m_CellCount + spacing.x * (m_CellCount - 1); } }

        //����loopʱ�����ÿ��
        private float loopResetWidth { get { return (coreConetontWidth + spacing.x) * Mathf.CeilToInt(m_Viewport.rect.width / coreConetontWidth); } }

        //����loopʱ����չ����
        private float expandedContentWidth { get { return coreConetontWidth + loopResetWidth * 4; } }

        protected override void Start()
        {
            base.Start();
            if (m_CellCount > 0) { TryStartCarousel(); }
        }

        #region Override
        //����loopʱ�����ڳ�ʼʱƫ��Content�ػ��������λ�ã�ʹ�������У����跴����� �׸�Cell��Content�ϵĳ�ʼλ��
        //protected override float m_CellStartOffsetOnMovementAxis { get { return (expandedContentWidth - coreConetontWidth) / 2f; } }

        protected override void OnScrollValueChanged(Vector2 delta)
        {
            if (m_CellCount > 0) { TryHandleLoopPos(); }
            base.OnScrollValueChanged(delta);       // ����ԭ���߼�
        }

        //�����߾ࣨע��ֻ������������
        protected override void FixPadding()
        {
            if (loop) 
            {
                if (m_MovementAxis == MovementAxis.Horizontal) { padding.left = padding.right = 0; }
                else { padding.top = padding.bottom = 0; }
            }
            else
            {
                int fixedPadding = Mathf.FloorToInt((m_Viewport.rect.width - m_CellRect.width) / 2);
                if (m_MovementAxis == MovementAxis.Horizontal) { padding.left = padding.right = fixedPadding; }
                else { padding.top = padding.bottom = fixedPadding; }  
            }
        }

        //������ࣨע��ֻ������������
        protected override void FixSpacing()
        {
            if (!cellOccupyPage) return;

            float fixedSpacing = m_Viewport.rect.width - m_CellRect.width;
            if (m_MovementAxis == MovementAxis.Horizontal) { spacing = new Vector2(fixedSpacing, spacing.y); }
            else { spacing = new Vector2(spacing.x, fixedSpacing); }
        }

        //���㲢����Content��С
        protected override void SetContentSizeOnMovementAxis()
        {
            if (loop)
            {
                RectTransform.Axis axis;
                float size;
                if (m_MovementAxis == MovementAxis.Horizontal)
                {
                    axis = RectTransform.Axis.Horizontal;
                    size = m_CellCount > 0 ? expandedContentWidth : 0;
                }
                else
                {
                    axis = RectTransform.Axis.Vertical;
                    size = m_CellCount > 0 ? expandedContentWidth : 0;  //todo
                }

                m_Content.SetSizeWithCurrentAnchors(axis, size);
            }
            else 
            {
                base.SetContentSizeOnMovementAxis();
            }
        }

        protected override void SetContentStartPos()
        {
            m_Content.anchoredPosition = new Vector2(-m_CellStartOffsetOnMovementAxis, m_Content.anchoredPosition.y);
        }

        //loopʱ����Ϊ��������������Ч�ģ���ʹ�� 0~m_CellCount �������ܹ���ʾԪ�أ�֮������ ConvertIndexToValid ת��
        protected override bool IsValidIndex(int index)
        {
            if (loop) { return true; }
            else { return base.IsValidIndex(index); }
        }

        //loopʱ��������������ת�� [0~m_CellCount-1] ��
        protected override int ConvertIndexToValid(int index)
        {
            if (loop) { return (index % m_CellCount + m_CellCount) % m_CellCount; }
            else { return base.ConvertIndexToValid(index); }
        }
        #endregion

        #region Drag Handling
        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);

            TryStopSnap();
            TryStopCarousel();
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);

            TryStartSnap();
        }
        #endregion

        #region Loop
        private void TryHandleLoopPos()
        {
            if (!loop) return;

            //Content��ʼλ��
            float contentStartPosX = -m_CellStartOffsetOnMovementAxis;
            //��ȡ��ǰλ��
            float curContentPosX = m_Content.anchoredPosition.x;
            //Content����ʱ��Content���õ����꣨��ʼλ�����1�����ÿ�ȣ�
            float leftResetPosX = contentStartPosX - loopResetWidth;
            //Content����ʱ��Content���õ����꣨��ʼλ���Ҳ�1�����ÿ�ȣ�
            float rightResetPosX = contentStartPosX + loopResetWidth;

            if (curContentPosX < leftResetPosX)
            {
                m_Content.anchoredPosition += Vector2.right * loopResetWidth;
            }
            //���󻬶�ʱ
            else if (curContentPosX > rightResetPosX)
            {
                m_Content.anchoredPosition += Vector2.left * loopResetWidth;
            }
        }
        #endregion

        #region Snap
        private void TryStartSnap()
        {
            if (!snap) { return; }
            m_SnapCoroutine = StartCoroutine(SnapRoutine());
        }

        private void TryStopSnap()
        {
            if (!snap) { return; }
            if (m_SnapCoroutine == null) { return; }

            StopCoroutine(m_SnapCoroutine);
            m_SnapCoroutine = null;
        }

        private IEnumerator SnapRoutine()
        {
            //Debug.Log($"��SnapRoutine��Snap ����");
            //��������ص��������ȵȴ��ص�����
            if (movementType == MovementType.Elastic)
            {
                float leftPosX = 0;
                float rightPosX = -(m_Content.rect.width - m_Viewport.rect.width);
                float offsetThreshold = 0.1f;

                //��ǰ������߽�ص�
                if (m_Content.anchoredPosition.x > leftPosX)
                {
                    yield return new WaitUntil(() => { return m_Content.anchoredPosition.x <= leftPosX + offsetThreshold; });
                }
                //��ǰ�����ұ߽�ص�
                else if (m_Content.anchoredPosition.x < rightPosX)
                {
                    yield return new WaitUntil(() => { return m_Content.anchoredPosition.x > rightPosX - offsetThreshold; });
                }
                //Debug.Log($"��SnapRoutine���ȴ��ص�����");
            }

            //����������ԣ������ȵȴ������ͣ��
            if (inertia)
            {
                yield return new WaitUntil(() =>
                {
                    //Debug.Log("scrollRect.velocity.x: " + scrollRect.velocity.x);
                    return Mathf.Abs(velocity.x) < snapWaitScrollSpeedX;
                });
                //Debug.Log($"��SnapRoutine���ȴ�����ͣ��");
            }

            #region ����Viewport����������Ǹ�Cell��
            //ע����������λ�ü���Ҫ�� Content��Viewportû������
            Debug.Assert(m_Content.localScale == Vector3.one);
            Debug.Assert(m_Viewport.localScale == Vector3.one);

            float minDistance = Mathf.Infinity;
            int minDistanceIndex = -1;
            foreach (var t in m_CellRTDict)
            {
                //Cell����Content��߽�ľ��루������
                //ע�⣬���� Cell�� pivot Ӱ�조Cell����Viewport���ġ��ĸ��������Ӱ�죬���ǼӸ�boolѡ�������
                float widthFromContentLeft = t.Value.anchoredPosition.x;
                //Cell����Viewport��߽�ľ��루������
                float widthFromViewportLeft = widthFromContentLeft + m_Content.anchoredPosition.x;
                //Cell����Viewport���ĵľ��루ʸ������>0�������ĵ��ұߣ�
                float distanceToViewportCenter = widthFromViewportLeft - m_Viewport.rect.width / 2f;

                //Debug.Log($"��SnapRoutine��, index: {t.Key}, distanceToViewportCenter: {distanceToViewportCenter}");

                if (Mathf.Abs(distanceToViewportCenter) < Mathf.Abs(minDistance))
                {
                    minDistance = distanceToViewportCenter;
                    minDistanceIndex = t.Key;
                }
            }

            //���ݿ�������ʱ��������ȫ��������Ļ�����
            #endregion

            // ֻ�轫 content �����ƶ� minDistance��
            // ��ע�� loop ������λ�ã�
            // ��˲��ܡ�ֱ�Ӽ����Ŀ��λ�ã�Ȼ���ֵ��
            // ����Ҫ��ÿ֡��������ƫ�ƣ�ֱ���ӹ�����

            // ����ƻ��ƶ�����
            float planMoveDistanceX = -minDistance;
            //Debug.Log($"��SnapRoutine��Snap ��ʼ��Ŀ������:{minDistanceIndex}, �ƶ�����: {planMoveDistanceX}");

            yield return DoMoveContentPosX(planMoveDistanceX, snapSpeed);

            //Debug.Log($"��SnapRoutine��Snap ����");

            m_CurPage = minDistanceIndex;
            onSnapCompleted?.Invoke();
            TryStartCarousel();
        }
        #endregion

        #region Carousel
        private void TryStartCarousel()
        {
            if (!carousel) { return; }
            m_CarouselCoroutine = StartCoroutine(CarouselRoutine());
        }

        private void TryStopCarousel()
        {
            if (!carousel) { return; }
            if (m_CarouselCoroutine == null) { return; }

            StopCoroutine(m_CarouselCoroutine);
            m_CarouselCoroutine = null;
        }

        private IEnumerator CarouselRoutine()
        {
            // �ȴ��ֲ����
            yield return new WaitForSeconds(carouselInterval);

            // ����ƻ��ƶ����� �� �ٶȱ���
            float planMoveDistanceX;
            float speedRate = 1f;
            if (loop)
            {
                // ����ѭ��ʱ��������󷭵���һҳ������Ҫע�� conentλ�ûᱻ����
                // �����ζ�ţ��߼������ǡ��ƶ���1ҳ���Ŀ��λ�á������ǡ�λ��������Ϊ1ҳ����
                planMoveDistanceX = -(m_CellRect.width + spacing.x);
            }
            else
            {
                if (m_CurPage < m_CellCount - 1)
                {
                    // δ����ѭ��ʱ������ǰ���ڷ����һҳ���򷭵���һҳ
                    planMoveDistanceX = -(m_CellRect.width + spacing.x);
                }
                else
                {
                    // δ����ѭ��ʱ������ǰ�������һҳ����Ѹ�ٷ��ص���һҳ
                    planMoveDistanceX = (m_CellRect.width + spacing.x) * (m_CellCount - 1);
                    speedRate = m_CellCount - 1;
                }
            }

            //Debug.Log($"��CarouselRoutine��Carousel��ʼ, �ƶ�����: {planMoveDistanceX}");

            yield return DoMoveContentPosX(planMoveDistanceX, carouselSpeed * speedRate);

            //Debug.Log($"��CarouselRoutine��Carousel ����");

            //ִ�ж��룬��������󽫼��������ֲ�
            TryStartSnap();
        }
        #endregion

        private float movedDistanceX = 0f;
        private IEnumerator DoMoveContentPosX(float planMoveDistanceX, float speed)
        {
            //��ֹͣ�κι����ٶ�
            StopMovement();  //m_Velocity = Vector2.zero

            //�����ۼ��ֶ�
            movedDistanceX = 0f;

            // �ٶȱ���ת����
            float velocity = speed * Mathf.Sign(planMoveDistanceX);

            // ƽ������λ��
            while (Mathf.Abs(movedDistanceX) < Mathf.Abs(planMoveDistanceX))
            {
                float addX = velocity * Time.deltaTime; //��Ҫ����ʱ�����ţ����� Time.unscaledDeltaTime;

                // ����Ƿ�ᳬ��Ŀ�����
                if (Mathf.Abs(movedDistanceX + addX) >= Mathf.Abs(planMoveDistanceX))
                {
                    // ֱ�����õ���ȷλ�ã���break
                    float remainingDistance = planMoveDistanceX - movedDistanceX;
                    m_Content.anchoredPosition += new Vector2(remainingDistance, 0);
                    movedDistanceX = planMoveDistanceX;
                    break;
                }
                else
                {
                    movedDistanceX += addX;
                    m_Content.anchoredPosition += new Vector2(addX, 0);
                }

                yield return null;
            }
        }
    }
}