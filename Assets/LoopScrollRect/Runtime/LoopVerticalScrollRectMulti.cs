using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using LoopScrollRect.Core;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Loop Vertical Scroll Rect(MultiPrefab)", 53)]
    [DisallowMultipleComponent]
    public class LoopVerticalScrollRectMulti : LoopScrollRectMulti
    {
        protected override float GetSize(RectTransform item, bool includeSpacing)
        {
            float size = includeSpacing ? contentSpacing : 0;
            if (m_AutoSizeGridLayoutGroup != null)
            {
                if (m_AutoSizeGridLayoutGroup.isAutoSize)
                    size += item.rect.height;
                else
                    size += m_AutoSizeGridLayoutGroup.cellSize.y;
            }
            else if (m_GridLayout != null)
            {
                size += m_GridLayout.cellSize.y;
            }
            else
            {
                size += item.rect.height;
            }
            size *= m_Content.localScale.y;
            return size;
        }

        protected override float GetDimension(Vector2 vector)
        {
            return vector.y;
        }
        
        protected override float GetAbsDimension(Vector2 vector)
        {
            return vector.y;
        }

        protected override Vector2 GetVector(float value)
        {
            return new Vector2(0, value);
        }

        protected override void Awake()
        {
            m_Direction = LoopScrollRectDirectionType.Vertical;
            base.Awake();
            if (m_Content)
            {
                GridLayoutGroup layout = m_Content.GetComponent<GridLayoutGroup>();
                if (layout != null && layout.constraint != GridLayoutGroup.Constraint.FixedColumnCount)
                {
                    Debug.LogError("[LoopScrollRect] unsupported GridLayoutGroup constraint");
                }
            }
        }

        protected override bool UpdateItems(ref Bounds viewBounds, ref Bounds contentBounds)
        {
            bool changed = false;

            // special case: handling move several page in one frame
            if ((viewBounds.size.y < contentBounds.min.y - viewBounds.max.y) && m_ItemDataIndexEnd > m_ItemDataIndexStart)
            {
                int maxItemTypeStart = -1;
                if (totalCount >= 0)
                {
                    maxItemTypeStart = Mathf.Max(0, totalCount - (m_ItemDataIndexEnd - m_ItemDataIndexStart));
                }
                float currentSize = contentBounds.size.y;
                float elementSize = (currentSize - contentSpacing * (currentLines - 1)) / currentLines;
                ReturnToTempPool(true, m_ItemDataIndexEnd - m_ItemDataIndexStart);
                m_ItemDataIndexStart = m_ItemDataIndexEnd;

                int offsetCount = Mathf.FloorToInt((contentBounds.min.y - viewBounds.max.y) / (elementSize + contentSpacing));
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
                m_Content.anchoredPosition -= new Vector2(0, offset + (reverseDirection ? 0 : currentSize));
                contentBounds.center -= new Vector3(0, offset + currentSize / 2, 0);
                contentBounds.size = Vector3.zero;

                changed = true;
            }
            
            if ((viewBounds.min.y - contentBounds.max.y > viewBounds.size.y) && m_ItemDataIndexEnd > m_ItemDataIndexStart)
            {
                float currentSize = contentBounds.size.y;
                float elementSize = (currentSize - contentSpacing * (currentLines - 1)) / currentLines;
                ReturnToTempPool(false, m_ItemDataIndexEnd - m_ItemDataIndexStart);
                m_ItemDataIndexEnd = m_ItemDataIndexStart;

                int offsetCount = Mathf.FloorToInt((viewBounds.min.y - contentBounds.max.y) / (elementSize + contentSpacing));
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
                m_Content.anchoredPosition += new Vector2(0, offset + (reverseDirection ? currentSize : 0));
                contentBounds.center += new Vector3(0, offset + currentSize / 2, 0);
                contentBounds.size = Vector3.zero;

                changed = true;
            }

            if (viewBounds.min.y < contentBounds.min.y + m_ContentBottomPadding)
            {
                float size = NewItemAtEnd(), totalSize = size;
                while (size > 0 && viewBounds.min.y < contentBounds.min.y + m_ContentBottomPadding - totalSize)
                {
                    size = NewItemAtEnd();
                    totalSize += size;
                }
                if (totalSize > 0)
                    changed = true;
            }

            if (viewBounds.max.y > contentBounds.max.y - m_ContentTopPadding)
            {
                float size = NewItemAtStart(), totalSize = size;
                while (size > 0 && viewBounds.max.y > contentBounds.max.y - m_ContentTopPadding + totalSize)
                {
                    size = NewItemAtStart();
                    totalSize += size;
                }
                if (totalSize > 0)
                    changed = true;
            }

            if (viewBounds.min.y > contentBounds.min.y + itemEndIndexThreshold + m_ContentBottomPadding)
            {
                float size = DeleteItemAtEnd(), totalSize = size;
                while (size > 0 && viewBounds.min.y > contentBounds.min.y + itemEndIndexThreshold + m_ContentBottomPadding + totalSize)
                {
                    size = DeleteItemAtEnd();
                    totalSize += size;
                }
                if (totalSize > 0)
                    changed = true;
            }

            if (viewBounds.max.y < contentBounds.max.y - itemStartIndexThreshold - m_ContentTopPadding)
            {
                float size = DeleteItemAtStart(), totalSize = size;
                while (size > 0 && viewBounds.max.y < contentBounds.max.y - itemStartIndexThreshold - m_ContentTopPadding - totalSize)
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