using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using LoopScrollRect.Core;
using Unity.VisualScripting;
using UnityEngine.UI;
using MovementType = UnityEngine.UI.ScrollRect.MovementType;
using ScrollbarVisibilityType = UnityEngine.UI.ScrollRect.ScrollbarVisibility;

namespace UnityEngine.UI
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public abstract class LoopScrollRectBase : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler, ICanvasElement, ILayoutElement, ILayoutGroup
    {
        protected List<RectTransform> m_ShowObjs = new List<RectTransform>();
        private bool isMultiple => this is LoopScrollRectMulti;
        
        /// <summary>
        /// item范围为[0,totalCount]，item的总数。当值为-1表示有无限项。
        /// </summary>
        public int totalCount;


        //当达到阈值时，把可视范围外的回收。默认设定为扩展到至少1.5 * itemSize。GetThresholdBySize里设置
        //单行单列时按照ItemIndex存储，多行多列时按照行数或者列数存储
        //这个key暂定为 itemIndex，看看后续是否需要修改
        
        /// <summary>
        /// 单行单列时使用的阈值
        /// 单行单列时按照ItemIndex存储
        /// 当达到阈值时，把可视范围外的回收。默认设定为扩展到至少1.5 * itemSize。GetThresholdBySize里设置
        /// </summary>
        public Dictionary<int, float> m_SingleItemThresholdMap = new Dictionary<int, float>();

        /// <summary>
        /// 多行多列时使用的阈值
        /// 根据行或者列进行存储
        /// </summary>
        public Dictionary<int, float> m_MultipleItemThresholdMap = new Dictionary<int, float>();
        
        // /// <summary>
        // /// 当达到阈值时，把可视范围外的回收。默认设定为扩展到至少1.5 * itemSize。GetThresholdBySize里设置
        // /// </summary>
        // private float m_Threshold = 0;
        
        [Tooltip("反向")]
        public bool reverseDirection = false;

        /// <summary>
        /// 第一个Item的DataIndex
        /// </summary>
        protected int m_ItemDataIndexStart = -1;

        /// <summary>
        /// 最后一个Item的DataIndex
        /// </summary>
        protected int m_ItemDataIndexEnd = -1;

        protected abstract float GetSize(RectTransform item, bool includeSpacing = true);
        protected abstract float GetDimension(Vector2 vector);
        protected abstract float GetAbsDimension(Vector2 vector);
        protected abstract Vector2 GetVector(float value);
        
        protected LoopScrollRectDirectionType m_Direction = LoopScrollRectDirectionType.Horizontal;

        private bool m_ContentSpaceInit = false;
        private float m_ContentSpacing = 0;
        protected float m_ContentLeftPadding = 0;
        protected float m_ContentRightPadding = 0;
        protected float m_ContentTopPadding = 0;
        protected float m_ContentBottomPadding = 0;
        protected GridLayoutGroup m_GridLayout = null;
        protected AutoSizeGridLayoutGroup m_AutoSizeGridLayoutGroup = null;
        protected float contentSpacing
        {
            get
            {
                if (m_ContentSpaceInit)
                {
                    return m_ContentSpacing;
                }
                m_ContentSpaceInit = true;
                m_ContentSpacing = 0;
                if (m_Content != null)
                {
                    LayoutGroup layout = m_Content.GetComponent<LayoutGroup>();
                    if (layout is HorizontalOrVerticalLayoutGroup horizontalOrVerticalLayoutGroup)
                    {
                        m_ContentSpacing = horizontalOrVerticalLayoutGroup.spacing;
                        m_ContentLeftPadding = horizontalOrVerticalLayoutGroup.padding.left;
                        m_ContentRightPadding = horizontalOrVerticalLayoutGroup.padding.right;
                        m_ContentTopPadding = horizontalOrVerticalLayoutGroup.padding.top;
                        m_ContentBottomPadding = horizontalOrVerticalLayoutGroup.padding.bottom;
                    }
                    else if(layout is GridLayoutGroup gridLayoutGroup)
                    {
                        m_GridLayout = gridLayoutGroup;
                        m_AutoSizeGridLayoutGroup = gridLayoutGroup as AutoSizeGridLayoutGroup;
                        m_ContentSpacing = GetAbsDimension(m_GridLayout.spacing);
                        m_ContentLeftPadding = m_GridLayout.padding.left;
                        m_ContentRightPadding = m_GridLayout.padding.right;
                        m_ContentTopPadding = m_GridLayout.padding.top;
                        m_ContentBottomPadding = m_GridLayout.padding.bottom;
                    }
                }
                return m_ContentSpacing;
            }
        }

        private bool m_ContentConstraintCountInit = false;
        private int m_ContentConstraintCount = 0;
        
        /// <summary>
        /// GridLayoutGroup上设置的行或者列的限制数量
        /// </summary>
        protected int contentConstraintCount
        {
            get
            {
                if (m_ContentConstraintCountInit)
                {
                    return m_ContentConstraintCount;
                }
                m_ContentConstraintCountInit = true;
                m_ContentConstraintCount = 1;
                if (m_Content != null)
                {
                    GridLayoutGroup layout2 = m_Content.GetComponent<GridLayoutGroup>();
                    if (layout2 != null)
                    {
                        if (layout2.constraint == GridLayoutGroup.Constraint.Flexible)
                        {
                            Debug.LogWarning("[LoopScrollRect] Flexible not supported yet");
                        }
                        m_ContentConstraintCount = layout2.constraintCount;
                    }
                }
                return m_ContentConstraintCount;
            }
        }
        protected int startLine
        {
            get
            {
                return Mathf.CeilToInt((float)(m_ItemDataIndexStart) / contentConstraintCount);
            }
        }
        protected int currentLines
        {
            get
            {
                return Mathf.CeilToInt((float)(m_ItemDataIndexEnd - m_ItemDataIndexStart) / contentConstraintCount);
            }
        }
        protected int totalLines
        {
            get
            {
                return Mathf.CeilToInt((float)(totalCount) / contentConstraintCount);
            }
        }

        protected virtual bool UpdateItems(ref Bounds viewBounds, ref Bounds contentBounds) { return false; }

        ///<summary>
        ///ScrollRect使用的事件类型。
        ///</summary>
        [Serializable]
        public class ScrollRectEvent : UnityEvent<Vector2> {}

        [SerializeField]
        protected RectTransform m_Content;	
        
        public RectTransform content { get { return m_Content; } set { m_Content = value; } }

        [SerializeField]
        private bool m_Horizontal = true;

        /// <summary>
        /// 应该启用水平滚动吗?
        /// </summary>
        public bool IsHorizontal { get { return m_Horizontal; } set { m_Horizontal = value; } }

        [SerializeField]
        private bool m_Vertical = true;

        /// <summary>
        /// 应该启用垂直滚动吗??
        /// </summary>
        public bool IsVertical { get { return m_Vertical; } set { m_Vertical = value; } }

        [SerializeField]
        private MovementType m_MovementType = MovementType.Elastic;

        /// <summary>
        /// 当内容移动到滚动框之外时使用的行为。
        /// </summary>
        public MovementType movementType { get { return m_MovementType; } set { m_MovementType = value; } }

        [SerializeField]
        private float m_Elasticity = 0.1f;

        // /// <summary>
        // /// 当内容移动到滚动框之外时要使用的弹性大小。
        // /// </summary>
        // public float elasticity { get { return m_Elasticity; } set { m_Elasticity = value; } }
        [SerializeField]
        private bool m_Inertia = true;
        public bool Inertia { get { return m_Inertia; } set { m_Inertia = value; } }
        
        // [Tooltip("吸附功能")]
        // //TODO:ysc,待实现
        // [SerializeField] Snap snap = new Snap {
        //     Enable = true,
        //     VelocityThreshold = 0.5f,
        //     Duration = 0.3f,
        //     Easing = EaseType.InOutCubic
        // };

        [SerializeField]
        private float m_DecelerationRate = 0.135f; // 仅在启用惯性时使用

        /// <summary>
        /// 运动减慢的速度
        /// </summary>
        public float decelerationRate { get { return m_DecelerationRate; } set { m_DecelerationRate = value; } }

        [SerializeField]
        private float m_ScrollSensitivity = 1.0f;

        /// <summary>
        /// 灵敏度，值越大灵敏度越高
        /// </summary>
        public float scrollSensitivity { get { return m_ScrollSensitivity; } set { m_ScrollSensitivity = value; } }

        [SerializeField]
        private RectTransform m_Viewport;
        
        public RectTransform viewport { get { return m_Viewport; } set { m_Viewport = value; SetDirtyCaching(); } }
        
        [SerializeField]
        private Scrollbar m_HorizontalScrollbar;

        /// <summary>
        /// 横向的滚动条
        /// </summary>
        public Scrollbar horizontalScrollbar
        {
            get
            {
                return m_HorizontalScrollbar;
            }
            set
            {
                if (m_HorizontalScrollbar)
                    m_HorizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
                m_HorizontalScrollbar = value;
                if (m_HorizontalScrollbar)
                    m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
                SetDirtyCaching();
            }
        }

        [SerializeField]
        private Scrollbar m_VerticalScrollbar;

        /// <summary>
        /// 竖向的滚动条
        /// </summary>
        public Scrollbar verticalScrollbar
        {
            get
            {
                return m_VerticalScrollbar;
            }
            set
            {
                if (m_VerticalScrollbar)
                    m_VerticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);
                m_VerticalScrollbar = value;
                if (m_VerticalScrollbar)
                    m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);
                SetDirtyCaching();
            }
        }

        [SerializeField]
        private ScrollbarVisibilityType m_HorizontalScrollbarVisibility;

        /// <summary>
        /// 横向滚动条的可见模式。
        /// </summary>
        public ScrollbarVisibilityType horizontalScrollbarVisibility { get { return m_HorizontalScrollbarVisibility; } set { m_HorizontalScrollbarVisibility = value; SetDirtyCaching(); } }

        [SerializeField]
        private ScrollbarVisibilityType m_VerticalScrollbarVisibility;

        /// <summary>
        /// 垂直滚动条的可见模式。
        /// </summary>
        public ScrollbarVisibilityType verticalScrollbarVisibility { get { return m_VerticalScrollbarVisibility; } set { m_VerticalScrollbarVisibility = value; SetDirtyCaching(); } }

        [SerializeField]
        private float m_HorizontalScrollbarSpacing;

        /// <summary>
        /// 滚动条和视口之间的间距
        /// </summary>
        public float horizontalScrollbarSpacing { get { return m_HorizontalScrollbarSpacing; } set { m_HorizontalScrollbarSpacing = value; SetDirty(); } }

        [SerializeField]
        private float m_VerticalScrollbarSpacing;

        /// <summary>
        /// 滚动条和视口之间的间距
        /// </summary>
        public float verticalScrollbarSpacing { get { return m_VerticalScrollbarSpacing; } set { m_VerticalScrollbarSpacing = value; SetDirty(); } }

        [SerializeField]
        private ScrollRectEvent m_OnValueChanged = new ScrollRectEvent();

        /// <summary>
        /// 当item位置变更时的回调
        /// </summary>
        /// <remarks>
        /// onvaluechange用于监视ScrollRect对象的变化。
        /// onvaluechange调用将使用UnityEvent。要监视的AddListener API
        ///修改。当发生更改时，将调用用户提供的脚本代码。
        /// UnityEvent。用于UI.ScrollRect的AddListener API。_onvaluechange接受一个Vector2。
        ///注意:编辑器允许手动设置onValueChanged值。例如
        ///值可以设置为只运行一个运行时。要调用的对象和脚本函数也是
        ///在这里提供。
        /// onValueChanged变量可以在运行时设置。下面的脚本示例
        ///显示如何执行此操作。该脚本附加到ScrollRect对象。
        /// </remarks>
        public ScrollRectEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

        // The offset from handle position to mouse down position
        private Vector2 m_PointerStartLocalCursor = Vector2.zero;
        protected Vector2 m_ContentStartPosition = Vector2.zero;

        private RectTransform m_ViewRect;

        protected RectTransform viewRect
        {
            get
            {
                if (m_ViewRect == null)
                    m_ViewRect = m_Viewport;
                if (m_ViewRect == null)
                    m_ViewRect = (RectTransform)transform;
                return m_ViewRect;
            }
        }
        
        //New

        protected float viewRectSize => IsHorizontal ? viewRect.rect.size.x : viewRect.rect.size.y;
        
        //EndNew

        protected Bounds m_ContentBounds;
        private Bounds m_ViewBounds;

        private Vector2 m_Velocity;

        /// <summary>
        /// The current velocity of the content.
        /// </summary>
        /// <remarks>
        /// The velocity is defined in units per second.
        /// </remarks>
        public Vector2 velocity { get { return m_Velocity; } set { m_Velocity = value; } }

        private bool m_Dragging;
        private bool m_Scrolling;

        private Vector2 m_PrevPosition = Vector2.zero;
        private Bounds m_PrevContentBounds;
        private Bounds m_PrevViewBounds;
        [NonSerialized]
        private bool m_HasRebuiltLayout = false;

        private bool m_HSliderExpand;
        private bool m_VSliderExpand;
        private float m_HSliderHeight;
        private float m_VSliderWidth;

        [System.NonSerialized] private RectTransform m_Rect;
        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        private RectTransform m_HorizontalScrollbarRect;
        private RectTransform m_VerticalScrollbarRect;

        private DrivenRectTransformTracker m_Tracker;

        protected Func<int,GameObject> m_OnCreateItemHandler;

        /// <summary>
        /// 设置创建Item的函数
        /// </summary>
        /// <param name="acticon">itemDataIndex,return GameObject</param>
        public void SetOnCreateItemHandler(Func<int,GameObject> acticon)
        {
            m_OnCreateItemHandler = acticon;
        }
        
        protected Action<int, int> m_OnFlushItem;
        
        /// <summary>
        /// 设置Item刷新的回调函数
        /// </summary>
        /// <param name="action">objIndex,itemDataIndex</param>
        public void SetOnFlushItemHandler(Action<int,int> action)
        {
            m_OnFlushItem = action;
        }
        
        #region 展开逻辑
        
        public int m_lastRefreshItemDataIndex = -1;
        /// <summary>
        /// 需要刷新的ItemDataIndex，当刷新到这个Index时会触发rebuild然后重新更新当前展示的所有item的阈值
        /// </summary>
        public int m_RefreshItemDataIndex = -1;

        private bool needRebuildLayout
        {
            get { return (m_RefreshItemDataIndex >= m_ItemDataIndexStart
                          && m_RefreshItemDataIndex <= m_ItemDataIndexEnd)
                         ||(m_lastRefreshItemDataIndex >= m_ItemDataIndexStart
                            && m_lastRefreshItemDataIndex <= m_ItemDataIndexEnd); }
        }
        
        private bool needRefreshThreshold
        {
            get
            {
                bool isRefreshLastItemDataIndex = m_lastRefreshItemDataIndex >= m_ItemDataIndexStart
                                                  && m_lastRefreshItemDataIndex <= m_ItemDataIndexEnd;
                bool isNeedRefreshThreshold = isRefreshLastItemDataIndex ||needRebuildLayout;
                if (isNeedRefreshThreshold)
                {
                    m_lastRefreshItemDataIndex = -1;
                }
                return isNeedRefreshThreshold;
            }
        }
        
        public void UpdateSelectItemIndex(int selectItemIndex)
        {
            m_lastRefreshItemDataIndex = m_RefreshItemDataIndex;
            m_RefreshItemDataIndex = selectItemIndex;
        }
        
        #endregion

        #if UNITY_EDITOR
        protected override void Awake()
        {
            base.Awake();
            if (Application.isPlaying)
            {
                float value = (reverseDirection ^ (m_Direction == LoopScrollRectDirectionType.Horizontal)) ? 0 : 1;
                if (m_Content != null)
                {
                    Debug.Assert(GetAbsDimension(m_Content.pivot) == value, this);
                    Debug.Assert(GetAbsDimension(m_Content.anchorMin) == value, this);
                    Debug.Assert(GetAbsDimension(m_Content.anchorMax) == value, this);
                }
                if (m_Direction == LoopScrollRectDirectionType.Vertical)
                    Debug.Assert(m_Vertical && !m_Horizontal, this);
                else
                    Debug.Assert(!m_Vertical && m_Horizontal, this);
            }
        }
        #endif

        public void ClearCells()
        {
            if (Application.isPlaying)
            {
                m_ItemDataIndexStart = -1;
                m_ItemDataIndexEnd = -1;
                totalCount = 0;
                for (int i = m_ShowObjs.Count - 1; i >= 0; i--)
                {
                    RectTransform childRt = m_ShowObjs[i] as RectTransform;
                    ReturnObjectToPool(childRt);
                }
            }
        }

        public int GetFirstItem(out float offset)
        {
            if (m_Direction == LoopScrollRectDirectionType.Vertical)
                offset = m_ViewBounds.max.y - m_ContentBounds.max.y;
            else
                offset = m_ContentBounds.min.x - m_ViewBounds.min.x;
            int idx = 0;
            if (m_ItemDataIndexEnd > m_ItemDataIndexStart)
            {
                float size = GetSize(m_ShowObjs[0], false);
                while (size + offset <= 0 && m_ItemDataIndexStart + idx + contentConstraintCount < m_ItemDataIndexEnd)
                {
                    offset += size;
                    idx += contentConstraintCount;
                    size = GetSize(m_ShowObjs[idx]);
                }
            }
            return idx + m_ItemDataIndexStart;
        }
        
        public int GetLastItem(out float offset)
        {
            if (m_Direction == LoopScrollRectDirectionType.Vertical)
                offset = m_ContentBounds.min.y - m_ViewBounds.min.y;
            else
                offset = m_ViewBounds.max.x - m_ContentBounds.max.x;
            int idx = 0;
            if (m_ItemDataIndexEnd > m_ItemDataIndexStart)
            {
                int totalChildCount = m_ShowObjs.Count;
                float size = GetSize(m_ShowObjs[totalChildCount - idx - 1], false);
                while (size + offset <= 0 && m_ItemDataIndexStart < m_ItemDataIndexEnd - idx - contentConstraintCount)
                {
                    offset += size;
                    idx += contentConstraintCount;
                    size = GetSize(m_ShowObjs[totalChildCount - idx - 1]);
                }
            }
            offset = -offset;
            return m_ItemDataIndexEnd - idx - 1;
        }

        // public void ScrollToPos(float position,float duration, EaseType easeType, Action onComplete = null)
        // {
        //     // Easing.Get(easing)
        // }
        
        public void ScrollToCell(int index, float speed)
        {
            if (totalCount >= 0 && (index < 0 || index >= totalCount))
            {
                Debug.LogErrorFormat("invalid index {0}", index);
                return;
            }
            StopAllCoroutines();
            if (speed <= 0)
            {
                RefillCells(totalCount,index);
                return;
            }
            StartCoroutine(ScrollToCellCoroutine(index, speed));
        }
        
        public void ScrollToCellWithinTime(int index, float time)
        {
            if (totalCount >= 0 && (index < 0 || index >= totalCount))
            {
                Debug.LogErrorFormat("invalid index {0}", index);
                return;
            }
            StopAllCoroutines();
            if (time <= 0)
            {
                RefillCells(totalCount,index);
                return;
            }
            float dist = 0;
            float offset = 0;
            int currentFirst = reverseDirection ? GetLastItem(out offset) : GetFirstItem(out offset);

            int TargetLine = (index / contentConstraintCount);
            int CurrentLine = (currentFirst / contentConstraintCount);

            if (TargetLine == CurrentLine)
            {
                dist = offset;
            }
            else
            {
                float elementSize = (GetAbsDimension(m_ContentBounds.size) - contentSpacing * (currentLines - 1)) / currentLines;
                dist = elementSize * (CurrentLine - TargetLine) + contentSpacing * (CurrentLine - TargetLine - 1);
                dist -= offset;
            }
            StartCoroutine(ScrollToCellCoroutine(index, Mathf.Abs(dist) / time));
        }

        IEnumerator ScrollToCellCoroutine(int index, float speed)
        {
            bool needMoving = true;
            while (needMoving)
            {
                yield return null;
                if (!m_Dragging)
                {
                    float move = 0;
                    if (index < m_ItemDataIndexStart)
                    {
                        move = -Time.deltaTime * speed;
                    }
                    else if (index >= m_ItemDataIndexEnd)
                    {
                        move = Time.deltaTime * speed;
                    }
                    else
                    {
                        m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
                        var m_ItemBounds = GetBounds4Item(index);
                        var offset = 0.0f;
                        if (m_Direction == LoopScrollRectDirectionType.Vertical)
                            offset = reverseDirection ? (m_ViewBounds.min.y - m_ItemBounds.min.y) : (m_ViewBounds.max.y - m_ItemBounds.max.y);
                        else
                            offset = reverseDirection ? (m_ItemBounds.max.x - m_ViewBounds.max.x) : (m_ItemBounds.min.x - m_ViewBounds.min.x);
                        // check if we cannot move on
                        if (totalCount >= 0)
                        {
                            if (offset > 0 && m_ItemDataIndexEnd == totalCount && !reverseDirection)
                            {
                                m_ItemBounds = GetBounds4Item(totalCount - 1);
                                // reach bottom
                                if ((m_Direction == LoopScrollRectDirectionType.Vertical && m_ItemBounds.min.y > m_ViewBounds.min.y) ||
                                    (m_Direction == LoopScrollRectDirectionType.Horizontal && m_ItemBounds.max.x < m_ViewBounds.max.x))
                                {
                                    needMoving = false;
                                    break;
                                }
                            }
                            else if (offset < 0 && m_ItemDataIndexStart == 0 && reverseDirection)
                            {
                                m_ItemBounds = GetBounds4Item(0);
                                if ((m_Direction == LoopScrollRectDirectionType.Vertical && m_ItemBounds.max.y < m_ViewBounds.max.y) ||
                                    (m_Direction == LoopScrollRectDirectionType.Horizontal && m_ItemBounds.min.x > m_ViewBounds.min.x))
                                {
                                    needMoving = false;
                                    break;
                                }
                            }
                        }

                        float maxMove = Time.deltaTime * speed;
                        if (Mathf.Abs(offset) < maxMove)
                        {
                            needMoving = false;
                            move = offset;
                        }
                        else
                            move = Mathf.Sign(offset) * maxMove;
                    }
                    if (move != 0)
                    {
                        Vector2 offset = GetVector(move);
                        m_Content.anchoredPosition += offset;
                        m_PrevPosition += offset;
                        m_ContentStartPosition += offset;
                        UpdateBounds(true);
                    }
                }
            }
            StopMovement();
            UpdatePrevData();
        }

        protected abstract void ProvideData(RectTransform transform, int index);

        /// <summary>
        /// 刷新列表数据，原地刷新
        /// </summary>
        public void RefreshCells(int newTotalCount = -1)
        {
            if (!Application.isPlaying || !this.isActiveAndEnabled)
            {
                return;
            }
            StopMovement();
            
            if (newTotalCount == -1)
                newTotalCount = totalCount;
            else
                totalCount = newTotalCount;
            
            //先确定itemTypeStart，还要注意保存偏移，或者说content不会复位
            int lastIndex = newTotalCount - 1;
            if (m_ItemDataIndexStart > lastIndex)
            {
                m_ItemDataIndexStart = lastIndex;
            }
            m_ItemDataIndexEnd = m_ItemDataIndexStart;
            //检查是否有可回收的item
            for (int i = 0; i < m_ShowObjs.Count; i++)
            {
                if (m_ItemDataIndexEnd < totalCount)
                {
                    RectTransform itemRt = m_ShowObjs[i];
                    ProvideData(itemRt, m_ItemDataIndexEnd);
                    m_ItemDataIndexEnd++;
                }
                else
                {
                    RectTransform childRt = m_ShowObjs[i];
                    ReturnObjectToPool(childRt);
                    i--;
                }
            }
            //如果是item展开则会影响到列表的尺寸和回收
            if (needRebuildLayout)
            {
                //这一步可能会导致item尺寸发生变更，需要更新Threshold
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
                int itemIndex = m_ItemDataIndexStart;
                bool isMultiple = contentConstraintCount > 1;
                float maxThreshold = 0;
                int lastMultipleIndex = 0;
                for (int i = 0; i < m_ShowObjs.Count; i++)
                {
                    //获取每个item最新的尺寸阈值
                    RectTransform itemRt = m_ShowObjs[i];
                    float size = GetSize(itemRt);
                    float threshold = GetThresholdBySize(size);
                    if (isMultiple)
                    {
                        //取本行或者本列的最高值
                        int multipleIndex = GetMultipleIndexByItemIndex(itemIndex);
                        if (lastMultipleIndex == multipleIndex)
                        {
                            //如果和上一个行/列数相等则进行比较，比较后保存大的。
                            maxThreshold = Math.Max(maxThreshold, threshold);
                            AddThresholdByItemIndex(itemIndex,maxThreshold);
                        }
                        else
                        {
                            maxThreshold = threshold;
                        }

                        lastMultipleIndex = multipleIndex;
                    }
                    else
                    {
                        //单行单列直接覆盖
                        AddThresholdByItemIndex(itemIndex,threshold);
                    }
                    itemIndex++;
                }
            }
            EnsureLayoutHasRebuilt(true);
            UpdateBounds(true);
            UpdateScrollbars(Vector2.zero);
        }

        /// <summary>
        /// 刷新列表，并且展示最后一个item。适合聊天框类似的场景。
        /// </summary>
        /// <param name="newTotalCount"></param>
        /// <param name="endItem">展示倒数第几个item</param>
        /// <param name="alignStart"></param>
        public void RefillCellsFromEnd(int newTotalCount,int endItem = 0, bool alignStart = false)
        {
            if (!Application.isPlaying)
                return;
            totalCount = newTotalCount;
            m_ItemDataIndexEnd = reverseDirection ? endItem : totalCount - endItem;
            m_ItemDataIndexStart = m_ItemDataIndexEnd;

            if (totalCount >= 0 && m_ItemDataIndexStart % contentConstraintCount != 0)
            {
                m_ItemDataIndexStart = (m_ItemDataIndexStart / contentConstraintCount) * contentConstraintCount;
            }

            ReturnToTempPool(!reverseDirection, m_ShowObjs.Count);

            float sizeToFill = GetAbsDimension(viewRect.rect.size), sizeFilled = 0;

            bool first = true;
            while (sizeToFill > sizeFilled)
            {
                float size = reverseDirection ? NewItemAtEnd(!first) : NewItemAtStart(!first);
                if (size < 0)
                    break;
                first = false;
                sizeFilled += size;
            }

            //如果还没满就反方向填充
            while (sizeToFill > sizeFilled)
            {
                float size = reverseDirection ? NewItemAtStart(!first) : NewItemAtEnd(!first);
                if (size < 0)
                    break;
                first = false;
                sizeFilled += size;
            }

            Vector2 pos = m_Content.anchoredPosition;
            float dist = alignStart ? 0 : Mathf.Max(0, sizeFilled - sizeToFill);
            if (reverseDirection)
                dist = -dist;
            if (m_Direction == LoopScrollRectDirectionType.Vertical)
                pos.y = dist;
            else
                pos.x = -dist;
            m_Content.anchoredPosition = pos;
            m_ContentStartPosition = pos;

            ClearTempPool();
            // force build bounds here so scrollbar can access newest bounds
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
            Canvas.ForceUpdateCanvases();
            UpdateBounds(false);
            UpdateScrollbars(Vector2.zero);
            StopMovement();
            UpdatePrevData();
        }
        
        public void RefillCells(int newTotalCount,int startItemIndex = -1)
        {
            if (!Application.isPlaying)
                return;
            bool isKeepPos = startItemIndex == -1;
            if (isKeepPos)
            {
                startItemIndex = m_ItemDataIndexStart;
            }
            totalCount = newTotalCount;
            if (totalCount > 0)
            {
                startItemIndex = Mathf.Clamp(startItemIndex, 0, newTotalCount - 1);
            }
            else
            {
                startItemIndex = 0;
            }

            m_ItemDataIndexStart = reverseDirection ? totalCount - startItemIndex : startItemIndex;
            if (totalCount >= 0 && m_ItemDataIndexStart % contentConstraintCount != 0)
            {
                m_ItemDataIndexStart = (m_ItemDataIndexStart / contentConstraintCount) * contentConstraintCount;
            }
            m_ItemDataIndexEnd = m_ItemDataIndexStart;
            m_SingleItemThresholdMap.Clear();
            ReturnToTempPool(reverseDirection, m_ShowObjs.Count);
            float sizeToFill = GetAbsDimension(viewRect.rect.size);
            float sizeFilled = 0;
            //当RefillCells开始时，m_ViewBounds可能还没准备好
            //从前面开始填充
            bool first = true;
            while (sizeToFill > sizeFilled)
            {
                float size = reverseDirection ? NewItemAtStart(!first) : NewItemAtEnd(!first);
                if (size < 0)
                    break;
                first = false;
                sizeFilled += size;
            }
            
            // 如果还没满就重新填充
            //从后面开始填充
            while (sizeToFill > sizeFilled)
            {
                float size = reverseDirection ? NewItemAtEnd(!first) : NewItemAtStart(!first);
                if (size < 0)
                    break;
                first = false;
                sizeFilled += size;
            }

            Vector2 pos = m_Content.anchoredPosition;
            m_Content.anchoredPosition = pos;
            m_ContentStartPosition = pos;

            ClearTempPool();
            // 在这里强制构建边界，这样滚动条就可以访问最新的边界
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
            Canvas.ForceUpdateCanvases();
            UpdateBounds();
            UpdateScrollbars(Vector2.zero);
            StopMovement();
            UpdatePrevData();
            UpdateItemAnimator();
        }

        private float GetThresholdBySize(float size)
        {
            return size * 1.2f;
        }

        private int GetMultipleIndexByItemIndex(int itemIndex)
        {
            //传递的时候索引是从0开始的，这里用从1开始方便计算。
            itemIndex++;
            int index = itemIndex / contentConstraintCount + (itemIndex % contentConstraintCount > 0 ? 1 : 0);
            return index;
        }

        private void AddThresholdByItemIndex(int itemIndex,float itemThreshold)
        {
            bool isMultiple = contentConstraintCount > 1;
            if (isMultiple)
            {
                //行数 或者列数
                int multipleIndex = GetMultipleIndexByItemIndex(itemIndex);
                m_MultipleItemThresholdMap[multipleIndex] = itemThreshold;
            }
            else
            {
                m_SingleItemThresholdMap[itemIndex] = itemThreshold;
            }
        }

        private void RemoveThresholdByItemIndex(int itemIndex)
        {
            bool isMultiple = contentConstraintCount > 1;
            if (isMultiple)
            {
                //行数 或者列数
                int multipleIndex = GetMultipleIndexByItemIndex(itemIndex);
                m_MultipleItemThresholdMap.Remove(multipleIndex);
            }
            else
            {
                m_SingleItemThresholdMap.Remove(itemIndex);
            }
        }

        private float GetThresholdByItemIndex(int itemIndex)
        {
            if (contentConstraintCount > 1)
            {
                //行数 或者列数
                int multipleIndex = GetMultipleIndexByItemIndex(itemIndex);
                if (m_MultipleItemThresholdMap.TryGetValue(multipleIndex,out float threshold))
                {
                    return threshold;
                }

                return 0;
            }

            return m_SingleItemThresholdMap[itemIndex];
        }
        
        protected float itemStartIndexThreshold
        {
            get { return GetThresholdByItemIndex(m_ItemDataIndexStart); }
        }

        protected float itemEndIndexThreshold
        {
            //这里-1是因为-1才是最后一个。itemDataIndexEnd会比实际多一个
            get { return GetThresholdByItemIndex(m_ItemDataIndexEnd - 1); }
        }

        protected float NewItemAtStart(bool includeSpacing = true)
        {
            if (totalCount >= 0 && m_ItemDataIndexStart - contentConstraintCount < 0)
            {
                return -1;
            }
            float size = 0;
            if (contentConstraintCount > 1)
            {
                List<RectTransform> tempRects = new List<RectTransform>();
                for (int i = 0; i < contentConstraintCount; i++)
                {
                    m_ItemDataIndexStart--;
                    RectTransform newItem = GetFromTempPool(m_ItemDataIndexStart);
                    if (needRebuildLayout)
                    {
                        tempRects.Add(newItem);
                    }
                    size = Mathf.Max(GetSize(newItem, includeSpacing), size);
                }

                if (needRebuildLayout)
                {
                    //这里需要rebuild说明之前的size不准确需要重置
                    size = 0;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
                    for (int i = 0; i < tempRects.Count; i++)
                    {
                        RectTransform rect = tempRects[i];
                        size = Mathf.Max(GetSize(rect, includeSpacing), size);
                    }
                }

                float itemThreshold = GetThresholdBySize(size);
                AddThresholdByItemIndex(m_ItemDataIndexStart,itemThreshold);
            }
            else
            {
                m_ItemDataIndexStart--;
                RectTransform newItem = GetFromTempPool(m_ItemDataIndexStart);
                //item可能会改变大小。
                if (needRebuildLayout)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
                }
                //rebuildLayout或者未存储阈值时更新阈值
                if (needRefreshThreshold || !m_SingleItemThresholdMap.ContainsKey(m_ItemDataIndexStart))
                {
                    size = Mathf.Max(GetSize(newItem, includeSpacing), size);
                    float itemThreshold = GetThresholdBySize(size);
                    AddThresholdByItemIndex(m_ItemDataIndexStart, itemThreshold);
                }
            }

            if (size > 0)
            {
                m_HasRebuiltLayout = false;
                if (!reverseDirection)
                {
                    Vector2 offset = GetVector(size);
                    m_Content.anchoredPosition += offset;
                    m_PrevPosition += offset;
                    m_ContentStartPosition += offset;
                }
            }

            return size;
        }

        protected float DeleteItemAtStart()
        {
            // special case: when moving or dragging, we cannot simply delete start when we've reached the end
            if ((m_Dragging || m_Velocity != Vector2.zero) && totalCount >= 0 && m_ItemDataIndexEnd >= totalCount - contentConstraintCount)
            {
                return 0;
            }
            int availableChilds = m_ShowObjs.Count - deletedItemTypeStart - deletedItemTypeEnd;
            Debug.Assert(availableChilds >= 0);
            if (availableChilds == 0)
            {
                return 0;
            }

            float size = 0;
            for (int i = 0; i < contentConstraintCount; i++)
            {
                RectTransform oldItem = m_ShowObjs[deletedItemTypeStart] as RectTransform;
                size = Mathf.Max(GetSize(oldItem), size);
                ReturnToTempPool(true);
                RemoveThresholdByItemIndex(m_ItemDataIndexStart);
                availableChilds--;
                m_ItemDataIndexStart++;

                if (availableChilds == 0)
                {
                    break;
                }
            }

            if (size > 0)
            {
                m_HasRebuiltLayout = false;
                if (!reverseDirection)
                {
                    Vector2 offset = GetVector(size);
                    m_Content.anchoredPosition -= offset;
                    m_PrevPosition -= offset;
                    m_ContentStartPosition -= offset;
                }
            }

            return size;
        }

        protected float NewItemAtEnd(bool includeSpacing = true)
        {
            if (totalCount >= 0 && m_ItemDataIndexEnd >= totalCount)
            {
                return -1;
            }
            float size = 0;
            //先填充行尾
            int availableChilds = m_ShowObjs.Count - deletedItemTypeStart - deletedItemTypeEnd;
            int count = contentConstraintCount - (availableChilds % contentConstraintCount);

            if (count > 1)
            {
                List<RectTransform> tempRects = new List<RectTransform>();
                for (int i = 0; i < count; i++)
                {
                    RectTransform newItem = GetFromTempPool(m_ItemDataIndexEnd);
                    if (needRebuildLayout)
                    {
                        tempRects.Add(newItem);
                    }
                    size = Mathf.Max(GetSize(newItem, includeSpacing), size);
                    m_ItemDataIndexEnd++;
                    if (totalCount >= 0 && m_ItemDataIndexEnd >= totalCount)
                    {
                        break;
                    }
                }

                if (needRebuildLayout)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
                    for (int i = 0; i < tempRects.Count; i++)
                    {
                        RectTransform rect = tempRects[i];
                        size = Mathf.Max(GetSize(rect, includeSpacing), size);
                    }
                }

                float itemThreshold = GetThresholdBySize(size);
                //这里m_ItemDataIndexEnd进行-1操作，因为IndexEnd会比最后一个多一个
                AddThresholdByItemIndex(m_ItemDataIndexEnd - 1,itemThreshold);
            }
            else
            {
                RectTransform newItem = GetFromTempPool(m_ItemDataIndexEnd);
                //item可能会改变大小。
                if (needRebuildLayout)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
                }
                //rebuildLayout或者未存储阈值时更新阈值
                if (needRefreshThreshold || !m_SingleItemThresholdMap.ContainsKey(m_ItemDataIndexEnd))
                {
                    size = Mathf.Max(GetSize(newItem, includeSpacing), size);
                    float itemThreshold = GetThresholdBySize(size);
                    AddThresholdByItemIndex(m_ItemDataIndexEnd, itemThreshold);
                }
                m_ItemDataIndexEnd++;
            }

            if (size > 0)
            {
                m_HasRebuiltLayout = false;
                if (reverseDirection)
                {
                    Vector2 offset = GetVector(size);
                    m_Content.anchoredPosition -= offset;
                    m_PrevPosition -= offset;
                    m_ContentStartPosition -= offset;
                }
            }

            return size;
        }

        protected float DeleteItemAtEnd()
        {
            if ((m_Dragging || m_Velocity != Vector2.zero) && totalCount >= 0 && m_ItemDataIndexStart < contentConstraintCount)
            {
                return 0;
            }
            int availableChilds = m_ShowObjs.Count - deletedItemTypeStart - deletedItemTypeEnd;
            Debug.Assert(availableChilds >= 0);
            if (availableChilds == 0)
            {
                return 0;
            }

            float size = 0;
            for (int i = 0; i < contentConstraintCount; i++)
            {
                RectTransform oldItem = m_ShowObjs[m_ShowObjs.Count - deletedItemTypeEnd - 1] as RectTransform;
                size = Mathf.Max(GetSize(oldItem), size);
                ReturnToTempPool(false);
                RemoveThresholdByItemIndex(m_ItemDataIndexEnd);
                availableChilds--;
                m_ItemDataIndexEnd--;
                if (m_ItemDataIndexEnd % contentConstraintCount == 0 || availableChilds == 0)
                {
                    break;  //just delete the whole row
                }
            }

            if (size > 0)
            {
                m_HasRebuiltLayout = false;
                if (reverseDirection)
                {
                    Vector2 offset = GetVector(size);
                    m_Content.anchoredPosition += offset;
                    m_PrevPosition += offset;
                    m_ContentStartPosition += offset;
                }
            }

            return size;
        }

        protected int deletedItemTypeStart = 0;
        protected int deletedItemTypeEnd = 0;
        
        /// <summary>
        /// 从列表里去取不显示的，插入到需要显示的位置
        /// </summary>
        /// <param name="itemIdx"></param>
        /// <returns></returns>
        protected abstract RectTransform GetFromTempPool(int itemIdx);
        protected abstract void ReturnToTempPool(bool fromStart, int count = 1);
        protected abstract void ClearTempPool();
        
        public virtual void Rebuild(CanvasUpdate executing)
        {
            if (executing == CanvasUpdate.Prelayout)
            {
                UpdateCachedData();
            }

            if (executing == CanvasUpdate.PostLayout)
            {
                UpdateBounds();
                UpdateScrollbars(Vector2.zero);
                UpdatePrevData();

                m_HasRebuiltLayout = true;
            }
        }

        public virtual void LayoutComplete()
        {}

        public virtual void GraphicUpdateComplete()
        {}

        void UpdateCachedData()
        {
            Transform transform = this.transform;
            m_HorizontalScrollbarRect = m_HorizontalScrollbar == null ? null : m_HorizontalScrollbar.transform as RectTransform;
            m_VerticalScrollbarRect = m_VerticalScrollbar == null ? null : m_VerticalScrollbar.transform as RectTransform;

            // These are true if either the elements are children, or they don't exist at all.
            bool viewIsChild = (viewRect.parent == transform);
            bool hScrollbarIsChild = (!m_HorizontalScrollbarRect || m_HorizontalScrollbarRect.parent == transform);
            bool vScrollbarIsChild = (!m_VerticalScrollbarRect || m_VerticalScrollbarRect.parent == transform);
            bool allAreChildren = (viewIsChild && hScrollbarIsChild && vScrollbarIsChild);

            m_HSliderExpand = allAreChildren && m_HorizontalScrollbarRect && horizontalScrollbarVisibility == ScrollbarVisibilityType.AutoHideAndExpandViewport;
            m_VSliderExpand = allAreChildren && m_VerticalScrollbarRect && verticalScrollbarVisibility == ScrollbarVisibilityType.AutoHideAndExpandViewport;
            m_HSliderHeight = (m_HorizontalScrollbarRect == null ? 0 : m_HorizontalScrollbarRect.rect.height);
            m_VSliderWidth = (m_VerticalScrollbarRect == null ? 0 : m_VerticalScrollbarRect.rect.width);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_HorizontalScrollbar)
                m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
            if (m_VerticalScrollbar)
                m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            SetDirty();
        }

        protected override void OnDisable()
        {
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            if (m_HorizontalScrollbar)
                m_HorizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
            if (m_VerticalScrollbar)
                m_VerticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);

            m_Dragging = false;
            m_Scrolling = false;
            m_HasRebuiltLayout = false;
            m_Tracker.Clear();
            m_Velocity = Vector2.zero;
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }
        
        public override bool IsActive()
        {
            return base.IsActive() && m_Content != null;
        }

        private void EnsureLayoutHasRebuilt(bool forceUpdateCanvases = false)
        {
            if ((forceUpdateCanvases||!m_HasRebuiltLayout) && !CanvasUpdateRegistry.IsRebuildingLayout())
                Canvas.ForceUpdateCanvases();
        }

        protected int GetObjSiblingIndex(int itemIndex)
        {
            return Mathf.Max(itemIndex - m_ItemDataIndexStart,0);
        }
        
        protected abstract int GetItemIndexByRt(RectTransform rectTransform);

        protected abstract GameObject GetObjectFromPool(int itemDataIndex);
        protected abstract void ReturnObjectToPool(RectTransform rectTransform);

        /// <summary>
        /// Sets the velocity to zero on both axes so the content stops moving.
        /// </summary>
        public virtual void StopMovement()
        {
            m_Velocity = Vector2.zero;
        }
        
        //滚轮和进度条用这个
        public virtual void OnScroll(PointerEventData data)
        {
            if (!IsActive())
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();
            //根据滚动距离计算出Content的位置
            Vector2 delta = data.scrollDelta;
            //对于滚动事件，Down是正的，而在UI系统中，up是正的。
            delta.y *= -1;
            if (IsVertical && !IsHorizontal)
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    delta.y = delta.x;
                delta.x = 0;
            }
            if (IsHorizontal && !IsVertical)
            {
                if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    delta.x = delta.y;
                delta.y = 0;
            }

            if (data.IsScrolling())
                m_Scrolling = true;

            Vector2 position = m_Content.anchoredPosition;
            position += delta * m_ScrollSensitivity;
            if (m_MovementType == MovementType.Clamped)
                position += CalculateOffset(position - m_Content.anchoredPosition);

            SetContentAnchoredPosition(position);
            UpdateBounds();
            UpdateItemAnimator();
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Velocity = Vector2.zero;
        }

        /// <summary>
        /// Handling for when the content is beging being dragged.
        /// </summary>
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            UpdateBounds();

            m_PointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out m_PointerStartLocalCursor);
            m_ContentStartPosition = m_Content.anchoredPosition;
            m_Dragging = true;
        }

        /// <summary>
        /// Handling for when the content has finished being dragged.
        /// </summary>
        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Dragging = false;
        }

        /// <summary>
        /// Handling for when the content is dragged.
        /// </summary>
        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!m_Dragging)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            Vector2 localCursor;
            //坐标转换
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out localCursor))
                return;

            UpdateBounds();
            //计算差值
            var pointerDelta = localCursor - m_PointerStartLocalCursor;
            Vector2 position = m_ContentStartPosition + pointerDelta;
            
            // 使内容显示在视图中的偏移量。
            Vector2 offset = CalculateOffset(position - m_Content.anchoredPosition);
            position += offset;
            if (m_MovementType == MovementType.Elastic)
            {
                if (offset.x != 0)
                    position.x = position.x - RubberDelta(offset.x, m_ViewBounds.size.x);
                if (offset.y != 0)
                    position.y = position.y - RubberDelta(offset.y, m_ViewBounds.size.y);
            }
            SetContentAnchoredPosition(position);
            UpdateItemAnimator();
        }

        /// <summary>
        /// 设置Content的anchored position
        /// </summary>
        protected virtual void SetContentAnchoredPosition(Vector2 position)
        {
            if (!m_Horizontal)
                position.x = m_Content.anchoredPosition.x;
            if (!m_Vertical)
                position.y = m_Content.anchoredPosition.y;
            
            if ((position - m_Content.anchoredPosition).sqrMagnitude > 0.001f)
            {
                m_Content.anchoredPosition = position;
                UpdateBounds(true);
            }
            // Debug.Log("SetContentAnchoredPosition~~~~~~~~");
        }

        /// <summary>
        /// 计算出进度值更新Item的状态机
        /// Item必须得有状态机，并且拥有需要更新的标记！
        /// 只支持非展开和单预制的滑动列表
        /// </summary>
        protected void UpdateItemAnimator()
        {
            // //不支持展开项列表和多预制的列表
            // if(isMultiple)
            //     return;
            // Vector2 contentPos = m_Content.anchoredPosition;
            // float axisContentPos = IsHorizontal ? contentPos.x : contentPos.y;
            // float itemSize = GetSize(m_ShowObjs[0]);
            // int showObjCount = m_ShowObjs.Count;
            // float itemPos = 0;
            // float startItemPos = itemPos = axisContentPos > itemSize + contentSpacing
            //     ? axisContentPos - itemSize - contentSpacing
            //     : axisContentPos;
            // float endItemPos = startItemPos + (itemSize + contentSpacing) * (showObjCount-1);
            // float radiusPos = endItemPos - startItemPos;
            // //终点-起点=范围
            // //进度值= (当前位置-起点)/范围
            // //计算出每个item的进度值，然后更新给item的状态机
            // for (int i = 0; i < showObjCount; i++)
            // {
            //     RectTransform itemRt = m_ShowObjs[i];
            //     float progressValue = 0;
            //     if (itemPos != startItemPos)
            //     {
            //         progressValue = (itemPos - startItemPos) / radiusPos;
            //     }
            //     //TODO:ysc,临时代码
            //     Debug.Log($"itemDataIndex={m_ItemDataIndexStart + i},progressValue = {progressValue}");
            //     Animator animator = itemRt.GetComponent<Animator>();
            //     if (animator != null)
            //     {
            //         animator.speed = 0;
            //         animator.Play("scroll",-1,progressValue);
            //     }
            //     //endTODO
            //     itemPos += (itemSize + contentSpacing);
            // }
        }

        protected virtual void LateUpdate()
        {
            if (!m_Content)
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();
            float deltaTime = Time.unscaledDeltaTime;
            Vector2 offset = CalculateOffset(Vector2.zero);
            if (!m_Dragging && (offset != Vector2.zero || m_Velocity != Vector2.zero))
            {
                Vector2 position = m_Content.anchoredPosition;
                for (int axis = 0; axis < 2; axis++)
                {
                    //如果移动是弹性的，并且内容和视图有偏移，那么使用弹簧
                    if (m_MovementType == MovementType.Elastic && offset[axis] != 0)
                    {
                        float speed = m_Velocity[axis];
                        float smoothTime = m_Elasticity;
                        if (m_Scrolling)
                            smoothTime *= 3.0f;
                        position[axis] = Mathf.SmoothDamp(m_Content.anchoredPosition[axis], m_Content.anchoredPosition[axis] + offset[axis], ref speed, smoothTime, Mathf.Infinity, deltaTime);
                        if (Mathf.Abs(speed) < 1)
                            speed = 0;
                        m_Velocity[axis] = speed;
                    }
                    //根据速度移动内容，然后减速
                    else if (m_Inertia)
                    {
                        m_Velocity[axis] *= Mathf.Pow(m_DecelerationRate, deltaTime);
                        if (Mathf.Abs(m_Velocity[axis]) < 1)
                            m_Velocity[axis] = 0;
                        position[axis] += m_Velocity[axis] * deltaTime;
                        // if (snap.Enable && Mathf.Abs(m_Velocity[axis]) < snap.VelocityThreshold)
                        // {
                        //     ScrollToPos(Mathf.RoundToInt(position[axis]), snap.Duration, snap.Easing);
                        // }
                    }
                    //散失活力
                    else
                    {
                        m_Velocity[axis] = 0;
                    }
                }

                if (m_MovementType == MovementType.Clamped)
                {
                    offset = CalculateOffset(position - m_Content.anchoredPosition);
                    position += offset;
                }

                SetContentAnchoredPosition(position);
                UpdateItemAnimator();
            }

            if (m_Dragging && m_Inertia)
            {
                Vector3 newVelocity = (m_Content.anchoredPosition - m_PrevPosition) / deltaTime;
                m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime * 10);
            }

            if (m_ViewBounds != m_PrevViewBounds || m_ContentBounds != m_PrevContentBounds || m_Content.anchoredPosition != m_PrevPosition)
            {
                UpdateScrollbars(offset);
                #if UNITY_2017_1_OR_NEWER
                UISystemProfilerApi.AddMarker("ScrollRect.value", this);
                #endif
                m_OnValueChanged.Invoke(normalizedPosition);
                UpdatePrevData();
            }
            UpdateScrollbarVisibility();
            m_Scrolling = false;
        }

        /// <summary>
        /// Helper function to update the previous data fields on a ScrollRect. Call this before you change data in the ScrollRect.
        /// </summary>
        protected void UpdatePrevData()
        {
            if (m_Content == null)
                m_PrevPosition = Vector2.zero;
            else
                m_PrevPosition = m_Content.anchoredPosition;
            m_PrevViewBounds = m_ViewBounds;
            m_PrevContentBounds = m_ContentBounds;
        }
        
        public void GetHorizonalOffsetAndSize(out float totalSize, out float offset)
        {
            float elementSize = (m_ContentBounds.size.x - contentSpacing * (currentLines - 1)) / currentLines;
            totalSize = elementSize * totalLines + contentSpacing * (totalLines - 1);
            offset = m_ContentBounds.min.x - elementSize * startLine - contentSpacing * startLine;
        }
        
        public void GetVerticalOffsetAndSize(out float totalSize, out float offset)
        {
            float elementSize = (m_ContentBounds.size.y - contentSpacing * (currentLines - 1)) / currentLines;
            totalSize = elementSize * totalLines + contentSpacing * (totalLines - 1);
            offset = m_ContentBounds.max.y + elementSize * startLine + contentSpacing * startLine;
        }

        private void UpdateScrollbars(Vector2 offset)
        {
            if (m_HorizontalScrollbar)
            {
                if (m_ContentBounds.size.x > 0 && totalCount > 0)
                {
                    float totalSize, _;
                    GetHorizonalOffsetAndSize(out totalSize, out _);
                    m_HorizontalScrollbar.size = Mathf.Clamp01((m_ViewBounds.size.x - Mathf.Abs(offset.x)) / totalSize);
                }
                else
                    m_HorizontalScrollbar.size = 1;

                m_HorizontalScrollbar.value = horizontalNormalizedPosition;
            }

            if (m_VerticalScrollbar)
            {
                if (m_ContentBounds.size.y > 0 && totalCount > 0)
                {
                    float totalSize, _;
                    GetVerticalOffsetAndSize(out totalSize, out _);
                    m_VerticalScrollbar.size = Mathf.Clamp01((m_ViewBounds.size.y - Mathf.Abs(offset.y)) / totalSize);
                }
                else
                    m_VerticalScrollbar.size = 1;

                m_VerticalScrollbar.value = verticalNormalizedPosition;
            }
        }

        /// <summary>
        /// The scroll position as a Vector2 between (0,0) and (1,1) with (0,0) being the lower left corner.
        /// </summary>
        /// <example>
        ///     Vector2 myPosition = new Vector2(0.5f, 0.5f);
        ///     myScrollRect.normalizedPosition = myPosition;
        /// </example>
        public Vector2 normalizedPosition
        {
            get
            {
                return new Vector2(horizontalNormalizedPosition, verticalNormalizedPosition);
            }
            set
            {
                SetNormalizedPosition(value.x, 0);
                SetNormalizedPosition(value.y, 1);
            }
        }

        /// <summary>
        /// The horizontal scroll position as a value between 0 and 1, with 0 being at the left.
        /// </summary>
        /// <example>
        ///     myScrollRect.horizontalNormalizedPosition = 0.5f;
        /// </example>
        public float horizontalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if (totalCount > 0 && m_ItemDataIndexEnd > m_ItemDataIndexStart)
                {
                    float totalSize, offset;
                    GetHorizonalOffsetAndSize(out totalSize, out offset);

                    if (totalSize <= m_ViewBounds.size.x)
                        return (m_ViewBounds.min.x > offset) ? 1 : 0;
                    return (m_ViewBounds.min.x - offset) / (totalSize - m_ViewBounds.size.x);
                }
                else
                    return 0.5f;
            }
            set
            {
                SetNormalizedPosition(value, 0);
            }
        }

        /// <summary>
        /// The vertical scroll position as a value between 0 and 1, with 0 being at the bottom.
        /// </summary>
        /// <example>
        ///     myScrollRect.verticalNormalizedPosition = 0.5f;
        /// </example>

        public float verticalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if (totalCount > 0 && m_ItemDataIndexEnd > m_ItemDataIndexStart)
                {
                    float totalSize, offset;
                    GetVerticalOffsetAndSize(out totalSize, out offset);

                    if (totalSize <= m_ViewBounds.size.y)
                        return (offset > m_ViewBounds.max.y) ? 1 : 0;
                    return (offset - m_ViewBounds.max.y) / (totalSize - m_ViewBounds.size.y);
                }
                else
                    return 0.5f;
            }
            set
            {
                SetNormalizedPosition(value, 1);
            }
        }

        private void SetHorizontalNormalizedPosition(float value) { SetNormalizedPosition(value, 0); }
        private void SetVerticalNormalizedPosition(float value) { SetNormalizedPosition(value, 1); }

        /// <summary>
        /// >Set the horizontal or vertical scroll position as a value between 0 and 1, with 0 being at the left or at the bottom.
        /// </summary>
        /// <param name="value">The position to set, between 0 and 1.</param>
        /// <param name="axis">The axis to set: 0 for horizontal, 1 for vertical.</param>
        protected virtual void SetNormalizedPosition(float value, int axis)
        {
            if (totalCount <= 0 || m_ItemDataIndexEnd <= m_ItemDataIndexStart)
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();
            
            float totalSize, offset;
            float newAnchoredPosition = m_Content.anchoredPosition[axis];
            if (axis == 0)
            {
                GetHorizonalOffsetAndSize(out totalSize, out offset);

                if (totalSize >= m_ViewBounds.size.x)
                {
                    newAnchoredPosition += m_ViewBounds.min.x - value * (totalSize - m_ViewBounds.size.x) - offset;
                }
            }
            else
            {
                GetVerticalOffsetAndSize(out totalSize, out offset);
                
                if (totalSize >= m_ViewBounds.size.y)
                {
                    newAnchoredPosition -= offset - value * (totalSize - m_ViewBounds.size.y) - m_ViewBounds.max.y;
                }
            }

            Vector3 anchoredPosition = m_Content.anchoredPosition;
            if (Mathf.Abs(anchoredPosition[axis] - newAnchoredPosition) > 0.01f)
            {
                anchoredPosition[axis] = newAnchoredPosition;
                m_Content.anchoredPosition = anchoredPosition;
                m_Velocity[axis] = 0;
                UpdateBounds(true);	
            }
        }

        private static float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        private bool hScrollingNeeded
        {
            get
            {
                if (Application.isPlaying)
                    return m_ContentBounds.size.x > m_ViewBounds.size.x + 0.01f;
                return true;
            }
        }
        private bool vScrollingNeeded
        {
            get
            {
                if (Application.isPlaying)
                    return m_ContentBounds.size.y > m_ViewBounds.size.y + 0.01f;
                return true;
            }
        }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void CalculateLayoutInputHorizontal() {}

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void CalculateLayoutInputVertical() {}

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float minWidth { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float preferredWidth { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float flexibleWidth { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float minHeight { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float preferredHeight { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float flexibleHeight { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual int layoutPriority { get { return -1; } }

        public virtual void SetLayoutHorizontal()
        {
            m_Tracker.Clear();

            if (m_HSliderExpand || m_VSliderExpand)
            {
                m_Tracker.Add(this, viewRect,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.SizeDelta |
                    DrivenTransformProperties.AnchoredPosition);

                // Make view full size to see if content fits.
                viewRect.anchorMin = Vector2.zero;
                viewRect.anchorMax = Vector2.one;
                viewRect.sizeDelta = Vector2.zero;
                viewRect.anchoredPosition = Vector2.zero;

                // Recalculate content layout with this size to see if it fits when there are no scrollbars.
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
                m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
                m_ContentBounds = GetBounds();
            }

            // If it doesn't fit vertically, enable vertical scrollbar and shrink view horizontally to make room for it.
            if (m_VSliderExpand && vScrollingNeeded)
            {
                viewRect.sizeDelta = new Vector2(-(m_VSliderWidth + m_VerticalScrollbarSpacing), viewRect.sizeDelta.y);

                // Recalculate content layout with this size to see if it fits vertically
                // when there is a vertical scrollbar (which may reflowed the content to make it taller).
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
                m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
                m_ContentBounds = GetBounds();
            }

            // If it doesn't fit horizontally, enable horizontal scrollbar and shrink view vertically to make room for it.
            if (m_HSliderExpand && hScrollingNeeded)
            {
                viewRect.sizeDelta = new Vector2(viewRect.sizeDelta.x, -(m_HSliderHeight + m_HorizontalScrollbarSpacing));
                m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
                m_ContentBounds = GetBounds();
            }

            // If the vertical slider didn't kick in the first time, and the horizontal one did,
            // we need to check again if the vertical slider now needs to kick in.
            // If it doesn't fit vertically, enable vertical scrollbar and shrink view horizontally to make room for it.
            if (m_VSliderExpand && vScrollingNeeded && viewRect.sizeDelta.x == 0 && viewRect.sizeDelta.y < 0)
            {
                viewRect.sizeDelta = new Vector2(-(m_VSliderWidth + m_VerticalScrollbarSpacing), viewRect.sizeDelta.y);
            }
        }

        public virtual void SetLayoutVertical()
        {
            UpdateScrollbarLayout();
            m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            m_ContentBounds = GetBounds();
        }

        void UpdateScrollbarVisibility()
        {
            UpdateOneScrollbarVisibility(vScrollingNeeded, m_Vertical, m_VerticalScrollbarVisibility, m_VerticalScrollbar);
            UpdateOneScrollbarVisibility(hScrollingNeeded, m_Horizontal, m_HorizontalScrollbarVisibility, m_HorizontalScrollbar);
        }

        private static void UpdateOneScrollbarVisibility(bool xScrollingNeeded, bool xAxisEnabled, ScrollbarVisibilityType scrollbarVisibility, Scrollbar scrollbar)
        {
            if (scrollbar)
            {
                if (scrollbarVisibility == ScrollbarVisibilityType.Permanent)
                {
                    if (scrollbar.gameObject.activeSelf != xAxisEnabled)
                        scrollbar.gameObject.SetActive(xAxisEnabled);
                }
                else
                {
                    if (scrollbar.gameObject.activeSelf != xScrollingNeeded)
                        scrollbar.gameObject.SetActive(xScrollingNeeded);
                }
            }
        }

        void UpdateScrollbarLayout()
        {
            if (m_VSliderExpand && m_HorizontalScrollbar)
            {
                m_Tracker.Add(this, m_HorizontalScrollbarRect,
                    DrivenTransformProperties.AnchorMinX |
                    DrivenTransformProperties.AnchorMaxX |
                    DrivenTransformProperties.SizeDeltaX |
                    DrivenTransformProperties.AnchoredPositionX);
                m_HorizontalScrollbarRect.anchorMin = new Vector2(0, m_HorizontalScrollbarRect.anchorMin.y);
                m_HorizontalScrollbarRect.anchorMax = new Vector2(1, m_HorizontalScrollbarRect.anchorMax.y);
                m_HorizontalScrollbarRect.anchoredPosition = new Vector2(0, m_HorizontalScrollbarRect.anchoredPosition.y);
                if (vScrollingNeeded)
                    m_HorizontalScrollbarRect.sizeDelta = new Vector2(-(m_VSliderWidth + m_VerticalScrollbarSpacing), m_HorizontalScrollbarRect.sizeDelta.y);
                else
                    m_HorizontalScrollbarRect.sizeDelta = new Vector2(0, m_HorizontalScrollbarRect.sizeDelta.y);
            }

            if (m_HSliderExpand && m_VerticalScrollbar)
            {
                m_Tracker.Add(this, m_VerticalScrollbarRect,
                    DrivenTransformProperties.AnchorMinY |
                    DrivenTransformProperties.AnchorMaxY |
                    DrivenTransformProperties.SizeDeltaY |
                    DrivenTransformProperties.AnchoredPositionY);
                m_VerticalScrollbarRect.anchorMin = new Vector2(m_VerticalScrollbarRect.anchorMin.x, 0);
                m_VerticalScrollbarRect.anchorMax = new Vector2(m_VerticalScrollbarRect.anchorMax.x, 1);
                m_VerticalScrollbarRect.anchoredPosition = new Vector2(m_VerticalScrollbarRect.anchoredPosition.x, 0);
                if (hScrollingNeeded)
                    m_VerticalScrollbarRect.sizeDelta = new Vector2(m_VerticalScrollbarRect.sizeDelta.x, -(m_HSliderHeight + m_HorizontalScrollbarSpacing));
                else
                    m_VerticalScrollbarRect.sizeDelta = new Vector2(m_VerticalScrollbarRect.sizeDelta.x, 0);
            }
        }

        /// <summary>
        /// 调整边界使内容永远不会超出内容边界
        /// </summary>
        protected void UpdateBounds(bool updateItems = false) 
        {
            m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            m_ContentBounds = GetBounds();

            if (m_Content == null)
                return;

            // Don't do this in Rebuild. Make use of ContentBounds before Adjust here.
            if (Application.isPlaying && updateItems && UpdateItems(ref m_ViewBounds, ref m_ContentBounds))
            {
                EnsureLayoutHasRebuilt();
                m_ContentBounds = GetBounds();
            }
            
            Vector3 contentSize = m_ContentBounds.size;
            Vector3 contentPos = m_ContentBounds.center;
            var contentPivot = m_Content.pivot;
            AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
            m_ContentBounds.size = contentSize;
            m_ContentBounds.center = contentPos;

            if (movementType == MovementType.Clamped)
            {
                // Adjust content so that content bounds bottom (right side) is never higher (to the left) than the view bounds bottom (right side).
                // top (left side) is never lower (to the right) than the view bounds top (left side).
                // All this can happen if content has shrunk.
                // This works because content size is at least as big as view size (because of the call to InternalUpdateBounds above).
                Vector2 delta = Vector2.zero;
                if (m_ViewBounds.max.x > m_ContentBounds.max.x)
                {
                    delta.x = Math.Min(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
                }
                else if (m_ViewBounds.min.x < m_ContentBounds.min.x)
                {
                    delta.x = Math.Max(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
                }

                if (m_ViewBounds.min.y < m_ContentBounds.min.y)
                {
                    delta.y = Math.Max(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
                }
                else if (m_ViewBounds.max.y > m_ContentBounds.max.y)
                {
                    delta.y = Math.Min(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
                }
                if (delta.sqrMagnitude > float.Epsilon)
                {
                    contentPos = m_Content.anchoredPosition + delta;
                    if (!m_Horizontal)
                        contentPos.x = m_Content.anchoredPosition.x;
                    if (!m_Vertical)
                        contentPos.y = m_Content.anchoredPosition.y;
                    AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
                }
            }
        }

        internal static void AdjustBounds(ref Bounds viewBounds, ref Vector2 contentPivot, ref Vector3 contentSize, ref Vector3 contentPos)
        {
            // Make sure content bounds are at least as large as view by adding padding if not.
            // One might think at first that if the content is smaller than the view, scrolling should be allowed.
            // However, that's not how scroll views normally work.
            // Scrolling is *only* possible when content is *larger* than view.
            // We use the pivot of the content rect to decide in which directions the content bounds should be expanded.
            // E.g. if pivot is at top, bounds are expanded downwards.
            // This also works nicely when ContentSizeFitter is used on the content.
            Vector3 excess = viewBounds.size - contentSize;
            if (excess.x > 0)
            {
                contentPos.x -= excess.x * (contentPivot.x - 0.5f);
                contentSize.x = viewBounds.size.x;
            }
            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (contentPivot.y - 0.5f);
                contentSize.y = viewBounds.size.y;
            }
        }

        private readonly Vector3[] m_Corners = new Vector3[4];
        private Bounds GetBounds()
        {
            if (m_Content == null)
                return new Bounds();
            m_Content.GetWorldCorners(m_Corners);
            var viewWorldToLocalMatrix = viewRect.worldToLocalMatrix;
            return InternalGetBounds(m_Corners, ref viewWorldToLocalMatrix);
        }

        internal static Bounds InternalGetBounds(Vector3[] corners, ref Matrix4x4 viewWorldToLocalMatrix)
        {
            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = viewWorldToLocalMatrix.MultiplyPoint3x4(corners[j]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);
            return bounds;
        }
        
        private Bounds GetBounds4Item(int index)
        {
            if (m_Content == null)
                return new Bounds();

            int offset = index - m_ItemDataIndexStart;
            if (offset < 0 || offset >= m_ShowObjs.Count)
                return new Bounds();

            var rt = m_ShowObjs[offset] as RectTransform;
            if (rt == null)
                return new Bounds();
            rt.GetWorldCorners(m_Corners);

            var viewWorldToLocalMatrix = viewRect.worldToLocalMatrix;
            return InternalGetBounds(m_Corners, ref viewWorldToLocalMatrix);
        }

        private Vector2 CalculateOffset(Vector2 delta)
        {
            if (totalCount < 0 || movementType == MovementType.Unrestricted)
                return delta;

            Bounds contentBound = m_ContentBounds;
            if (m_Horizontal)
            {
                float totalSize, offset;
                GetHorizonalOffsetAndSize(out totalSize, out offset);
                
                Vector3 center = contentBound.center;
                center.x = offset;
                contentBound.Encapsulate(center);
                center.x = offset + totalSize;
                contentBound.Encapsulate(center);
            }
            if (m_Vertical)
            {
                float totalSize, offset;
                GetVerticalOffsetAndSize(out totalSize, out offset);

                Vector3 center = contentBound.center;
                center.y = offset;
                contentBound.Encapsulate(center);
                center.y = offset - totalSize;
                contentBound.Encapsulate(center);
            }
            return InternalCalculateOffset(ref m_ViewBounds, ref contentBound, m_Horizontal, m_Vertical, m_MovementType, ref delta);
        }

        internal static Vector2 InternalCalculateOffset(ref Bounds viewBounds, ref Bounds contentBounds, bool horizontal, bool vertical, MovementType movementType, ref Vector2 delta)
        {
            Vector2 offset = Vector2.zero;
            if (movementType == MovementType.Unrestricted)
                return offset;
            
            Vector2 min = contentBounds.min;
            Vector2 max = contentBounds.max;

            // min/max offset extracted to check if approximately 0 and avoid recalculating layout every frame (case 1010178)

            if (horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;

                float maxOffset = viewBounds.max.x - max.x;
                float minOffset = viewBounds.min.x - min.x;

                if (minOffset < -0.001f)
                    offset.x = minOffset;
                else if (maxOffset > 0.001f)
                    offset.x = maxOffset;
            }

            if (vertical)
            {
                min.y += delta.y;
                max.y += delta.y;

                float maxOffset = viewBounds.max.y - max.y;
                float minOffset = viewBounds.min.y - min.y;

                if (maxOffset > 0.001f)
                    offset.y = maxOffset;
                else if (minOffset < -0.001f)
                    offset.y = minOffset;
            }

            return offset;
        }

        /// <summary>
        /// Override to alter or add to the code that keeps the appearance of the scroll rect synced with its data.
        /// </summary>
        protected void SetDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        /// <summary>
        /// 重写添加缓存数据的代码，避免重复的操作。
        /// </summary>
        protected void SetDirtyCaching()
        {
            if (!IsActive())
                return;

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirtyCaching();
        }

        #endif
    }
}