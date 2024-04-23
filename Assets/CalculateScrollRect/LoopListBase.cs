using System;
using System.Collections.Generic;
using Calculate.Interface;
using LoopScrollRect.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Calculate
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(ScrollRect))]
    public abstract class LoopListBase : UIBehaviour
    {
        public int totalCount;
        
        public RectTransform content;

        protected int m_ItemDataIndexStart = 0;
        protected int m_ItemDataIndexEnd = 0;

        private ScrollRect m_ScrollRect;

        protected ScrollRect scrollRect
        {
            get
            {
                if (m_ScrollRect == null)
                {
                    m_ScrollRect = this.GetComponent<ScrollRect>();
                }

                return m_ScrollRect;
            }
        }
        
        protected LoopScrollRectDirectionType m_DirectionType => scrollRect.horizontal
            ? LoopScrollRectDirectionType.Horizontal
            : LoopScrollRectDirectionType.Vertical;
        
        protected float contentSizeDelta
        {
            set
            {
                var sizeDelta = content.sizeDelta;
                if (m_DirectionType == LoopScrollRectDirectionType.Vertical)
                    content.sizeDelta = new Vector2(sizeDelta.x, value);
                else
                    content.sizeDelta = new Vector2(value, sizeDelta.y);
            }
            get
            {
                var sizeDelta = content.sizeDelta;
                return GetAbsDimension(sizeDelta);
            }
        }
        
        private RectTransform m_Viewport;

        protected RectTransform viewport
        {
            get
            {
                if (m_Viewport == null)
                    m_Viewport = transform.GetComponent<RectTransform>();
                return m_Viewport;
            }
        }

        protected float viewportSizeDelta
        {
            get
            {
                Vector2 sizeDelta = viewport.sizeDelta;
                return GetAbsDimension(sizeDelta);
            }
        }
        
        protected float GetAbsDimension(Vector2 vector2)
        {
            return GetAbsDimension(vector2, m_DirectionType);
        }
        
        protected float GetAbsDimension(Vector2 vector2, LoopScrollRectDirectionType directionType)
        {
            if (directionType == LoopScrollRectDirectionType.Vertical)
                return vector2.y;
            return vector2.x;
        }

        #region Base
        
        protected override void Awake()
        {
            base.Awake();
            scrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);
        }

        /// <summary>
        /// 滑动监听
        /// </summary>
        /// <param name="pos"></param>
        protected abstract void OnScrollRectValueChanged(Vector2 pos);
        
        protected abstract int GetObjIndexByRt(RectTransform objRT);

        protected abstract int GetDataIndexByObjIndex(RectTransform objRT);

        #endregion

        #region 回调

        protected Func<RectTransform> m_OnCreateItemHandler;

        protected Action<int, int> m_OnFlushItemHandler;

        public void SetOnCreateItemHandler(Func<RectTransform> handler)
        {
            m_OnCreateItemHandler = handler;
        }

        public void SetOnFlushItemHandler(Action<int, int> handler)
        {
            m_OnFlushItemHandler = handler;
        }

        protected RectTransform CreateNewItem()
        {
            RectTransform rectTransform = m_OnCreateItemHandler?.Invoke();
            if (this.selectionMode == SelectionMode.Single
                || this.selectionMode == SelectionMode.Multi)
            {
                BindItemSelectionHandler(rectTransform);
            }
            return rectTransform;
        }
        
        protected void ProvideData(RectTransform itemRT, int itemDataIndex)
        {
            try
            {
                int objIndex = GetObjIndexByRt(itemRT);
                if (m_OnFlushItemHandler != null)
                    m_OnFlushItemHandler(objIndex, itemDataIndex);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    string.Format("LoopScrollRect：滑动组件：{0}", e));
                throw;
            }
        }

        #endregion
      
        
        #region Click

        protected HashSet<int> selectedIndexMap = new HashSet<int>();

        public int selectedIndex;

        public SelectionMode selectionMode;

        private Action<int, int> m_ClickItemHandler;
        
        /// <summary>
        /// 绑定Item点击回调
        /// </summary>
        /// <param name="rectTransform"></param>
        private void BindItemSelectionHandler(RectTransform rectTransform)
        {
            Button button = rectTransform.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("SelectionMode.Single:Item未绑定Button");
                return;
            }
            button.onClick.AddListener(()=>OnClickItemHandler(rectTransform));
        }
        /// <summary>
        /// 给外面的业务逻辑用的
        /// </summary>
        /// <param name="handler"></param>
        public void SetOnClickItemHandler(Action<int, int> handler)
        {
            m_ClickItemHandler = handler;
        }

        private void OnClickItemHandler(RectTransform rectTransform)
        {
            if (selectionMode == SelectionMode.None)
            {
                return;
            }
            scrollRect.StopMovement();
            int objIndex = GetObjIndexByRt(rectTransform);
            int dataIndex = GetDataIndexByObjIndex(rectTransform);
            if (selectionMode == SelectionMode.Single)
            {
                int oldSelectedIndex = selectedIndex;
                selectedIndex = -1;
                selectedIndexMap.Clear();
                if (oldSelectedIndex != dataIndex)
                {
                    selectedIndex = dataIndex;
                    selectedIndexMap.Add(selectedIndex);
                }

                if (this is ISingleClick singleClick)
                    singleClick.OnSingleClick(oldSelectedIndex, selectedIndex);
            }
            else if (selectionMode == SelectionMode.Multi)
            {
                bool isSelected = false;
                if (selectedIndexMap.Contains(dataIndex))
                {
                    selectedIndexMap.Remove(dataIndex);
                }
                else
                {
                    selectedIndexMap.Add(dataIndex);
                    isSelected = true;
                }
                if(this is IMultClick multClick)
                    multClick.OnMultClick(dataIndex,isSelected);
            }
            m_ClickItemHandler?.Invoke(objIndex,dataIndex);
        }

        public bool IsSelected(int index)
        {
            if (index < 0)
            {
                return false;
            }
            if (selectionMode == SelectionMode.Single)
            {
                return selectedIndex == index;
            }
            if(selectionMode == SelectionMode.Multi)
            {
                return selectedIndexMap.Contains(index);
            }

            return false;
        }

        #endregion

    }
}