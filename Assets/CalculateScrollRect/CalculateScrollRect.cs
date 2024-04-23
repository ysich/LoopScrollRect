/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2024-03-15 15:38:09
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using LoopScrollRect.Core;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Calculate
{
    public class CalculateScrollRect : LoopListBase
    {
        private List<RectTransform> m_ShowObjs = new List<RectTransform>();
        private Dictionary<int, int> m_ObjIndexDict = new Dictionary<int, int>();
        private Dictionary<int, int> m_DataIndexDict = new Dictionary<int, int>();
        private Stack<RectTransform> m_Pools = new Stack<RectTransform>();
        
        public int rowOrColumn = 1;

        protected float contentAnchoredPosition
        {
            set
            {
                var maxPoS = contentSizeDelta - viewportSizeDelta;
                if (value < 0)
                    value = 0;
                else if (value > maxPoS)
                    value = maxPoS;
                if (m_DirectionType == LoopScrollRectDirectionType.Vertical)
                    content.anchoredPosition = new Vector2(0, value);
                else
                    content.anchoredPosition = new Vector2(-value, 0);
            }
            get
            {
                Vector2 anchoredPosition = content.anchoredPosition;
                if (m_DirectionType == LoopScrollRectDirectionType.Vertical)
                    return anchoredPosition.y;
                return anchoredPosition.x;
            }
        }

        /// <summary>
        /// item的尺寸，horizontal则是宽度，Vertical则是高度。
        /// </summary>
        public Vector2 itemSize;

        /// <summary>
        /// item之间的间隔
        /// </summary>
        public Vector2 spacing;

        protected float rowOrColumnItemDimension
        {
            get
            {
                LoopScrollRectDirectionType directionType = scrollRect.horizontal
                    ? LoopScrollRectDirectionType.Vertical
                    : LoopScrollRectDirectionType.Horizontal;
                float size = GetAbsDimension(itemSize, directionType);
                float directionSpacing = GetAbsDimension(this.spacing, directionType);
                float dimension = size + directionSpacing;
                return dimension;
            }
        }

        protected float itemDimension
        {
            get
            {
                if (itemSize == Vector2.zero)
                {
                    throw new Exception("CalculateScrollRect:ItemSize 不能为0！");
                }

                return GetAbsDimension(itemSize) + GetAbsDimension(spacing);
            }
        }
        
        protected int m_DeletedItemTypeStart = 0;
        protected int m_DeletedItemTypeEnd = 0;
        
        protected override void OnScrollRectValueChanged(Vector2 pos)
        {
            UpdateItems();
        }

        protected int GetRowOrColumnIndex(int itemDataIndex)
        {
            int rowOrColumnIndex = itemDataIndex / rowOrColumn;
            return rowOrColumnIndex;
        }

        private void UpdateItems()
        {
            Vector2 contentPos = content.anchoredPosition;

            bool change = false;
            if (m_DirectionType == LoopScrollRectDirectionType.Vertical)
            {
                change = UpdateItemsByContent(contentPos.y, viewportSizeDelta);
            }
            else
            {
                change = UpdateItemsByContent(-contentPos.x, viewportSizeDelta);
            }

            if (change)
            {
                Debug.Log("UpdateItems~~");
            }
        }

        protected virtual bool UpdateItemsByContent(float contentStartPos, float viewSize)
        {
            bool change = false;
            //当前content显示结束的位置
            float contentEndPos = contentStartPos + viewSize;
            int rowOrColumnEndIndex = GetRowOrColumnIndex(m_ItemDataIndexEnd);
            float endItemPos = rowOrColumnEndIndex * itemDimension;
            //最后一个如果没到content的范围就向后添加
            if (endItemPos < contentEndPos && m_ItemDataIndexEnd < totalCount)
            {
                float totalSize = 0;
                while (endItemPos + totalSize < contentEndPos && m_ItemDataIndexEnd < totalCount)
                {
                    Debug.Log($"NewItemAtEnd_1:totalSize{totalSize},itemEndIndex{m_ItemDataIndexEnd}");
                    float size = NewItemAtEnd();
                    totalSize += size;
                }

                endItemPos += totalSize;

                if (totalSize > 0)
                {
                    change = true;
                }
            }

            int rowOrColumnStartIndex = GetRowOrColumnIndex(m_ItemDataIndexStart);
            float startItemPos = rowOrColumnStartIndex * itemDimension;
            //第一个如果超过了content的范围就向前添加
            if (startItemPos > contentStartPos && m_ItemDataIndexStart > 0)
            {
                float totalSize = 0;
                while (startItemPos - totalSize > contentStartPos && m_ItemDataIndexStart > 0)
                {
                    Debug.Log($"NewItemAtStart_1:totalSize{totalSize},itemStartIndex{m_ItemDataIndexStart}");
                    float size = NewItemAtStart();
                    totalSize += size;
                }

                startItemPos -= totalSize;

                if (totalSize > 0)
                {
                    change = true;
                }
            }

            if (endItemPos - contentEndPos >= itemDimension && m_ItemDataIndexEnd >= 0)
            {
                float totalSize = 0;
                while (endItemPos - contentEndPos - totalSize >= itemDimension && m_ItemDataIndexEnd >= 0)
                {
                    Debug.Log(
                        $"DeleteItemAtEnd_1:totalSize{totalSize},endItemPos{endItemPos},contentEndPos{contentEndPos},itemEndIndex{m_ItemDataIndexEnd}");
                    float size = DeleteItemAtEnd();
                    totalSize += size;
                }

                if (totalSize > 0)
                {
                    change = true;
                }
            }

            if (contentStartPos - startItemPos >= itemDimension && m_ItemDataIndexStart >= 0)
            {
                float totalSize = 0;
                while (contentStartPos - startItemPos - totalSize >= itemDimension && m_ItemDataIndexStart >= 0)
                {
                    Debug.Log(
                        $"DeleteItemAtStart_1:totalSize{totalSize},endItemPos{endItemPos},contentEndPos{contentEndPos},itemStartIndex{m_ItemDataIndexStart}");
                    float size = DeleteItemAtStart();
                    totalSize += size;
                }

                if (totalSize > 0)
                {
                    change = true;
                }
            }

            //最后再做清空，不然会出现闪烁
            if (change)
            {
                ClearTempPool();
            }

            return change;
        }

        protected float NewItemAtEnd()
        { 
            if (totalCount >= 0 && m_ItemDataIndexEnd >= totalCount)
            {
                return -1;
            }

            for (int i = 0; i < rowOrColumn; i++)
            {
                RectTransform nextItem = GetFromTempPool(m_ItemDataIndexEnd);
                SetItemAnchoredPosition(nextItem, m_ItemDataIndexEnd);
                m_ItemDataIndexEnd++;
            }

            return itemDimension;
        }

        protected float NewItemAtStart()
        {
            if (totalCount >= 0 && m_ItemDataIndexStart < 0)
            {
                return -1;
            }

            for (int i = 0; i < rowOrColumn; i++)
            {
                m_ItemDataIndexStart--;
                RectTransform nextItem = GetFromTempPool(m_ItemDataIndexStart);
                SetItemAnchoredPosition(nextItem, m_ItemDataIndexStart);
            }

            return itemDimension;
        }

        protected float DeleteItemAtEnd()
        {
            for (int i = 0; i < rowOrColumn; i++)
            {
                ReturnObjectToTempPool(false);
                m_ItemDataIndexEnd--;
            }

            return itemDimension;
        }

        protected float DeleteItemAtStart()
        {
            for (int i = 0; i < rowOrColumn; i++)
            {
                ReturnObjectToTempPool(true);
                m_ItemDataIndexStart++;
            }

            return itemDimension;
        }

        #region item回调

        protected override int GetObjIndexByRt(RectTransform objRT)
        {
            int itemIndex = m_ObjIndexDict[objRT.GetHashCode()];
            return itemIndex;
        }

        protected override int GetDataIndexByObjIndex(RectTransform objRT)
        {
            int dataIndex = m_DataIndexDict[objRT.GetHashCode()];
            return dataIndex;
        }

        #endregion

        private int GetObjSiblingIndex(int itemIndex)
        {
            return Mathf.Max(itemIndex - m_ItemDataIndexStart, 0);
        }

        #region pool

        private void ReturnObjectToTempPool(bool fromStart, int count = 1)
        {
            if (fromStart)
            {
                m_DeletedItemTypeStart += count;
            }
            else
            {
                m_DeletedItemTypeEnd += count;
            }
        }

        protected RectTransform GetFromTempPool(int itemDataIndex)
        {
            RectTransform nextItem = null;
            if (m_DeletedItemTypeStart > 0)
            {
                m_DeletedItemTypeStart--;
                nextItem = m_ShowObjs[m_DeletedItemTypeStart];
            }
            else if (m_DeletedItemTypeEnd > 0)
            {
                nextItem = m_ShowObjs[m_ShowObjs.Count - m_DeletedItemTypeEnd];
                m_DeletedItemTypeEnd--;
            }
            else
            {
                nextItem = GetObjectFormPool(itemDataIndex);
            }

            m_DataIndexDict[nextItem.GetHashCode()] = itemDataIndex;
            int itemSiblingIndex = Mathf.Max(0, itemDataIndex - m_ItemDataIndexStart + m_Pools.Count);
            nextItem.SetSiblingIndex(itemSiblingIndex);
            ProvideData(nextItem, itemDataIndex);
            return nextItem;
        }

        protected void ClearTempPool()
        {
            if (m_DeletedItemTypeStart > 0)
            {
                for (int i = m_DeletedItemTypeStart - 1; i >= 0; i--)
                {
                    RectTransform deletedItem = m_ShowObjs[i];
                    ReturnObjectToPool(deletedItem);
                }

                m_DeletedItemTypeStart = 0;
            }

            if (m_DeletedItemTypeEnd > 0)
            {
                int t = m_ShowObjs.Count - m_DeletedItemTypeEnd;
                for (int i = m_ShowObjs.Count - 1; i >= t; i--)
                {
                    RectTransform deletedItem = m_ShowObjs[i];
                    ReturnObjectToPool(deletedItem);
                }

                m_DeletedItemTypeEnd = 0;
            }
        }

        private void ReturnObjectToPool(RectTransform rectTransform)
        {
            rectTransform.gameObject.SetActive(false);
            m_ShowObjs.Remove(rectTransform);
            m_Pools.Push(rectTransform);
            rectTransform.SetAsFirstSibling();
        }

        protected RectTransform GetObjectFormPool(int itemDataIndex)
        {
            RectTransform nextItem;
            int itemIndex = GetObjSiblingIndex(itemDataIndex);
            if (m_Pools.Count > 0)
            {
                nextItem = m_Pools.Pop();
                nextItem.gameObject.SetActive(true);
                m_ShowObjs.Insert(itemIndex, nextItem);
                return nextItem;
            }

            nextItem = CreateNewItem();
            m_ShowObjs.Insert(itemIndex, nextItem);
            m_ObjIndexDict[nextItem.GetHashCode()] = m_ObjIndexDict.Count;

            if (m_DirectionType == LoopScrollRectDirectionType.Vertical)
            {
                var anchor = new Vector2(0, 1);
                nextItem.anchorMax = anchor;
                nextItem.anchorMin = anchor;
                nextItem.pivot = anchor;
            }
            else
            {
                var anchor = new Vector2(0, 1);
                nextItem.anchorMax = anchor;
                nextItem.anchorMin = anchor;
                nextItem.pivot = anchor;
            }

            return nextItem;
        }

        #endregion

        #region private

        protected virtual void SetItemAnchoredPosition(RectTransform itemRt, int itemDataIndex)
        {
            int rowOrColumnIndex = GetRowOrColumnIndex(itemDataIndex);
            int rowOrColumnItemIndex = itemDataIndex % rowOrColumn;
            float pos = rowOrColumnIndex * itemDimension;
            float rowOrColumnPos = rowOrColumnItemIndex * rowOrColumnItemDimension;
            Vector2 itemPos;
            if (m_DirectionType == LoopScrollRectDirectionType.Horizontal)
            {
                itemPos = new Vector2(pos, -rowOrColumnPos);
            }
            else
            {
                itemPos = new Vector2(rowOrColumnPos, -pos);
            }

            itemRt.anchoredPosition = itemPos;
        }
        
        #endregion

        #region public

        public void RefillCells(int newTotalCount, int startItemIndex = -1)
        {
            if (!Application.isPlaying)
                return;
            bool isChangeSize = newTotalCount != totalCount;
            bool isKeepPos = startItemIndex == -1;
            if (isKeepPos)
            {
                startItemIndex = m_ItemDataIndexStart;
            }

            int oldStartItemIndex = m_ItemDataIndexStart;
            totalCount = newTotalCount;
            if (totalCount > 0)
            {
                startItemIndex = Mathf.Clamp(startItemIndex, 0, newTotalCount - 1);
            }
            else
            {
                startItemIndex = 0;
            }

            m_ItemDataIndexStart = startItemIndex;
            m_ItemDataIndexEnd = m_ItemDataIndexStart;

            ReturnObjectToTempPool(false, m_ShowObjs.Count);

            float fillSize = viewportSizeDelta;
            float totalSize = 0;
            while (fillSize > totalSize)
            {
                float size = NewItemAtEnd();
                if (size < 0)
                    break;
                totalSize += size;
            }

            while (fillSize > totalSize)
            {
                float size = NewItemAtStart();
                if (size < 0)
                    break;
                totalSize += size;
            }

            if (isChangeSize)
            {
                //计算尺寸
                int totalRowOrColumnCount = Mathf.CeilToInt((float)totalCount / rowOrColumn);
                float contentSize = itemDimension * totalRowOrColumnCount;
                contentSizeDelta = contentSize;
            }

            //比如当前刷新20个，pos在18的位置，然后刷新为12个，整体会往前拉伸，这里要直接设置。
            if (!isKeepPos || (isKeepPos && oldStartItemIndex > startItemIndex))
            {
                float startRowOrColumnIndex = GetRowOrColumnIndex(startItemIndex);
                float directionPos = startRowOrColumnIndex * itemDimension;
                contentAnchoredPosition = directionPos;
            }

            scrollRect.StopMovement();
            ClearTempPool();
        }

        public void RefreshCells(int newTotalCount = -1)
        {
            if (!Application.isPlaying || !this.isActiveAndEnabled)
                return;

            if (newTotalCount == -1)
            {
                newTotalCount = totalCount;
            }

            RefillCells(newTotalCount);
        }

        /// <summary>
        /// 滑动到指定位置，单位毫秒
        /// </summary>
        /// <param name="index"></param>
        /// <param name="speed"></param>
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
                RefillCells(totalCount, index);
                return;
            }

            StartCoroutine(ScrollToCellCoroutine(index, speed));
        }

        IEnumerator ScrollToCellCoroutine(int index, float speed)
        {
            scrollRect.StopMovement();
            bool needMoving = true;
            while (needMoving)
            {
                yield return null;
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
                    //todo:这里需要对位置进行矫正，判断index是否在列表的最上方
                    needMoving = false;
                }

                if (move != 0)
                {
                    Vector2 offset;
                    if (m_DirectionType == LoopScrollRectDirectionType.Vertical)
                    {
                        offset = new Vector2(0, move);
                    }
                    else
                    {
                        offset = new Vector2(move,0);
                    }
                    content.anchoredPosition += offset;
                }
            }

            scrollRect.StopMovement();
        }

        #endregion
    }
}