using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NRatel
{
    public class UIPageView : UIListView
    {
        [SerializeField] private bool m_CellOccupyPage = false;           //ʹCellռ��һҳ��ǿ�轫spacing.x��

        [SerializeField] private bool m_Snap = false;                     //����Snap��
        [SerializeField] private float m_SnapSpeed = 500f;                //Snap�ٶ�
        [SerializeField] private float m_SnapWaitScrollSpeed = 50f;       //��������ʱ���ȴ�����ͣ�Ȳſ�ʼSnap

        [SerializeField] private bool m_Carousel = false;                 //�����ֲ���
        [SerializeField] private float m_CarouselInterval = 3f;           //�ֲ��������
        [SerializeField] private float m_CarouselSpeed = 500f;            //�ֲ�ʱ�ƶ����ٶ�

        private int m_CurPage = 0;
        private Coroutine m_SnapCoroutine;
        private Coroutine m_CarouselCoroutine;

        public event Action onSnapCompleted;

        #region Override
        //�����߾ࣨע��ֻ������������
        protected override void FixPadding()
        {
            if (m_Loop) 
            {
                base.FixPadding();
            }
            else
            {
                if (m_MovementAxis == MovementAxis.Horizontal)
                {
                    int fixedPaddingX = Mathf.FloorToInt((m_Viewport.rect.width - m_CellRect.width) / 2);
                    padding.left = padding.right = fixedPaddingX; 
                }
                else 
                {
                    int fixedPaddingY = Mathf.FloorToInt((m_Viewport.rect.height - m_CellRect.height) / 2);
                    padding.top = padding.bottom = fixedPaddingY; 
                }  
            }
        }

        //������ࣨע��ֻ������������
        protected override void FixSpacing()
        {
            if (!m_CellOccupyPage) { return; }

            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                float fixedSpacingX = m_Viewport.rect.width - m_CellRect.width;
                spacing = new Vector2(fixedSpacingX, spacing.y); 
            }
            else
            {
                float fixedSpacingY = m_Viewport.rect.height - m_CellRect.height;
                spacing = new Vector2(spacing.x, fixedSpacingY); 
            }
        }

        protected override void OnStartShow()
        {
            if (m_CellCount > 0) { TryStartCarousel(); }
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

        #region Snap
        private void TryStartSnap()
        {
            if (!m_Snap) { return; }
            m_SnapCoroutine = StartCoroutine(SnapRoutine());
        }

        private void TryStopSnap()
        {
            if (!m_Snap) { return; }
            if (m_SnapCoroutine == null) { return; }

            StopCoroutine(m_SnapCoroutine);
            m_SnapCoroutine = null;
        }

        private IEnumerator SnapRoutine()
        {
            //Debug.Log($"��SnapRoutine��Snap ����");
            //��������ص��������ȵȴ��ص�����
            //loopģʽ�£��ص������ϲ�����Ч
            if (movementType == MovementType.Elastic)
            {
                if (m_MovementAxis == MovementAxis.Horizontal)
                {
                    float leftPos;
                    float rightPos;
                    float offsetThreshold = 0.1f;

                    if (m_StartCorner == StartCorner.LeftOrUpper) 
                    {
                        leftPos = 0;
                        rightPos = -(m_Content.rect.width - m_Viewport.rect.width);
                    }
                    else 
                    {
                        leftPos = m_Content.rect.width - m_Viewport.rect.width;
                        rightPos = 0;
                    }

                    //��ǰ��Content����������߽�ص�
                    if (m_Content.anchoredPosition.x > leftPos)
                    {
                        yield return new WaitUntil(() => { return m_Content.anchoredPosition.x <= leftPos + offsetThreshold; });
                    }
                    //��ǰ��Content���������ұ߽�ص�
                    else if (m_Content.anchoredPosition.x < rightPos)
                    {
                        yield return new WaitUntil(() => { return m_Content.anchoredPosition.x >= rightPos - offsetThreshold; });
                    }
                }
                else 
                {
                    float upPos = 0;
                    float downPos = m_Content.rect.height - m_Viewport.rect.height;
                    float offsetThreshold = 0.1f;

                    if (m_StartCorner == StartCorner.LeftOrUpper)
                    {
                        upPos = 0;
                        downPos = m_Content.rect.height - m_Viewport.rect.height;
                    }
                    else
                    {
                        upPos = -(m_Content.rect.height - m_Viewport.rect.height);
                        downPos = 0;
                    }

                    //��ǰ��Content���������ϱ߽�ص�
                    if (m_Content.anchoredPosition.y < upPos)
                    {
                        yield return new WaitUntil(() => { return m_Content.anchoredPosition.y <= upPos + offsetThreshold; });
                    }
                    //��ǰ��Content���������±߽�ص�
                    else if (m_Content.anchoredPosition.y > downPos)
                    {
                        yield return new WaitUntil(() => { return m_Content.anchoredPosition.y >= downPos - offsetThreshold; });
                    }
                }
                Debug.Log($"��SnapRoutine���ȴ��ص�����");
            }

            //����������ԣ������ȵȴ������ͣ��
            if (inertia)
            {
                yield return new WaitUntil(() =>
                {
                    //Debug.Log("scrollRect.velocity: " + scrollRect.velocity);
                    if (m_MovementAxis == MovementAxis.Horizontal) { return Mathf.Abs(velocity.x) < m_SnapWaitScrollSpeed; }
                    else { return Mathf.Abs(velocity.y) < m_SnapWaitScrollSpeed; }
                });
                Debug.Log($"��SnapRoutine���ȴ������ƶ�����");
            }

            #region ����Viewport����������Ǹ�Cell��
            //ע����������λ�ü���Ҫ�� Content��Viewportû������
            Debug.Assert(m_Content.localScale == Vector3.one);
            Debug.Assert(m_Viewport.localScale == Vector3.one);

            float minDistance = Mathf.Infinity;
            int minDistanceIndex = -1;
            foreach (var t in m_CellRTDict)
            {
                float distanceToViewportCenter;
                if (m_MovementAxis == MovementAxis.Horizontal)
                {
                    //Cell����Content��߽��λ�ƣ�����Ϊ������
                    //ע�⣬���� Cell�� pivot Ӱ�조Cell����Viewport���ġ��ĸ���
                    //������Ӱ�죬���Կ��ǼӸ�boolѡ����������޴����󣩡�
                    float distanceFromContentLeft = t.Value.anchoredPosition.x;
                    //Cell����Viewport��߽��λ�ƣ�����Ϊ������
                    float distanceFromViewportLeft = distanceFromContentLeft + m_Content.anchoredPosition.x;
                    //Cell����Viewport���ĵ�λ�ƣ�����Ϊ��������>0���������ĵ��ұߣ�
                    distanceToViewportCenter = distanceFromViewportLeft - m_Viewport.rect.width / 2f;
                }
                else 
                {
                    //Cell����Content�ϱ߽�ľ��루����Ϊ������
                    //ע�⣬���� Cell�� pivot Ӱ�조Cell����Viewport���ġ��ĸ��
                    //������Ӱ�죬���Կ��ǼӸ�boolѡ����������޴����󣩡�
                    float distanceFromContentUp = t.Value.anchoredPosition.y;
                    //Cell����Viewport�ϱ߽�ľ��루����Ϊ������
                    float distanceFromViewportUp = distanceFromContentUp + m_Content.anchoredPosition.y;
                    //Cell����Viewport���ĵľ��루����Ϊ��������<0�������ĵ��±ߣ�
                    distanceToViewportCenter = distanceFromViewportUp + m_Viewport.rect.height / 2f;
                }

                //Debug.Log($"��SnapRoutine��, index: {t.Key}, distanceToViewportCenter: {distanceToViewportCenter}");

                if (Mathf.Abs(distanceToViewportCenter) < Mathf.Abs(minDistance))
                {
                    minDistance = distanceToViewportCenter;
                    minDistanceIndex = t.Key;
                }
            }
            #endregion

            // ֻ�轫 content �����ƶ� minDistance��
            // ��ע�� loop ������λ�ã�
            // ��˲��ܡ�ֱ�Ӽ����Ŀ��λ�ã�Ȼ���ֵ��
            // ����Ҫ��ÿ֡��������ƫ�ƣ�ֱ���ӹ�����

            // ����ƻ��ƶ�����
            float planMoveDistance = m_MovementAxis == MovementAxis.Horizontal ? -minDistance : minDistance;
            //Debug.Log($"��SnapRoutine��Snap ��ʼ��Ŀ������:{minDistanceIndex}, �ƶ�����: {planMoveDistanceX}");

            yield return DoMoveContentPosOnMovementAxis(planMoveDistance, m_SnapSpeed);

            //Debug.Log($"��SnapRoutine��Snap ����");

            m_CurPage = minDistanceIndex;
            onSnapCompleted?.Invoke();
            TryStartCarousel();
        }
        #endregion

        #region Carousel
        private void TryStartCarousel()
        {
            if (!m_Carousel) { return; }
            m_CarouselCoroutine = StartCoroutine(CarouselRoutine());
        }

        private void TryStopCarousel()
        {
            if (!m_Carousel) { return; }
            if (m_CarouselCoroutine == null) { return; }

            StopCoroutine(m_CarouselCoroutine);
            m_CarouselCoroutine = null;
        }

        private IEnumerator CarouselRoutine()
        {
            // �ȴ��ֲ����
            yield return new WaitForSeconds(m_CarouselInterval);

            float pageSize = m_MovementAxis == MovementAxis.Horizontal ? m_CellRect.width + spacing.x : m_CellRect.height + spacing.y;
            int direction = m_MovementAxis == MovementAxis.Horizontal ? (m_StartCorner == StartCorner.LeftOrUpper ? -1 : 1) : (m_StartCorner == StartCorner.LeftOrUpper ? 1 : -1);

            // ����ƻ��ƶ����� �� �ٶȱ���
            float planMoveDistance;
            float speedRate = 1f;
            if (m_Loop)
            {
                // ����ѭ��ʱ��������󷭵���һҳ������Ҫע�� conentλ�ûᱻ����
                // �����ζ�ţ��߼������ǡ��ƶ���1ҳ���Ŀ��λ�á������ǡ�λ��������Ϊ1ҳ����
                // ע��������������ţ������˷�ҳ����
                planMoveDistance = pageSize * direction;
            }
            else
            {
                if (m_CurPage < m_CellCount - 1)
                {
                    // δ����ѭ��ʱ������ǰ���ڷ����һҳ���򷭵���һҳ
                    planMoveDistance = pageSize * direction;
                }
                else
                {
                    // δ����ѭ��ʱ������ǰ�������һҳ����Ѹ�ٷ��ص���һҳ
                    planMoveDistance = pageSize * (m_CellCount - 1) * -direction;
                    speedRate = m_CellCount - 1;
                }
            }

            //Debug.Log($"��CarouselRoutine��Carousel��ʼ, �ƶ�����: {planMoveDistanceX}");

            yield return DoMoveContentPosOnMovementAxis(planMoveDistance, m_CarouselSpeed * speedRate);

            //Debug.Log($"��CarouselRoutine��Carousel ����");

            //ִ�ж��룬��������󽫼��������ֲ�
            TryStartSnap();
        }
        #endregion

        private float m_MovedDistance = 0f;
        private IEnumerator DoMoveContentPosOnMovementAxis(float planMoveDistance, float speed)
        {
            //�����ۼ��ֶ�
            m_MovedDistance = 0f;

            //��ֹͣ�κι����ٶ�
            StopMovement();

            //�ٶȱ���ת����
            float velocity = speed * Mathf.Sign(planMoveDistance);

            //�ƶ�������
            Vector2 moveDirection = m_MovementAxis == MovementAxis.Horizontal ? Vector2.right : Vector2.down;

            // ƽ������λ��
            while (Mathf.Abs(m_MovedDistance) < Mathf.Abs(planMoveDistance))
            {
                float addDistance = velocity * Time.deltaTime;  //��Ҫ����ʱ�����ţ����� Time.unscaledDeltaTime;

                // ����Ƿ�ᳬ��Ŀ�����
                if (Mathf.Abs(m_MovedDistance + addDistance) >= Mathf.Abs(planMoveDistance))
                {
                    // ֱ�����õ���ȷλ�ã���break
                    float remainingDistance = planMoveDistance - m_MovedDistance;
                    m_Content.anchoredPosition += (moveDirection * remainingDistance);
                    m_MovedDistance = planMoveDistance;
                    break;
                }
                else
                {
                    m_MovedDistance += addDistance;
                    m_Content.anchoredPosition += (moveDirection * addDistance);
                }

                yield return null;
            }
        }
    }
}