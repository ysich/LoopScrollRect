using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 为LoopScrollRect服务的GridLayoutGroup。主要分为竖向流动和横向流动两种
/// </summary>
public class AutoSizeGridLayoutGroup : GridLayoutGroup
{
    private Dictionary<int, float> m_ColumnMaxWidthMap = new Dictionary<int, float>();
    private Dictionary<int, float> m_RowMaxHeightMap = new Dictionary<int, float>();
    
    [Tooltip("开启自动大小后高度将自动适配,宽度依然使用CellSize设定好的")] [SerializeField]
    public bool m_IsAutoSize = false;

    public bool isAutoSize
    {
        get { return m_IsAutoSize; }
        set { SetProperty(ref m_IsAutoSize, value); }
    }

    [SerializeField] protected bool m_ChildControlWidth = true;

    public bool childControlWidth
    {
        get { return m_ChildControlWidth; }
        set { SetProperty(ref m_ChildControlWidth, value); }
    }

    [SerializeField] protected bool m_ChildControlHeight = true;

    public bool childControlHeight
    {
        get { return m_ChildControlHeight; }
        set { SetProperty(ref m_ChildControlHeight, value); }
    }

    protected AutoSizeGridLayoutGroup()
    {
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        constraintCount = constraintCount;
    }

#endif
    /// <summary>
    /// 由布局系统调用，以计算水平布局大小。
    /// 收集子节点下没有被ignoreLayout的物体。
    /// </summary>
    public override void CalculateLayoutInputHorizontal()
    {
        m_ColumnMaxWidthMap.Clear();
        m_RowMaxHeightMap.Clear();

        //计算了布局元素的minWidth，preferredWidth和flexibleWidth值
        base.CalculateLayoutInputHorizontal();

        int minColumns = 0;
        int preferredColumns = 0;
        if (m_Constraint == Constraint.FixedColumnCount)
        {
            minColumns = preferredColumns = m_ConstraintCount;
        }
        else if (m_Constraint == Constraint.FixedRowCount)
        {
            minColumns = preferredColumns = Mathf.CeilToInt(rectChildren.Count / (float)m_ConstraintCount - 0.001f);
        }
        else
        {
            minColumns = 1;
            preferredColumns = Mathf.CeilToInt(Mathf.Sqrt(rectChildren.Count));
        }

        if (m_IsAutoSize && m_ChildControlWidth)
        {
            //自动宽度需要遍历所有的Child
            //取每一列中最大的width
            float totalMinColumnWidth = padding.horizontal - spacing.x;
            float totalPreferredColumnWidth = padding.horizontal - spacing.x;

            int rectChildrenCount = rectChildren.Count;
            int minCellCountX = 0;
            if (minColumns > 0)
                minCellCountX = rectChildrenCount / minColumns + (rectChildrenCount % minColumns > 0 ? 1 : 0);
            int preferredCountX = 0;
            if(preferredColumns > 0)
                preferredCountX = rectChildrenCount / preferredColumns + (rectChildrenCount % preferredColumns > 0 ? 1 : 0);
            int minCellCountXIndex = 0;
            int preferredCellCountXIndex = 0;
            float itemMinWidth = 0;
            float itemPreferredWidth = 0;
            int axis = 0;

            int columnIndex = 0;
            for (int i = 0; i < rectChildrenCount; i++)
            {
                RectTransform child = rectChildren[i];
                float min, preferred, flexible;
                GetChildSizes(child, axis, true, out min, out preferred, out flexible);
                itemMinWidth = Mathf.Max(itemMinWidth, min);
                itemPreferredWidth = Mathf.Max(itemPreferredWidth, preferred);
                minCellCountXIndex++;

                if (minCellCountXIndex >= minCellCountX || i + 1 >= rectChildrenCount)
                {
                    //这里直接存进去，在排版的时候就不需要算了
                    //这里和高度保持一致，也是用minSize来存。
                    columnIndex++;
                    m_ColumnMaxWidthMap.Add(columnIndex, itemMinWidth);

                    totalMinColumnWidth += (itemMinWidth + spacing.x);
                    minCellCountXIndex = 0;
                    itemMinWidth = 0;
                }

                preferredCellCountXIndex++;
                if (preferredCellCountXIndex >= preferredCountX || i + 1 >= rectChildrenCount)
                {
                    totalPreferredColumnWidth += (itemPreferredWidth + spacing.x);
                    preferredCellCountXIndex = 0;
                    itemPreferredWidth = 0;
                }
            }

            SetLayoutInputForAxis(totalMinColumnWidth, totalPreferredColumnWidth, -1, 0);
        }
        else
        {
            //非自动大小，使用CellSize来计算
            SetLayoutInputForAxis(
                padding.horizontal + (cellSize.x + spacing.x) * minColumns - spacing.x,
                padding.horizontal + (cellSize.x + spacing.x) * preferredColumns - spacing.x,
                -1, 0);
        }
    }

    /// <summary>
    /// 由布局系统调用，以计算垂直布局大小。
    /// 收集子节点下没有被ignoreLayout的物体。
    /// </summary>
    public override void CalculateLayoutInputVertical()
    {
        //计算了布局元素的minWidth，preferredWidth和flexibleWidth值
        int minRows = 0;
        if (m_Constraint == Constraint.FixedColumnCount)
        {
            minRows = Mathf.CeilToInt(rectChildren.Count / (float)m_ConstraintCount - 0.001f);
        }
        else if (m_Constraint == Constraint.FixedRowCount)
        {
            minRows = m_ConstraintCount;
        }
        else
        {
            float width = rectTransform.rect.width;
            int cellCountX = Mathf.Max(1,
                Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
            minRows = Mathf.CeilToInt(rectChildren.Count / (float)cellCountX);
        }

        if (m_IsAutoSize && m_ChildControlHeight)
        {
            //自动宽度需要遍历所有的Child
            //取每一列中最大的height
            float minSpace = padding.vertical - spacing.y;
            int rectChildrenCount = rectChildren.Count;
            int cellCountY = 0;
            if(minRows > 0)
                cellCountY = rectChildrenCount / minRows + (rectChildrenCount % minRows > 0 ? 1 : 0);
            int cellCountYIndex = 0;
            float itemMinHeight = 0;
            int axis = 1;
            int rowIndex = 0;
            for (int i = 0; i < rectChildrenCount; i++)
            {
                RectTransform child = rectChildren[i];
                float min, preferred, flexible;
                GetChildSizes(child, axis, true, out min, out preferred, out flexible);
                itemMinHeight = Mathf.Max(itemMinHeight, preferred);
                cellCountYIndex++;
                if (cellCountYIndex >= cellCountY || i + 1 >= rectChildrenCount)
                {
                    //这里直接存进去，在排版的时候就不需要算了
                    rowIndex++;
                    m_RowMaxHeightMap.Add(rowIndex, itemMinHeight);

                    minSpace += (itemMinHeight + spacing.y);
                    cellCountYIndex = 0;
                    itemMinHeight = 0;
                }
            }

            SetLayoutInputForAxis(minSpace, minSpace, -1, 1);
        }
        else
        {
            //非自动大小，使用CellSize来计算
            float minSpace = padding.vertical + (cellSize.y + spacing.y) * minRows - spacing.y;
            SetLayoutInputForAxis(minSpace, minSpace, -1, 1);
        }
    }

    /// <summary>
    /// 由布局系统调用
    /// 计算横向layout大小
    /// </summary>
    public override void SetLayoutHorizontal()
    {
        SetCellsAlongAxis(0);
    }

    /// <summary>
    /// 由布局系统调用
    /// 计算垂直layout大小
    /// </summary>
    public override void SetLayoutVertical()
    {
        SetCellsAlongAxis(1);
    }

    private void SetCellsAlongAxis(int axis)
    {
        //根据自己的可用宽度/高度，计算和设置子对象的大小和位置。

        //通常，布局控制器应该只在调用水平轴时设置水平值
        //并且在为垂直轴调用时只设置垂直值。
        //但是，在本例中，我们在调用垂直轴时同时设置水平和垂直位置。
        //因为我们只设置了水平位置而没有设置大小，所以它不应该影响子元素的布局，
        //因此，不应违反所有水平布局必须在所有垂直布局之前计算的规则。
        if (axis == 0)
        {
            return;
        }

        var rectChildrenCount = rectChildren.Count;

        // 仅在调用水平轴时设置大小，不设置位置
        for (int i = 0; i < rectChildrenCount; i++)
        {
            RectTransform rect = rectChildren[i];

            m_Tracker.Add(this, rect,
                DrivenTransformProperties.Anchors |
                DrivenTransformProperties.AnchoredPosition |
                DrivenTransformProperties.SizeDelta);

            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.sizeDelta = cellSize;
            //设置item的自动尺寸
            if (isAutoSize && m_ChildControlWidth)
            {
                int autoAxis = 0;
                float min, preferred, flexible;
                GetChildSizes(rect, autoAxis, true, out min, out preferred, out flexible);
                Vector2 itemSize = rect.sizeDelta;
                itemSize[autoAxis] = preferred;
                rect.sizeDelta = itemSize;
            }

            if (isAutoSize && m_ChildControlHeight)
            {
                int autoAxis = 1;
                float min, preferred, flexible;
                GetChildSizes(rect, autoAxis, true, out min, out preferred, out flexible);
                Vector2 itemSize = rect.sizeDelta;
                itemSize[autoAxis] = preferred;
                rect.sizeDelta = itemSize;
            }
        }

        float width = rectTransform.rect.size.x;
        float height = rectTransform.rect.size.y;

        //计算横向和纵向上可以排布的数量
        int cellCountX = 1;
        int cellCountY = 1;
        if (m_Constraint == Constraint.FixedColumnCount)
        {
            cellCountX = m_ConstraintCount;

            if (rectChildrenCount > cellCountX)
                cellCountY = rectChildrenCount / cellCountX + (rectChildrenCount % cellCountX > 0 ? 1 : 0);
        }
        else if (m_Constraint == Constraint.FixedRowCount)
        {
            cellCountY = m_ConstraintCount;

            if (rectChildrenCount > cellCountY)
                cellCountX = rectChildrenCount / cellCountY + (rectChildrenCount % cellCountY > 0 ? 1 : 0);
        }
        else
        {
            if (cellSize.x + spacing.x <= 0)
                cellCountX = int.MaxValue;
            else
                cellCountX = Mathf.Max(1,
                    Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));

            if (cellSize.y + spacing.y <= 0)
                cellCountY = int.MaxValue;
            else
                cellCountY = Mathf.Max(1,
                    Mathf.FloorToInt((height - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
        }

        //横向和纵向排布的起始方向
        //StartCorner设置的地方
        int cornerX = (int)startCorner % 2;
        int cornerY = (int)startCorner / 2;

        int cellsPerMainAxis, actualCellCountX, actualCellCountY;
        //计算出行或者列上排列的数量
        if (startAxis == Axis.Horizontal)
        {
            cellsPerMainAxis = cellCountX;
            actualCellCountX = Mathf.Clamp(cellCountX, 1, rectChildrenCount);
            actualCellCountY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(rectChildrenCount / (float)cellsPerMainAxis));
        }
        else
        {
            cellsPerMainAxis = cellCountY;
            actualCellCountY = Mathf.Clamp(cellCountY, 1, rectChildrenCount);
            actualCellCountX = Mathf.Clamp(cellCountX, 1, Mathf.CeilToInt(rectChildrenCount / (float)cellsPerMainAxis));
        }

        //根据实际排列数量算出实际需要用到的空间
        Vector2 requiredSpace;
        //根据需求空间和自身大小计算出开始排布子级的横向及纵向偏移，这里是ChildAlignment 设置也就是对齐设置生效的敌方。
        Vector2 startOffset;

        if (isAutoSize)
        {
            float requiredSpaceWidth = (actualCellCountX - 1) * spacing.x;
            float requiredSpaceHeight = (actualCellCountY - 1) * spacing.y;
            float actualCellWidth = actualCellCountX * cellSize.x;
            float actualCellHeight = actualCellCountY * cellSize.y;
            if (m_ChildControlWidth)
            {
                actualCellWidth = 0;
                for (int i = 0; i < actualCellCountX; i++)
                {
                    actualCellWidth += m_ColumnMaxWidthMap[i + 1];
                }
            }

            if (m_ChildControlHeight)
            {
                actualCellHeight = 0;
                for (int i = 0; i < actualCellCountY; i++)
                {
                    actualCellHeight += m_RowMaxHeightMap[i + 1];
                }
            }

            requiredSpace = new Vector2(requiredSpaceWidth + actualCellWidth, requiredSpaceHeight + actualCellHeight);
        }
        else
        {
            requiredSpace = new Vector2(
                actualCellCountX * cellSize.x + (actualCellCountX - 1) * spacing.x,
                actualCellCountY * cellSize.y + (actualCellCountY - 1) * spacing.y
            );
        }

        //ChildAlignment生效的地方
        startOffset = new Vector2(
            GetStartOffset(0, requiredSpace.x),
            GetStartOffset(1, requiredSpace.y)
        );

        for (int i = 0; i < rectChildrenCount; i++)
        {
            RectTransform child = rectChildren[i];
            //根据排布位置、排布数量等数据，设置每个item的实际位置
            Vector2 itemSize;
            Vector2 lastItemSize = child.sizeDelta;
            if (isAutoSize)
            {
                itemSize = child.sizeDelta;
                int itemIndex = i + 1;
                if (m_ChildControlWidth)
                {
                    //算出列数。取最大宽度。
                    int column = itemIndex / actualCellCountY + (itemIndex % actualCellCountY > 0 ? 1 : 0);
                    float maxWidth = m_ColumnMaxWidthMap[column];
                    itemSize[0] = maxWidth;
                    int lastColumn = column - 1 < 1 ? 1 : column - 1;
                    lastItemSize[0] = m_ColumnMaxWidthMap[lastColumn];
                }

                if (m_ChildControlHeight)
                {
                    //算出行数。取最大高度。
                    int row = itemIndex / actualCellCountX + (itemIndex % actualCellCountX > 0 ? 1 : 0);
                    float maxHeight = m_RowMaxHeightMap[row];
                    itemSize[1] = maxHeight;
                    int lastRow = row - 1 < 1 ? 1 : row - 1;
                    lastItemSize[1] = m_RowMaxHeightMap[lastRow];
                }
            }
            else
            {
                itemSize = this.cellSize;
            }

            int positionX;
            int positionY;
            if (startAxis == Axis.Horizontal)
            {
                positionX = i % cellsPerMainAxis;
                positionY = i / cellsPerMainAxis;
            }
            else
            {
                positionX = i / cellsPerMainAxis;
                positionY = i % cellsPerMainAxis;
            }

            if (cornerX == 1)
                positionX = actualCellCountX - 1 - positionX;
            if (cornerY == 1)
                positionY = actualCellCountY - 1 - positionY;
            //算出item的坐标
            float offsetPosX = 0;
            float offsetPosY = 0;
            if (isAutoSize)
            {
                if (m_ChildControlWidth)
                {
                    for (int j = 1; j < positionX + 1; j++)
                    {
                        float offsetX = m_ColumnMaxWidthMap[j];
                        offsetPosX += offsetX;
                        offsetPosX += spacing[0];
                    }
                }

                if (m_ChildControlHeight)
                {
                    for (int j = 1; j < positionY + 1; j++)
                    {
                        float offsetY = m_RowMaxHeightMap[j];
                        offsetPosY += offsetY;
                        offsetPosY += spacing[1];
                    }
                }
            }
            else
            {
                offsetPosX = (cellSize[0] + spacing[0]) * positionX;
                offsetPosY = (cellSize[1] + spacing[1]) * positionY;
            }

            SetChildAlongAxis(child, 0, startOffset.x + offsetPosX, itemSize[0]);
            SetChildAlongAxis(child, 1, startOffset.y + offsetPosY, itemSize[1]);
        }

        //每次rebuild最后执行到的函数，做清除
        m_ColumnMaxWidthMap.Clear();
        m_RowMaxHeightMap.Clear();
    }

    private void GetChildSizes(RectTransform child, int axis, bool controlSize,
        out float min, out float preferred, out float flexible)
    {
        if (!controlSize)
        {
            min = child.sizeDelta[axis];
            preferred = min;
            flexible = 0;
        }
        else
        {
            min = LayoutUtility.GetMinSize(child, axis);
            preferred = LayoutUtility.GetPreferredSize(child, axis);
            flexible = LayoutUtility.GetFlexibleSize(child, axis);
        }
    }
}