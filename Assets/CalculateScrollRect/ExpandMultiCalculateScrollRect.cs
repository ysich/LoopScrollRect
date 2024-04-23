/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2024-03-25 14:20:11
-- 概述: 只支持单行单列，点击展开item列表
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Calculate.Interface;
using LoopScrollRect.Core;
using Unity.VisualScripting;
using UnityEngine;

namespace Calculate
{
    public class ExpandMultiCalculateScrollRect : LoopListBase,ISingleClick,IMultClick
    {
        public float btnSize;
        public float expandSize;
        public float spacing;
        private float btnDimension => btnSize + spacing;
        private float expandDimension => expandSize + spacing;
        
        //重新创建的适合从这里取
        private Stack<RectTransform> m_BtnPool = new Stack<RectTransform>();
        private List<RectTransform> m_BtnShowObjs = new List<RectTransform>();
        /// <summary>
        /// BtnItem之后的坐标，第一个ExpandItem开始的坐标
        /// </summary>
        private List<float> m_GroupStartPosList = new List<float>();

        private List<RectTransform> m_ShowObjs = new List<RectTransform>();
        private Dictionary<int, int> m_ExpandDataIndexDict = new Dictionary<int, int>();
        private Stack<RectTransform> m_ExpandObjPool = new Stack<RectTransform>();
        private Dictionary<int, int> m_ExpandObjIndexDict = new Dictionary<int, int>();

        /// <summary>
        /// 记录每组展开的数量，btnIndex，ExpandCount
        /// </summary>
        private Dictionary<int,int> m_ExpandGroupNumMap = new Dictionary<int, int>();

        private int m_GroupIndexStart;
        private int m_GroupIndexEnd;

        private int m_GroupStartItemDataIndex;
        private int m_GroupEndItemDataIndex;

        /// <summary>
        /// 开头结束的位置。可以理解为下一个Item的Pos，算上最近的BtnDimension
        /// </summary>
        private float m_ItemEndPosByEnd;
        /// <summary>
        /// 尾巴结束的位置。可以理解为下一个Item的Pos，算上最近的BtnDimension
        /// </summary>
        private float m_ItemEndPosByStart;
        
        /// <summary>
        /// 开头开始的位置。可以理解为排除最近的BtnDimension。
        /// </summary>
        private float m_ItemStartPosByStart;
        /// <summary>
        /// 开头结束的位置。可以理解为排除最近的BtnDimension。
        /// </summary>
        private float m_ItemStartPosByEnd;
        
        #region override

        protected override void OnScrollRectValueChanged(Vector2 pos)
        {
            UpdateItems();
        }

        protected override int GetObjIndexByRt(RectTransform objRT)
        {
            for (int i = 0; i < m_BtnShowObjs.Count; i++)
            {
                var rt = m_BtnShowObjs[i];
                if (rt.Equals(objRT))
                {
                    return i;
                }
            }

            return 0;
        }
        
        protected override int GetDataIndexByObjIndex(RectTransform objRT)
        {
            return GetObjIndexByRt(objRT);
        }

        protected int GetExpandObjIndexByRt(RectTransform objRt)
        {
            int index = m_ExpandObjIndexDict[objRt.GetHashCode()];
            return index;
        }

        public void OnSingleClick(int oldSelectBtnIndex, int selectBtnIndex)
        {
            if (oldSelectBtnIndex == selectBtnIndex)
            {
                CloseExpand(selectBtnIndex);
                return;
            }
            OpenExpand(selectBtnIndex);
        }

        public void OnMultClick(int selectBtnIndex, bool isSelected)
        {
            if (isSelected)
            {
                OpenExpand(selectBtnIndex);
                return;
            }
            CloseExpand(selectBtnIndex);
        }

        #endregion

        #region CreateExpand

        private Func<RectTransform> m_OnCreateExpandItemHandler;

        public void SetOnCreateExpandItemHandler(Func<RectTransform> handler)
        {
            m_OnCreateExpandItemHandler = handler;
        }

        private RectTransform CreateExpandItem()
        {
            RectTransform rectTransform = m_OnCreateExpandItemHandler?.Invoke();
            return rectTransform;
        }

        private void ProvideExpandData(RectTransform itemRt,int groupIndex,int dataIndex)
        {
            int objIndex = GetExpandObjIndexByRt(itemRt);
        }

        #endregion

        private int GetExpandCount(int groupIndex, int itemDataIndex)
        {
            int count = itemDataIndex;
            for (int i = 0; i < groupIndex; i++)
            {
                if (IsSelected(i))
                {
                    int groupCount =  m_ExpandGroupNumMap[i];
                    count += groupCount;
                }
            }

            return count;
        }

        private RectTransform CreateExpandItem(int groupIndex,int itemDataIndex)
        {
            RectTransform nextRt= GetFormExpandPool(itemDataIndex);
            m_ExpandDataIndexDict[nextRt.GetHashCode()] = itemDataIndex;
            int curExpandCount = GetExpandCount(groupIndex, itemDataIndex);
            int startExpandCount = GetExpandCount(Mathf.Max(0,m_GroupIndexStart),Mathf.Max(0,m_GroupStartItemDataIndex));
            int itemIndex = Mathf.Max(0, curExpandCount - startExpandCount);
            Debug.Log($"CreateExpandItem-itemIndex:{itemIndex},curGroup,itemDataIndex:{groupIndex}{itemDataIndex};" +
                      $"startGroup,startDataIndex:{m_GroupIndexStart}-{m_GroupStartItemDataIndex}");
            m_ShowObjs.Insert(itemIndex, nextRt);
            nextRt.SetSiblingIndex(itemIndex);
            SetItemAnchoredPosition(nextRt, groupIndex, itemDataIndex);
            ProvideExpandData(nextRt,groupIndex,itemDataIndex);
            return nextRt;
        }
        
        private RectTransform GetFormExpandPool(int itemDataIndex)
        {
            RectTransform nextItem;
            if (m_ExpandObjPool.Count > 0)
            {
                nextItem = m_ExpandObjPool.Pop();
                nextItem.gameObject.SetActive(true);
                return nextItem;
            }
            nextItem = CreateExpandItem();
            m_ExpandObjIndexDict[nextItem.GetHashCode()] = m_ExpandObjIndexDict.Count;
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

        private RectTransform GetFormBtnPool()
        {
            RectTransform nextItem;
            int btnIndex = m_BtnShowObjs.Count;
            if (m_BtnPool.Count > 0)
            {
                nextItem = m_BtnPool.Pop();
                nextItem.gameObject.SetActive(true);
                m_BtnShowObjs.Add(nextItem);
                ProvideData(nextItem,btnIndex);
                SetBtnAnchoredPosition(nextItem,btnIndex);
                return nextItem;
            }
            nextItem = CreateNewItem();
            m_BtnShowObjs.Add(nextItem);
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
            ProvideData(nextItem,btnIndex);
            SetBtnAnchoredPosition(nextItem,btnIndex);
            return nextItem;
        }

        private void ReturnBtnObjectToPool(RectTransform rectTransform)
        {
            m_BtnShowObjs.Remove(rectTransform);
            rectTransform.gameObject.SetActive(false);
            rectTransform.SetAsFirstSibling();
            m_BtnPool.Push(rectTransform);
        }

        private void ReturnAllBtnObjectToPool()
        {
            for (int i = m_BtnShowObjs.Count-1; i >= 0; i--)
            {
                var rt = m_BtnShowObjs[i];
                ReturnBtnObjectToPool(rt);
            }
            m_GroupStartPosList.Clear();
        }
        
        private void ReturnExpandObjectToPool(RectTransform rectTransform)
        {
            m_ShowObjs.Remove(rectTransform);
            rectTransform.gameObject.SetActive(false);
            m_ExpandObjPool.Push(rectTransform);
        }

        private void ReturnExpandObjectToTempPool(bool fromStart, int count = 1)
        {
            Debug.Assert(m_ShowObjs.Count >= count);
            if (m_ShowObjs.Count < count)
            {
                return;
            }
            if (fromStart)
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    RectTransform childRt = m_ShowObjs[i];
                    ReturnExpandObjectToPool(childRt);
                }
            }
            else
            {
                int t = m_ShowObjs.Count - count;
                for (int i = m_ShowObjs.Count - 1; i >= t; i--)
                {
                    RectTransform childRt = m_ShowObjs[i];
                    ReturnExpandObjectToPool(childRt);
                }
            }
        }

        private void UpdateItems()
        {
            Vector2 contentPos = content.anchoredPosition;
            if (m_DirectionType == LoopScrollRectDirectionType.Vertical)
            {
                UpdateItemsByContent(contentPos.y, viewportSizeDelta);
            }
            else
            {
                UpdateItemsByContent(-contentPos.x, viewportSizeDelta);
            }
        }

        private void UpdateItemsByContent(float contentStartPos, float viewSize)
        {
            float contentEndPos = contentStartPos + viewSize;
            if (m_ItemEndPosByEnd < contentEndPos && m_GroupIndexEnd < m_ExpandGroupNumMap.Count)
            {
                while (m_ItemEndPosByEnd < contentEndPos && m_GroupIndexEnd < m_ExpandGroupNumMap.Count)
                {
                    NewItemAtEnd();
                }
            }
            
            if (m_ItemStartPosByStart  > contentStartPos && contentStartPos >=0)
            {
                while (m_ItemStartPosByStart  > contentStartPos  && contentStartPos >=0 )
                {
                    NewItemAtStart();
                }
            }

            // if (m_ItemStartPosByEnd > contentEndPos  && contentEndPos < contentSizeDelta)
            // {
            //     while (m_ItemStartPosByEnd > contentEndPos && contentEndPos < contentSizeDelta)
            //     {
            //         DeleteItemAtEnd();
            //     }
            // }
            
            if (contentStartPos - m_ItemStartPosByStart >= expandDimension && contentStartPos >=0)
            {
                while (contentStartPos - m_ItemEndPosByStart >= expandDimension && contentStartPos >=0)
                {
                    DeleteItemAtStart();
                }
            }
        }

        private void NewItemAtEnd()
        {
            if (m_GroupIndexEnd >= m_ExpandGroupNumMap.Count)
            {
                return ;
            }

            bool isExpand = IsSelected(m_GroupIndexEnd);
            int groupCount = isExpand ? m_ExpandGroupNumMap[m_GroupIndexEnd] : 0;
            if (m_GroupEndItemDataIndex >= groupCount)
            {
                m_GroupIndexEnd++;
                //找到展开的
                while (!IsSelected(m_GroupIndexEnd) && m_GroupIndexEnd < m_ExpandGroupNumMap.Count)
                {
                    //没展开继续找
                    m_GroupIndexEnd++;
                }
                //这里可能会有都不展开的情况，上面的while就会使GroupIndexEnd超界
                if (m_GroupIndexEnd >= m_ExpandGroupNumMap.Count)
                {
                    int endGroupIndex = m_ExpandGroupNumMap.Count - 1;
                    int endGroupItemIndex = IsSelected(endGroupIndex)? m_ExpandGroupNumMap[endGroupIndex] - 1 : 0;
                    m_ItemEndPosByEnd = GetExpandItemPos(endGroupIndex,endGroupItemIndex);
                    m_ItemStartPosByEnd = m_ItemEndPosByEnd;
                    return;
                }
                m_GroupEndItemDataIndex = 0;
                m_ItemEndPosByEnd = GetExpandItemPos(m_GroupIndexEnd,m_GroupEndItemDataIndex);
                m_ItemStartPosByEnd = m_ItemEndPosByEnd - btnDimension;
            }
            RectTransform nextItem = CreateExpandItem(m_GroupIndexEnd,m_GroupEndItemDataIndex);
            Debug.Log($"NewItemAtEnd--GroupIndexEnd{m_GroupIndexEnd},GroupEndItemDataIndex{m_GroupEndItemDataIndex}");
            m_ItemEndPosByEnd += expandDimension;
            m_ItemStartPosByEnd = m_ItemEndPosByEnd;
            m_GroupEndItemDataIndex++;
        }
        
        private void NewItemAtStart()
        {
            if (m_GroupIndexStart < 0)
            {
                return;
            }
            
            m_GroupStartItemDataIndex--;
            if (m_GroupStartItemDataIndex < 0)
            {
                m_GroupIndexStart--;
                //找到展开的
                while (!IsSelected(m_GroupIndexStart) && m_GroupIndexStart >= 0)
                {
                    //没展开的继续找
                    m_GroupIndexStart--;
                }
                //这里会存在都不展开的情况
                if (m_GroupIndexStart < 0)
                {
                    m_GroupIndexStart = 0;
                    m_GroupStartItemDataIndex = 0;
                    m_ItemEndPosByStart = GetExpandItemPos(0, 0);
                    m_ItemStartPosByStart = 0;
                    return;
                }
                m_GroupStartItemDataIndex = m_ExpandGroupNumMap[m_GroupIndexStart] - 1;
                m_ItemEndPosByStart = GetExpandItemPos(m_GroupIndexStart, m_ExpandGroupNumMap[m_GroupIndexStart]);
                m_ItemStartPosByStart = m_ItemEndPosByStart;
            }
            RectTransform nextItem = CreateExpandItem(m_GroupIndexStart,m_GroupStartItemDataIndex);
            Debug.Log($"NewItemAtStart--GroupIndexStart{m_GroupIndexStart},GroupStartItemDataIndex{m_GroupStartItemDataIndex}");
            m_ItemEndPosByStart -= expandDimension;
            if (m_GroupStartItemDataIndex == 0)
                m_ItemStartPosByStart -= btnDimension;
            else
                m_ItemStartPosByStart -= expandDimension;
        }
        
        private void DeleteItemAtEnd()
        {
            if (m_GroupEndItemDataIndex < 0)
            {
                m_GroupIndexEnd--;
                while (!IsSelected(m_GroupIndexEnd) && m_GroupIndexEnd >= 0)
                {
                    m_GroupIndexEnd--;
                }

                if (m_GroupIndexEnd < 0)
                {
                    m_GroupIndexEnd = 0;
                    m_GroupEndItemDataIndex = 0;
                    m_ItemEndPosByEnd = GetExpandItemPos(0, 0);
                    m_ItemStartPosByEnd = 0;
                    return;
                }
                //todo:???这里如果都没展开应该就不需要创建了
                m_GroupEndItemDataIndex = m_ExpandGroupNumMap[m_GroupIndexEnd] - 1;
                m_ItemEndPosByEnd = GetExpandItemPos(m_GroupIndexEnd, m_ExpandGroupNumMap[m_GroupIndexEnd]);
                m_ItemStartPosByEnd = m_ItemEndPosByEnd;
            }
            ReturnExpandObjectToTempPool(false);
            m_ItemEndPosByEnd -= expandDimension;
            if (m_GroupEndItemDataIndex == 0)
                m_ItemStartPosByEnd = m_ItemEndPosByEnd - btnDimension;
            else
                m_ItemStartPosByEnd = m_ItemEndPosByEnd;
            Debug.Log($"DeleteItemAtEnd--GroupIndexEnd{m_GroupIndexEnd},GroupEndItemDataIndex{m_GroupEndItemDataIndex}");
            m_GroupEndItemDataIndex--;
        }
        
        private void DeleteItemAtStart()
        {
            bool isExpand = IsSelected(m_GroupIndexStart);
            int groupCount = isExpand ? m_ExpandGroupNumMap[m_GroupIndexStart] : 0;
            //换group
            if (m_GroupStartItemDataIndex >= groupCount - 1)
            {
                m_GroupIndexStart++;
                while (!IsSelected(m_GroupIndexStart) && m_GroupIndexStart < m_ExpandGroupNumMap.Count)
                {
                    m_GroupIndexStart++;
                }
                if (m_GroupIndexStart >= m_ExpandGroupNumMap.Count)
                {
                    int endGroupIndex = m_ExpandGroupNumMap.Count - 1;
                    if (IsSelected(endGroupIndex))
                    {
                        m_ItemEndPosByStart = GetExpandItemPos(endGroupIndex , m_ExpandGroupNumMap[endGroupIndex]);
                        m_ItemStartPosByStart = m_ItemEndPosByStart;
                        return;
                    }
                    m_ItemEndPosByStart = GetExpandItemPos(endGroupIndex ,0);
                    m_ItemStartPosByStart = m_ItemEndPosByStart - btnDimension;
                    return;
                }
                //todo:???这里如果都没展开应该就不需要创建了
                m_GroupStartItemDataIndex = 0;
                m_ItemEndPosByStart = GetExpandItemPos(m_GroupIndexStart, m_GroupStartItemDataIndex);
                m_ItemStartPosByStart = m_ItemEndPosByStart - btnDimension;
            }
            else
            {
                m_GroupStartItemDataIndex++;
                m_ItemEndPosByStart += expandDimension;
                m_ItemStartPosByStart = m_ItemEndPosByStart;
            }
            ReturnExpandObjectToTempPool(true);
            Debug.Log($"DeleteItemAtStart--GroupIndexStart{m_GroupIndexStart},GroupStartItemDataIndex{m_GroupStartItemDataIndex}");
        }
        
        private void SetBtnAnchoredPosition(RectTransform btnItem,int btnIndex)
        {
            float pos = 0;
            for (int i = 1; i <= btnIndex; i++)
            {
                int lastBtnIndex = i - 1;
                int lastExpandCount = m_ExpandGroupNumMap[lastBtnIndex];
                pos += btnDimension;
                //判断是否展开,加上展开item后的位置
                if (IsSelected(lastBtnIndex))
                {
                    float lastExpandSize = lastExpandCount * expandDimension;
                    pos += lastExpandSize;
                }
            }
            
            float groupStartPos = pos + btnDimension;
            if (btnIndex < m_GroupStartPosList.Count)
            {
                m_GroupStartPosList[btnIndex] = groupStartPos;
            }
            else
            {
                m_GroupStartPosList.Add(groupStartPos);
            }

            Vector2 itemPos;
            if (m_DirectionType == LoopScrollRectDirectionType.Horizontal)
            {
                itemPos = new Vector2(pos,0);
            }
            else
            {
                itemPos = new Vector2(0, -pos);
            }
            btnItem.anchoredPosition = itemPos;
            Debug.Log($"SetBtnAnchoredPosition-btnIndex:{btnIndex}");
        }

        private void RefreshBtnPosAfterBtnIndex(int btnDataIndex)
        {
            //当前这个不刷，刷这个往后的
            btnDataIndex += 1;
            for (int i = btnDataIndex; i < m_ExpandGroupNumMap.Count; i++)
            {
                RectTransform btnItem = m_BtnShowObjs[i];
                SetBtnAnchoredPosition(btnItem,i);
            }
        }

        private float GetExpandItemPos(int groupIndex, int itemIndex)
        {
            if (groupIndex > m_GroupStartPosList.Count - 1 || groupIndex<0)
            {
                return 0;
            }
            float pos =  m_GroupStartPosList[groupIndex];
            pos += itemIndex * expandDimension;
            return pos;
        }

        private void SetItemAnchoredPosition(RectTransform nextItem,int groupIndex,int itemIndex)
        {
            float pos = GetExpandItemPos(groupIndex,itemIndex);
            Vector2 itemPos;
            if (m_DirectionType == LoopScrollRectDirectionType.Horizontal)
            {
                itemPos = new Vector2(pos,0);
            }
            else
            {
                itemPos = new Vector2(0, -pos);
            }

            nextItem.anchoredPosition = itemPos;
        }

        #region 对外接口

        private void ClearCellsData()
        {
            m_ExpandGroupNumMap.Clear();
        }

        public void RefillCells(List<CalculateGroupData> calculateGroupData)
        {
            if (!Application.isPlaying)
                return;

            ReturnExpandObjectToTempPool(false,m_ShowObjs.Count);
            //先回收再清缓存，因为回收依赖缓存。id会发生变动
            ReturnAllBtnObjectToPool();
            ClearCellsData();
            float newTotalSize = 0;
            int firstSelectedGroupIndex = -1;
            for (int i = 0; i < calculateGroupData.Count; i++)
            {
                CalculateGroupData group = calculateGroupData[i];
                newTotalSize += btnDimension;
                if (group.isExpand)
                {
                    if (firstSelectedGroupIndex == -1)
                    {
                        firstSelectedGroupIndex = i;
                    }
                    float groupSize = group.itemCount * expandDimension;
                    newTotalSize += groupSize;
                    //把item加入到展开列表里
                    selectedIndexMap.Add(i);
                }
                //btn
                m_ExpandGroupNumMap.Add(i,group.itemCount);
                RectTransform btnItem = GetFormBtnPool();
            }

            selectedIndex = firstSelectedGroupIndex;
            
            m_GroupIndexStart = firstSelectedGroupIndex;
            m_GroupIndexEnd = m_GroupIndexStart;
            m_GroupStartItemDataIndex = 0;
            m_GroupEndItemDataIndex = m_GroupStartItemDataIndex;
            
            m_ItemEndPosByStart = GetExpandItemPos(firstSelectedGroupIndex, 0);
            m_ItemEndPosByEnd = m_ItemEndPosByStart ;
            m_ItemStartPosByStart = 0;
            m_ItemStartPosByEnd = m_ItemStartPosByStart;
            
            UpdateItems();
            
            //与旧的尺寸不相同那么进行修改
            if (MathF.Abs(newTotalSize - contentSizeDelta) > 0.01f)
            {
                contentSizeDelta = newTotalSize;
            }
            
        }

        /// <summary>
        /// 原地刷新
        /// </summary>
        public void RefreshCells()
        {
            ReturnExpandObjectToTempPool(false,m_ShowObjs.Count);
            m_ItemEndPosByEnd = m_ItemEndPosByStart;
            UpdateItems();
        }

        #endregion

        #region Expand

        private void OpenExpand(int btnIndex)
        {
            Debug.Log($"OpenExpand-btnIndex:{btnIndex}");
            scrollRect.StopMovement();
            int expandCount = m_ExpandGroupNumMap[btnIndex];
            float addExpandSize = expandCount * expandDimension;
            contentSizeDelta += addExpandSize;
            
            RefreshGroupIndexByExpand();
            
            RefreshBtnPosAfterBtnIndex(btnIndex);
            RefreshCells();
        }

        private void CloseExpand(int btnIndex)
        {
            Debug.Log($"CloseExpand-btnIndex:{btnIndex}");
            scrollRect.StopMovement();
            int expandCount = m_ExpandGroupNumMap[btnIndex];
            float removeExpandSize = expandCount * expandDimension;
            contentSizeDelta -= removeExpandSize;

            RefreshGroupIndexByExpand();
            
            RefreshBtnPosAfterBtnIndex(btnIndex);
            RefreshCells();
        }

        /// <summary>
        /// 往前找到展开的GroupIndex，如果没有就为-1，表示不进行DeleteStart回收
        /// </summary>
        private void RefreshGroupIndexByExpand()
        {
            int groupStartIndex = 0;
            for (int i = m_GroupIndexStart; i >=0 ; i--)
            {
                if (IsSelected(i))
                {
                    groupStartIndex = i;
                    break;
                }
            }

            if (groupStartIndex != m_GroupIndexStart)
            {
                m_GroupStartItemDataIndex = 0;
            }
            m_GroupEndItemDataIndex = m_GroupStartItemDataIndex;
            m_GroupIndexStart = groupStartIndex;
            m_GroupIndexEnd = m_GroupIndexStart;
            
            Debug.Log($"RefreshGroupIndexByExpand---groupStart:{m_GroupIndexStart},groupEndDataIndex{m_GroupEndItemDataIndex}");
        }
        

        #endregion
    }
}