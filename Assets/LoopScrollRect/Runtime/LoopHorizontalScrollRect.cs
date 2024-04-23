using UnityEngine;
using System.Collections;
using LoopScrollRect.Core;
using UnityEngine.UI;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Loop Horizontal Scroll Rect", 50)]
    [DisallowMultipleComponent]
    public class LoopHorizontalScrollRect : LoopScrollRect
    {
        protected override float GetSize(RectTransform item, bool includeSpacing)
        {
            float size = includeSpacing ? contentSpacing : 0;
            if (m_AutoSizeGridLayoutGroup != null)
            {
                if (m_AutoSizeGridLayoutGroup.isAutoSize)
                    size += item.rect.width;
                else
                    size += m_AutoSizeGridLayoutGroup.cellSize.x;
            }
            else if (m_GridLayout != null)
            {
                size += m_GridLayout.cellSize.x;
            }
            else
            {
                size += item.rect.width;
            }
            size *= m_Content.localScale.x;
            return size;
        }

        protected override float GetDimension(Vector2 vector)
        {
            return -vector.x;
        }
        
        protected override float GetAbsDimension(Vector2 vector)
        {
            return vector.x;
        }

        protected override Vector2 GetVector(float value)
        {
            return new Vector2(-value, 0);
        }

        protected override void Awake()
        {
            m_Direction = LoopScrollRectDirectionType.Horizontal;
            base.Awake();
            if (m_Content)
            {
                GridLayoutGroup layout = m_Content.GetComponent<GridLayoutGroup>();
                if (layout != null && layout.constraint != GridLayoutGroup.Constraint.FixedRowCount)
                {
                    Debug.LogError("[LoopScrollRect] unsupported GridLayoutGroup constraint");
                }
            }
        }

        protected override bool UpdateItems(ref Bounds viewBounds, ref Bounds contentBounds)
        {
            bool changed = false;

            // 特殊情况:在一个帧内移动多个页面
            if ((viewBounds.size.x < contentBounds.min.x - viewBounds.max.x) && m_ItemDataIndexEnd > m_ItemDataIndexStart)
            {
                float currentSize = contentBounds.size.x;
                float elementSize = (currentSize - contentSpacing * (currentLines - 1)) / currentLines;
                ReturnToTempPool(false, m_ItemDataIndexEnd - m_ItemDataIndexStart);
                m_ItemDataIndexEnd = m_ItemDataIndexStart;

                int offsetCount = Mathf.FloorToInt((contentBounds.min.x - viewBounds.max.x) / (elementSize + contentSpacing));
                if (totalCount >= 0 && m_ItemDataIndexStart - offsetCount * contentConstraintCount < 0)
                {
                    offsetCount = Mathf.FloorToInt((float)(m_ItemDataIndexStart) / contentConstraintCount);
                }
                m_ItemDataIndexStart -= offsetCount * contentConstraintCount;
                if (totalCount >= 0)
                {
                    m_ItemDataIndexStart = Mathf.Max(m_ItemDataIndexStart, 0);
                }
                m_ItemDataIndexEnd = m_ItemDataIndexStart;

                float offset = offsetCount * (elementSize + contentSpacing);
                m_Content.anchoredPosition -= new Vector2(offset + (reverseDirection ? currentSize : 0), 0);
                contentBounds.center -= new Vector3(offset + currentSize / 2, 0, 0);
                contentBounds.size = Vector3.zero;

                changed = true;
            }

            if ((viewBounds.min.x - contentBounds.max.x > viewBounds.size.x)  && m_ItemDataIndexEnd > m_ItemDataIndexStart)
            {
                int maxItemTypeStart = -1;
                if (totalCount >= 0)
                {
                    maxItemTypeStart = Mathf.Max(0, totalCount - (m_ItemDataIndexEnd - m_ItemDataIndexStart));
                    maxItemTypeStart = (maxItemTypeStart / contentConstraintCount) * contentConstraintCount;
                }
                float currentSize = contentBounds.size.x;
                float elementSize = (currentSize - contentSpacing * (currentLines - 1)) / currentLines;
                ReturnToTempPool(true, m_ItemDataIndexEnd - m_ItemDataIndexStart);
                // TODO: fix with contentConstraint?
                m_ItemDataIndexStart = m_ItemDataIndexEnd;
            
                int offsetCount = Mathf.FloorToInt((viewBounds.min.x - contentBounds.max.x) / (elementSize + contentSpacing));
                if (maxItemTypeStart >= 0 && m_ItemDataIndexStart + offsetCount * contentConstraintCount > maxItemTypeStart)
                {
                    offsetCount = Mathf.FloorToInt((float)(maxItemTypeStart - m_ItemDataIndexStart) / contentConstraintCount);
                }
                m_ItemDataIndexStart += offsetCount * contentConstraintCount;
                if (totalCount >= 0)
                {
                    m_ItemDataIndexStart = Mathf.Max(m_ItemDataIndexStart, 0);
                }
                m_ItemDataIndexEnd = m_ItemDataIndexStart;

                float offset = offsetCount * (elementSize + contentSpacing);
                m_Content.anchoredPosition += new Vector2(offset + (reverseDirection ? 0 : currentSize), 0);
                contentBounds.center += new Vector3(offset + currentSize / 2, 0, 0);
                contentBounds.size = Vector3.zero;

                changed = true;
            }
            //可视范围右端大于content的右端，要在右边插入
            if (viewBounds.max.x > contentBounds.max.x - m_ContentRightPadding)
            {
                float size = NewItemAtEnd(), totalSize = size;
                while (size > 0 && viewBounds.max.x > contentBounds.max.x - m_ContentRightPadding + totalSize)
                {
                    size = NewItemAtEnd();
                    totalSize += size;
                }
                if (totalSize > 0)
                    changed = true;
            }
            //可视范围左端小于content的左端，要在左边插入
            if (viewBounds.min.x < contentBounds.min.x + m_ContentLeftPadding)
            {
                float size = NewItemAtStart(), totalSize = size;
                while (size > 0 && viewBounds.min.x < contentBounds.min.x + m_ContentLeftPadding - totalSize)
                {
                    size = NewItemAtStart();
                    totalSize += size;
                }
                if (totalSize > 0)
                    changed = true;
            }
            //content的右端大于可视范围的左端，要从右边回收
            if (viewBounds.max.x < contentBounds.max.x - itemEndIndexThreshold - m_ContentRightPadding)
            {
                float size = DeleteItemAtEnd(), totalSize = size;
                while (size > 0 && viewBounds.max.x < contentBounds.max.x - itemEndIndexThreshold - m_ContentRightPadding - totalSize)
                {
                    size = DeleteItemAtEnd();
                    totalSize += size;
                }
                if (totalSize > 0)
                    changed = true;
            }
            //content的左端小于可视范围的左端，要从左边回收
            
            if (viewBounds.min.x > contentBounds.min.x + itemStartIndexThreshold + m_ContentLeftPadding)
            {
                float size = DeleteItemAtStart(), totalSize = size;
                while (size > 0 && viewBounds.min.x > contentBounds.min.x + itemStartIndexThreshold + m_ContentLeftPadding + totalSize)
                {
                    size = DeleteItemAtStart();
                    totalSize += size;
                }
                if (totalSize > 0)
                    changed = true;
            }

            if (changed)
            {
                ClearTempPool();
            }

            return changed;
        }
    }
}