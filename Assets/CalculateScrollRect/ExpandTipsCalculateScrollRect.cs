/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2024-04-01 15:19:05
-- 概述: 单行单列或者多行多列，点击展开tips，这种情况下SelectMode只能是Single
---------------------------------------------------------------------------------------*/

using System;
using System.Numerics;
using Calculate.Interface;
using LoopScrollRect.Core;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace Calculate
{
    public class ExpandTipsCalculateScrollRect : CalculateScrollRect,ISingleClick
    {
        public float expandTipsSize;
        public float expandSpacing;
        private RectTransform m_ExpandTipsRt;

        private int m_ExpandRowOrColumnIndex = 0;
        private bool m_IsExpand = false;
        protected float expandTipsDimension => expandTipsSize + expandSpacing * 2 ;
        
        protected override void Awake()
        {
            base.Awake();
            selectionMode = SelectionMode.Single;
        }

        protected Func<RectTransform> m_OnCreateExpandTipsHandler;

        public void SetOnCreateExpandTipsHandler(Func<RectTransform> handler)
        {
            m_OnCreateExpandTipsHandler = handler;
        }

        public void OpenExpandTips(int itemDataIndex)
        {
            if (!m_IsExpand)
            {
                float oldContentSizeDelta = contentSizeDelta;
                float newContentSizeDelta = oldContentSizeDelta + expandTipsDimension;
                contentSizeDelta = newContentSizeDelta;
            }
            int rowOrColumnIndex = GetRowOrColumnIndex(itemDataIndex);
            //记录下展开的行数,后面的item需要避让。
            m_ExpandRowOrColumnIndex = rowOrColumnIndex;
            m_IsExpand = true;
            float itemSpacing = GetAbsDimension(spacing);
            //这里要+1因为要显示在这一行的下方
            float expandTipsPos = (rowOrColumnIndex + 1 ) * itemDimension - itemSpacing + expandSpacing;

            if (m_ExpandTipsRt == null)
            {
                m_ExpandTipsRt = m_OnCreateExpandTipsHandler.Invoke();
                var anchor = new Vector2(0, 1);
                m_ExpandTipsRt.anchorMax = anchor;
                m_ExpandTipsRt.anchorMin = anchor;
                m_ExpandTipsRt.pivot = anchor;
            }
            else
            {
                m_ExpandTipsRt.gameObject.SetActive(true);
            }
            
            if (m_DirectionType == LoopScrollRectDirectionType.Vertical)
            {
                m_ExpandTipsRt.anchoredPosition = new Vector2(0, -expandTipsPos);
            }
            else
            {
                m_ExpandTipsRt.anchoredPosition = new Vector2(expandTipsPos, 0);
            }
            
            RefreshCells();
        }

        public void CloseExpandTips()
        {
            if (!m_IsExpand)
            {
                return;
            }
            m_IsExpand = false;
            float oldContentSizeDelta = contentSizeDelta;
            contentSizeDelta = oldContentSizeDelta - expandTipsDimension;
            m_ExpandTipsRt.gameObject.SetActive(false);
            //上面设置完ContentSize后，这里原地刷不会刷新ContentSize
            RefreshCells();
        }
        
        #region override

        protected override bool UpdateItemsByContent(float contentStartPos, float viewSize)
        {
            //这里判断范围在前后加上Tips的范围，如果动态加减计算会出现问题
            float expandSize = expandTipsDimension - GetAbsDimension(spacing);
            viewSize += expandSize;
            contentStartPos -= expandSize;
            return base.UpdateItemsByContent(contentStartPos, viewSize);
        }

        protected override void SetItemAnchoredPosition(RectTransform itemRt, int itemDataIndex)
        {
            int rowOrColumnIndex = GetRowOrColumnIndex(itemDataIndex);
            int rowOrColumnItemIndex = itemDataIndex % rowOrColumn;
            float itemSpacing = GetAbsDimension(spacing);
            float pos = rowOrColumnIndex * itemDimension;
            //超过展开的行，后面的都需要加展开行的尺寸
            if (m_IsExpand && rowOrColumnIndex > m_ExpandRowOrColumnIndex)
            {
                pos += expandTipsDimension;
                pos -= itemSpacing;
            }

            float rowOrColumnPos = rowOrColumnItemIndex * rowOrColumnItemDimension;
            Vector2 itemPos;
            if (m_DirectionType == LoopScrollRectDirectionType.Vertical)
            {
                itemPos = new Vector2(rowOrColumnPos, -pos);
            }
            else
            {
                itemPos = new Vector2(pos, -rowOrColumnPos);
            }

            itemRt.anchoredPosition = itemPos;
        }

        #endregion

        void ISingleClick.OnSingleClick(int oldSelectIndex, int selectIndex)
        {
            if (oldSelectIndex == selectedIndex && m_IsExpand)
            {
                CloseExpandTips();
                return;
            }
            OpenExpandTips(selectIndex);
        }
    }
}