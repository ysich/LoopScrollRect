/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2024-03-12 11:21:23
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MovementType = UnityEngine.UI.ScrollRect.MovementType;

namespace fScrollRect.Core
{
    public class fScroller : UIBehaviour, IBeginDragHandler, IDragHandler,
        IEndDragHandler, IScrollHandler,IPointerDownHandler,IPointerUpHandler
    {
        private RectTransform m_ViewRect;
        
        protected RectTransform viewRect
        {
            get
            {
                if (m_ViewRect == null)
                    m_ViewRect = (RectTransform)transform;
                return m_ViewRect;
            }
        }

        public float viewRtSize => scrollDirection == ScrollDirection.Horizontal
            ? viewRect.rect.size.x
            : viewRect.rect.size.y;
        
        public ScrollDirection scrollDirection;
        
        [SerializeField] 
        private MovementType m_MovementType = MovementType.Elastic;

        /// <summary>
        /// 当内容移动到滚动框之外时使用的行为。
        /// </summary>
        public MovementType movementType
        {
            get { return m_MovementType; }
            set { m_MovementType = value; }
        }
        
        [SerializeField] 
        private float m_Elasticity = 0.1f;
        
        /// <summary>
        /// 当内容移动到滚动框之外时要使用的弹性大小。
        /// </summary>
        public float elasticity
        {
            get => m_Elasticity;
            set => m_Elasticity = value;
        }
        
        [SerializeField]
        private float m_ScrollSensitivity = 1.0f;

        /// <summary>
        /// 灵敏度，值越大灵敏度越高
        /// </summary>
        public float scrollSensitivity { get { return m_ScrollSensitivity; } set { m_ScrollSensitivity = value; } }

        [SerializeField]
        private bool m_Inertia = true;
        
        public bool inertia { get { return m_Inertia; } set { m_Inertia = value; } }
        
        [SerializeField]
        private float m_DecelerationRate = 0.135f; // 仅在启用惯性时使用

        /// <summary>
        /// 运动减慢的速度
        /// </summary>
        public float decelerationRate { get { return m_DecelerationRate; } set { m_DecelerationRate = value; } }
        
        [Tooltip("吸附功能")] [SerializeField] Snap snap = new Snap
        {
            Enable = true,
            VelocityThreshold = 0.5f,
            Duration = 0.3f,
            Easing = EaseType.InOutCubic
        };
        
        public bool snapEnabled
        {
            get => snap.Enable;
            set => snap.Enable = value;
        }
        
        [SerializeField]
        private Scrollbar m_Scrollbar = default;

        /// <summary>
        /// 滚动条
        /// </summary>
        public Scrollbar scrollbar => m_Scrollbar;

        /// <summary>
        /// 当前滚动的位置
        /// </summary>
        public float Position
        {
            get => m_CurrentPosition;
            set
            {
                // autoScrollState.Reset();
                m_Velocity = 0f;
                m_Dragging = false;

                UpdatePosition(value);
            }
        }

        private Vector2 m_BeginDragPointerPosition = Vector2.zero;
        private float m_ScrollStartPosition = 0f;
        private float m_CurrentPosition;

        public Action<float> onValueChanged;

        public Action<int> onSelectionChanged;

        private bool m_Dragging;
        private bool m_Scrolling;
        private float m_Velocity;

        private int m_TotalCount = 0;

        public int totalCount
        {
            get { return m_TotalCount; }
            set { m_TotalCount = value; }
        }
        
        #region Drag

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewRect,
                eventData.position,
                eventData.pressEventCamera,
                out m_BeginDragPointerPosition);
            
            m_ScrollStartPosition = m_CurrentPosition;
            m_Dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(!m_Dragging)
                return;
            
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;
            
            //坐标转换
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out Vector2 dragPointerPosition))
                return;

            Vector2 pointerDelta = dragPointerPosition - m_BeginDragPointerPosition;
            
            float position = (scrollDirection == ScrollDirection.Horizontal ? -pointerDelta.x : pointerDelta.y)
                           / viewRtSize
                           * scrollSensitivity
                           + m_ScrollStartPosition;

            float offset = CalculateOffset(position);
            position += offset;
            
            if (movementType == MovementType.Elastic)
            {
                if (offset != 0f)
                {
                    position -= RubberDelta(offset, scrollSensitivity);
                }
            }
            
            UpdatePosition(position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            m_Dragging = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if(!IsActive())
                return;;
            
            Vector2 delta = eventData.scrollDelta;
            //对于滚动事件，Down是正的，而在UI系统中，up是正的。
            delta.y *= -1;
            
            var scrollDelta = scrollDirection == ScrollDirection.Horizontal
                ? Mathf.Abs(delta.y) > Mathf.Abs(delta.x)
                    ? delta.y
                    : delta.x
                : Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                    ? delta.x
                    : delta.y;

            if (eventData.IsScrolling())
            {
                m_Scrolling = true;
            }
            
            var position = m_CurrentPosition + scrollDelta / viewRtSize * scrollSensitivity;
            if (movementType == MovementType.Clamped)
            {
                position += CalculateOffset(position);
            }
            
            UpdatePosition(position);
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            
            //状态重置
            m_Velocity = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
        }

        #endregion
        
        #region private
        
        protected override void OnEnable()
        {
            base.OnEnable();
            if(m_Scrollbar)
                m_Scrollbar.onValueChanged.AddListener(ScrollbarOnValueChanged);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if(m_Scrollbar)
                m_Scrollbar.onValueChanged.RemoveListener(ScrollbarOnValueChanged);
        }

        private void ScrollbarOnValueChanged(float pos)
        {
            UpdatePosition(pos *(totalCount-1f));
        }

        private void LateUpdate()
        {
            
        }

        private float CalculateOffset(float position)
        {
            //自由的则没有偏移值
            if (movementType == MovementType.Unrestricted)
            {
                return 0f;
            }
            //小于零时返回相反数，让外部结果为0
            if (position < 0f)
            {
                return -position;
            }
            //超过最后一个index位置时变成最后一个
            if (position > totalCount - 1)
            {
                return totalCount - 1 - position;
            }

            return 0f;
        }
        
        private float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - 1 / (Mathf.Abs(overStretching) * 0.55f / viewSize + 1)) * viewSize * Mathf.Sign(overStretching);
        }
        
        private float CircularPosition(float p, int size) => size < 1 ? 0 : p < 0 ? size - 1 + (p + 1) % size : p % size;

        #endregion

        #region public

        public void UpdatePosition(float pos,bool updateScrollbar = true)
        {
            m_CurrentPosition = pos;
            
            onValueChanged?.Invoke(pos);
            
            if (scrollbar && updateScrollbar)
            {
                scrollbar.value = Mathf.Clamp01(pos / Mathf.Max(totalCount - 1f, 1e-4f));
            }
        }

        public void UpdateSelection(int dataIndex) => onSelectionChanged?.Invoke(dataIndex);

        public void ScrollTo(float position)
        {
            position = CircularPosition(position, totalCount);
        }

        public void JumpTo(int dataIndex)
        {
            if (dataIndex < 0 || dataIndex > totalCount - 1)
            {
                Debug.LogError("fScroller:JumpTo dataIndex 超出界限");
                return;
            }
            
            UpdateSelection(dataIndex);
            Position = dataIndex;

        }

        #endregion
    }
}