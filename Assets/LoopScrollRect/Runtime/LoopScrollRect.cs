using System;
using System.Collections.Generic;

namespace UnityEngine.UI
{
    public abstract class LoopScrollRect : LoopScrollRectBase
    {
        /// <summary>
        /// item对象池
        /// </summary>
        //TODO:ysc,后面把生命周期加上用ObjectPool管理
        private Stack<RectTransform> m_ObjPool = new Stack<RectTransform>();
        
        /// <summary>
        /// 缓存Item的itemIndex，key为RectTransform的HashCode
        /// </summary>
        protected Dictionary<int, int> m_ObjIndexDict = new Dictionary<int, int>();

        protected override int GetItemIndexByRt(RectTransform rectTransform)
        {
            int itemIndex = m_ObjIndexDict[rectTransform.GetHashCode()];
            return itemIndex;
        }

        protected override void ProvideData(RectTransform ItemTransform, int itemDataIndex)
        {
            try
            {
                int objIndex = GetItemIndexByRt(ItemTransform);
                if (m_OnFlushItem != null)
                    m_OnFlushItem(objIndex, itemDataIndex);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    string.Format("LoopScrollRect：滑动组件：{0}", e));
                throw;
            }
        }

        /// <summary>
        /// 从对象池中取出
        /// </summary>
        /// <param name="itemDataIndex"></param>
        protected override GameObject GetObjectFromPool(int itemDataIndex)
        {
            GameObject item = null;
            int itemIndex = GetObjSiblingIndex(itemDataIndex);
            if (m_ObjPool.Count > 0)
            {
                RectTransform rt = m_ObjPool.Pop();
                item = rt.gameObject;
                m_ShowObjs.Insert(itemIndex,item.transform as RectTransform);
                item.SetActive(true);
                return item;
            }
            //TODO:ysc,这里到时候要换成lua的创建代码
            if (m_OnCreateItemHandler == null)
            {
                Debug.LogError("LoopScrollRect：SetOnCreateItemHandler还没设置创建Item函数！");
            }
            
            item = m_OnCreateItemHandler.Invoke(itemDataIndex);
            RectTransform rectTransform = item.transform as RectTransform;
            m_ObjIndexDict.Add(rectTransform.GetHashCode(),m_ObjIndexDict.Count);
            rectTransform.SetParent(m_Content);
            m_ShowObjs.Insert(itemIndex,rectTransform);
            return item;
        }

        /// <summary>
        /// 返回到对象池中
        /// </summary>
        /// <param name="rectTransform"></param>
        protected override void ReturnObjectToPool(RectTransform rectTransform)
        {
            m_ObjPool.Push(rectTransform);
            m_ShowObjs.Remove(rectTransform);
            rectTransform.gameObject.SetActive(false);
            rectTransform.SetAsFirstSibling();
        }

        protected override RectTransform GetFromTempPool(int itemDataIndex)
        {
            RectTransform nextItem = null;
            if (deletedItemTypeStart > 0)
            {
                deletedItemTypeStart--;
                nextItem = m_ShowObjs[deletedItemTypeStart];
            }
            else if (deletedItemTypeEnd > 0)
            {
                nextItem = m_ShowObjs[m_ShowObjs.Count - deletedItemTypeEnd];
                deletedItemTypeEnd--;
            }
            else
            {
                nextItem = GetObjectFromPool(itemDataIndex).transform as RectTransform;
            }
            int itemSiblingIndex = Mathf.Max(0, itemDataIndex - m_ItemDataIndexStart + m_ObjPool.Count);
            nextItem.SetSiblingIndex(itemSiblingIndex);
            ProvideData(nextItem, itemDataIndex);
            return nextItem;
        }

        protected override void ReturnToTempPool(bool fromStart, int count)
        {
            if (fromStart)
                deletedItemTypeStart += count;
            else
                deletedItemTypeEnd += count;
        }

        protected override void ClearTempPool()
        {
            Debug.Assert(m_ShowObjs.Count >= deletedItemTypeStart + deletedItemTypeEnd);
            if (deletedItemTypeStart > 0)
            {
                for (int i = deletedItemTypeStart - 1; i >= 0; i--)
                {
                    RectTransform childRt = m_ShowObjs[i] as RectTransform;
                    ReturnObjectToPool(childRt);
                }
                deletedItemTypeStart = 0;
            }
            if (deletedItemTypeEnd > 0)
            {
                int t = m_ShowObjs.Count - deletedItemTypeEnd;
                for (int i = m_ShowObjs.Count - 1; i >= t; i--)
                {
                    RectTransform childRt = m_ShowObjs[i] as RectTransform;
                    ReturnObjectToPool(childRt);
                }
                deletedItemTypeEnd = 0;
            }
        }
    }
}