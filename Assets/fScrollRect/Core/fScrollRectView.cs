using System;
using System.Collections;
using System.Collections.Generic;
using fScrollRect.Core;
using LoopScrollRect.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEngine.UI
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(fScroller))]
    public class fScrollRectView : UIBehaviour
    {
        [SerializeField]
        private int m_TotalCount;

        public int totalCount
        {
            get { return m_TotalCount; }
            set
            {
                scroller.totalCount = value;
                m_TotalCount = value;
            }
        }

        [SerializeField]
        private Transform m_Content;

        //todo:改为创建方法
        public GameObject InstantiateItem;

        [SerializeField]
        private fScroller m_Scroller;

        public fScroller scroller
        {
            get
            {
                if (!m_Scroller)
                {
                    m_Scroller = GetComponent<fScroller>();
                }
                return m_Scroller;
            }
        }

        [SerializeField,Range(1e-2f,1f)]
        [Tooltip("item之间的间隔")]
        private float m_ItemInterval = 0.2f;

        [SerializeField,Range(0f,1f)]
        private float m_ScrollOffset = 0.5f;

        [SerializeField]
        private bool m_Loop;

        // private Stack<RectTransform> m_ObjPool = new Stack<RectTransform>();
        private List<RectTransform> m_ShowObjs = new List<RectTransform>();
        private Dictionary<RectTransform, int> m_ItemIndexByObj = new Dictionary<RectTransform, int>();

        private float m_CurrentPosition;

        private Func<RectTransform> m_OnCreateItemFunc;

        #region 内部接口

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            if (!scroller)
            {
                scroller.onValueChanged = OnScrollerValueChange;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        private void OnScrollerValueChange(float position)
        {
            
        }

        private void UpdatePosition(float position, bool forceRefresh)
        {
            m_CurrentPosition = position;
            float p = position - m_ScrollOffset / m_ItemInterval;
            int firstItemIndex = Mathf.CeilToInt(p);
            float firstItemPosition = (Mathf.Ceil(p) - p) * m_ItemInterval;

            if (firstItemPosition + m_ShowObjs.Count * m_ItemInterval < 1f)
            {
                int addItemCount = Mathf.CeilToInt((1f - firstItemPosition) / m_CurrentPosition) - m_ShowObjs.Count;
                for (int i = 0; i < addItemCount; i++)
                {
                     GameObject item = Instantiate(InstantiateItem, m_Content);
                     item.SetActive(false);
                }
            }

            UpdateItems(firstItemPosition, firstItemIndex, forceRefresh);
        }
        

        private void UpdateItems(float firstItemPosition,int firstItemIndex,bool forceRefresh)
        {
            for (int i = 0; i < m_ShowObjs.Count; i++)
            {
                var index = firstItemIndex + i;
                var position = firstItemPosition + i * m_ItemInterval;
                var item = m_ShowObjs[CircularIndex(index, m_ShowObjs.Count)];

                if (m_Loop)
                {
                    index = CircularIndex(index, totalCount);
                }

                if (index < 0 || index >= totalCount || position > 1f)
                {
                    item.gameObject.SetActive(false);
                    continue;
                }

                if (forceRefresh || GetItemIndexByObj(item) != index || !item.gameObject.activeSelf)
                {
                    m_ItemIndexByObj[item] = index;
                    item.gameObject.SetActive(true);
                }
                // if (forceRefresh || cell.Index != index || !cell.IsVisible)
                // {
                //     cell.Index = index;
                //     cell.SetVisible(true);
                //     cell.UpdateContent(ItemsSource[index]);
                // }
                //
                // cell.UpdatePosition(position);
            }
        }

        private int GetItemIndexByObj(RectTransform rt)
        {
            if (m_ItemIndexByObj.TryGetValue(rt, out int index))
            {
                return index;
            }

            return -1;
        }
        
        private int CircularIndex(int i, int size) => size < 1 ? 0 : i < 0 ? size - 1 + (i + 1) % size : i % size;

        // private RectTransform GetObjFromPool(int dataIndex)
        // {
        //     RectTransform rectTransform;
        //     if (m_ObjPool.Count > 0)
        //     {
        //         rectTransform = m_ObjPool.Pop();
        //         m_ShowObjs.Insert(dataIndex,rectTransform);
        //         return rectTransform;
        //     }
        //
        //     rectTransform = m_OnCreateItemFunc.Invoke();
        //     return rectTransform;
        // } 
        //
        // private void ReturnObjectToPool(RectTransform rectTransform)
        // {
        // }

        #endregion

        #region 外部接口

        public void RefillCells(int newTotalCount, int startItemIndex = -1)
        {
            
        }

        /// <summary>
        /// 刷新列表数据，原地刷新
        /// </summary>
        public void RefreshCells(int newTotalCount = -1)
        {
        }

        /// <summary>
        /// 刷新列表，并且展示最后一个item。适合聊天框类似的场景。
        /// </summary>
        /// <param name="newTotalCount"></param>
        /// <param name="endItem">展示倒数第几个item</param>
        /// <param name="alignStart"></param>
        public void RefillCellsFromEnd(int newTotalCount, int endItem = 0, bool alignStart = false)
        {
        }

        #endregion

        #region 回调接口

        protected Func<int, GameObject> m_OnCreateItemHandler;

        /// <summary>
        /// 设置创建Item的函数
        /// </summary>
        /// <param name="acticon">itemDataIndex,return GameObject</param>
        public void SetOnCreateItemHandler(Func<int, GameObject> acticon)
        {
            m_OnCreateItemHandler = acticon;
        }

        /// <summary>
        /// 刷新item的回调返回尺寸
        /// </summary>
        protected Func<float,int, int> m_OnFlushItem;

        /// <summary>
        /// 设置Item刷新的回调函数
        /// </summary>
        /// <param name="action">objIndex,itemDataIndex</param>
        public void SetOnFlushItemHandler(Func<float,int, int> action)
        {
            m_OnFlushItem = action;
        }

        #endregion
        
    }
}