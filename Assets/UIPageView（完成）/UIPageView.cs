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
        [SerializeField] private bool m_Reverse = false;                  //�����ֲ���Ĭ���ǰ�Cell����˳��

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
            yield return WaitUtilElasticEnd();

            //����������ԣ������ȵȴ������ͣ��
            yield return WaitUtilInertiaEnd();

            //����Viewport����������Ǹ�Cell��
            //ע����������λ�ü���Ҫ�� Content��Viewportû������
            Debug.Assert(m_Content.localScale == Vector3.one);
            Debug.Assert(m_Viewport.localScale == Vector3.one);
            var closestCell = FindClosestCellToViewCenterOnMovementAxis();

            // ����ƻ��ƶ�����
            // ֻ�轫 content �����ƶ� closestCell.distance��
            // ��ע�� loop ������λ�ã���˲��ܡ�ֱ�Ӽ����Ŀ��λ�ã�Ȼ���ֵ�� ����Ҫ��ÿ֡��������ƫ�ƣ�ֱ���ӹ�������
            float planMoveDistance = -closestCell.distance;

            //Debug.Log($"��SnapRoutine��Snap ��ʼ��Ŀ������:{minDistanceIndex}, �ƶ�����: {planMoveDistanceX}");

            yield return DoMoveContentPosOnMovementAxis(planMoveDistance, m_SnapSpeed);

            //Debug.Log($"��SnapRoutine��Snap ����");

            m_CurPage = closestCell.index;
            onSnapCompleted?.Invoke();
            TryStartCarousel();
        }

        private IEnumerator WaitUtilElasticEnd()
        {
            if (movementType != MovementType.Elastic) yield break;

            var threshold = 0.1f;
            var pos = m_Content.anchoredPosition;

            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                var bounds = GetHorizontalElasticBounds();
                if (pos.x > bounds.left) yield return WaitUntil(() => pos.x <= bounds.left + threshold);
                if (pos.x < bounds.right) yield return WaitUntil(() => pos.x >= bounds.right - threshold);
            }
            else
            {
                var bounds = GetVerticalElasticBounds();
                if (pos.y < bounds.up) yield return WaitUntil(() => pos.y <= bounds.up + threshold);
                if (pos.y > bounds.down) yield return WaitUntil(() => pos.y >= bounds.down - threshold);
            }
        }

        private IEnumerator WaitUtilInertiaEnd()
        {
            if (!inertia) yield break;

            yield return WaitUntil(() =>
            {
                //Debug.Log("scrollRect.velocity: " + scrollRect.velocity);
                float velocityOnMoveAxis = m_MovementAxis == MovementAxis.Horizontal ? velocity.x : velocity.y;
                return Mathf.Abs(velocityOnMoveAxis) < m_SnapWaitScrollSpeed;
            });
        }

        //��ȡˮƽ�ص��߽�
        private (float left, float right) GetHorizontalElasticBounds()
        {
            return m_StartCorner == StartCorner.LeftOrUpper
                ? (0, -(m_Content.rect.width - m_Viewport.rect.width))
                : (m_Content.rect.width - m_Viewport.rect.width, 0);
        }

        //��ȡ��ֱ�ص��߽�
        private (float up, float down) GetVerticalElasticBounds()
        {
            return m_StartCorner == StartCorner.LeftOrUpper
                ? (0, m_Content.rect.height - m_Viewport.rect.height)
                : (-(m_Content.rect.height - m_Viewport.rect.height), 0);
        }

        //������View����������Ǹ�Cell�����������ϣ�
        private (int index, float distance) FindClosestCellToViewCenterOnMovementAxis()
        {
            var closestIndex = -1;
            var minDistance = float.MaxValue;

            foreach (var cell in m_CellRTDict)
            {
                var distance = CalcCellDistanceToViewCenterMovementAxis(cell.Value);
                if (Mathf.Abs(distance) >= Mathf.Abs(minDistance)) continue;

                minDistance = distance;
                closestIndex = cell.Key;
            }
            return (closestIndex, minDistance);
        }

        //����Cell��View���ĵľ��루���������ϣ�
        private float CalcCellDistanceToViewCenterMovementAxis(RectTransform cell)
        {
            float distanceToViewCenter;
            if (m_MovementAxis == MovementAxis.Horizontal)
            {
                if (m_StartCorner == StartCorner.LeftOrUpper)
                {
                    //Cell����Content��߽��λ�ƣ�����Ϊ������
                    //ע�⣬���� Cell�� pivot Ӱ�조Cell����Viewport���ġ��ĸ���
                    //������Ӱ�죬���Կ��ǼӸ�boolѡ����������޴����󣩡�
                    float distanceFromContentLeft = cell.anchoredPosition.x;
                    //Cell����Viewport��߽��λ�ƣ�����Ϊ������
                    float distanceFromViewportLeft = distanceFromContentLeft + m_Content.anchoredPosition.x;
                    //Cell����Viewport���ĵ�λ�ƣ�����Ϊ�����򣩣����>0ʱ�������ĵ��ұߣ�
                    distanceToViewCenter = distanceFromViewportLeft - m_Viewport.rect.width / 2f;
                }
                else
                {
                    //Cell����Content�ұ߽��λ�ƣ�����Ϊ������
                    //ע�⣬���� Cell�� pivot Ӱ�조Cell����Viewport���ġ��ĸ���
                    //������Ӱ�죬���Կ��ǼӸ�boolѡ����������޴����󣩡�
                    float distanceFromContentLeft = cell.anchoredPosition.x;
                    //Cell����Viewport��߽��λ�ƣ�����Ϊ������
                    float distanceFromViewportLeft = distanceFromContentLeft + m_Content.anchoredPosition.x - (m_Content.rect.width - m_Viewport.rect.width);
                    //Cell����Viewport���ĵ�λ�ƣ�����Ϊ�����򣩣����>0ʱ�������ĵ��ұߣ�
                    distanceToViewCenter = distanceFromViewportLeft - m_Viewport.rect.width / 2f;
                }
            }
            else
            {
                if (m_StartCorner == StartCorner.LeftOrUpper)
                {
                    //Cell����Content�ϱ߽�ľ��루����Ϊ������
                    //ע�⣬���� Cell�� pivot Ӱ�조Cell����Viewport���ġ��ĸ��
                    //������Ӱ�죬���Կ��ǼӸ�boolѡ����������޴����󣩡�
                    float distanceFromContentUp = cell.anchoredPosition.y;
                    //Cell����Viewport�ϱ߽�ľ��루����Ϊ������
                    float distanceFromViewportUp = distanceFromContentUp + m_Content.anchoredPosition.y;
                    //Cell����Viewport���ĵľ��루����Ϊ�����򣩣����0ʱ�������ĵ��ϱߣ�
                    distanceToViewCenter = distanceFromViewportUp + m_Viewport.rect.height / 2f;
                }
                else
                {
                    //Cell����Content�±߽�ľ��루����Ϊ������
                    //ע�⣬���� Cell�� pivot Ӱ��"Cell����Viewport����"�ĸ���
                    float distanceFromContentUp = cell.anchoredPosition.y;
                    //Cell����Viewport�±߽�ľ��루����Ϊ������
                    float distanceFromViewportUp = distanceFromContentUp + m_Content.anchoredPosition.y + (m_Content.rect.height - m_Viewport.rect.height);
                    //Cell����Viewport���ĵľ��루����Ϊ�����򣩣����0ʱ�������ĵ��ϱߣ�
                    distanceToViewCenter = distanceFromViewportUp + m_Viewport.rect.height / 2f;
                }
            }
            return distanceToViewCenter;
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

            //ҳ��/��
            float pageSize = m_MovementAxis == MovementAxis.Horizontal ? m_CellRect.width + spacing.x : m_CellRect.height + spacing.y;

            //��ҳ����
            int rawTurnDirection = m_MovementAxis == MovementAxis.Horizontal ? (m_StartCorner == StartCorner.LeftOrUpper ? -1 : 1) : (m_StartCorner == StartCorner.LeftOrUpper ? 1 : -1);

            //�ܷ�ת����Ӱ���ķ�ҳ����
            int turnDirection = rawTurnDirection * (m_Reverse ? -1 : 1);

            // ����ƻ��ƶ����� �� �ٶȱ���
            float planMoveDistance;
            float speedRate = 1f;
            if (m_Loop)
            {
                // ����ѭ��ʱ
                // ������󷭵���һҳ������Ҫע�� conentλ�ûᱻ����
                // �����ζ�ţ��߼������ǡ��ƶ���1ҳ���Ŀ��λ�á������ǡ�λ��������Ϊ1ҳ����
                // ע��������������ţ������˷�ҳ����
                planMoveDistance = pageSize * turnDirection;
            }
            else
            {
                // δ����ѭ��ʱ
                // ����ǰ�������һҳ����Ѹ�ٷ��ص���һҳ
                // ����ǰ���ڷ����һҳ���򷭵���һҳ
                bool isTheLastPage = !m_Reverse ? m_CurPage == m_CellCount - 1 : m_CurPage == 0;    //�Ƿ��Ѵ��ڷ�ҳ��������һҳ
                if (isTheLastPage)
                {
                    planMoveDistance = pageSize * (m_CellCount - 1) * -turnDirection;
                    speedRate = m_CellCount - 1;
                }
                else 
                {
                    planMoveDistance = pageSize * turnDirection;
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

            //������������
            Vector2 axisDirection = m_MovementAxis == MovementAxis.Horizontal ? Vector2.right : Vector2.up;

            // ƽ������λ��
            while (Mathf.Abs(m_MovedDistance) < Mathf.Abs(planMoveDistance))
            {
                float addDistance = velocity * Time.deltaTime;  //��Ҫ����ʱ�����ţ����� Time.unscaledDeltaTime;

                // ����Ƿ�ᳬ��Ŀ�����
                if (Mathf.Abs(m_MovedDistance + addDistance) >= Mathf.Abs(planMoveDistance))
                {
                    // ֱ�����õ���ȷλ�ã���break
                    float remainingDistance = planMoveDistance - m_MovedDistance;
                    m_Content.anchoredPosition += (axisDirection * remainingDistance);
                    m_MovedDistance = planMoveDistance;
                    break;
                }
                else
                {
                    m_MovedDistance += addDistance;
                    m_Content.anchoredPosition += (axisDirection * addDistance);
                }

                yield return null;
            }
        }

        private IEnumerator WaitUntil(Func<bool> condition)
        {
            while (!condition()) yield return null;
        }
    }
}