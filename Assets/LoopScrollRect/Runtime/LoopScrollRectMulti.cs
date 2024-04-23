using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.UI
{
    public abstract class LoopScrollRectMulti : LoopScrollRectBase
    {
        private Dictionary<string,Stack<RectTransform>> m_ObjPoolByType = new Dictionary<string,Stack<RectTransform>>();

        private Dictionary<int, string> m_ObjTypeDict = new Dictionary<int, string>();

        private Dictionary<string, Dictionary<int, int>> m_ObjIndexDictByType =
            new Dictionary<string, Dictionary<int, int>>();

        private Func<int, string> m_GetObjTypeByItemIndexHandler;
        
        protected override int GetItemIndexByRt(RectTransform rectTransform)
        {
            int hashCode = rectTransform.GetHashCode();
            string type = m_ObjTypeDict[hashCode];
            Dictionary<int,int> objIndexDIct = m_ObjIndexDictByType[type];
            int itemIndex = objIndexDIct[hashCode];
            return itemIndex;
        }

        /// <summary>
        /// 设置根据ItemDataIndex获取到预制名的方法
        /// </summary>
        /// <param name="getObjNameByItemIndexHandler"></param>
        public void SetGetObjTypeByItemIndexHandler(Func<int, string> getObjNameByItemIndexHandler)
        {
            m_GetObjTypeByItemIndexHandler = getObjNameByItemIndexHandler;
        }

        private Stack<RectTransform> GetObjPoolByType(string type)
        {
            if (!m_ObjPoolByType.TryGetValue(type, out Stack<RectTransform> pool))
            {
                pool = new Stack<RectTransform>();
                m_ObjPoolByType.Add(type,pool);
            }
            return pool;
        }

        protected override void ProvideData(RectTransform rectTransform, int itemDataIndex)
        {
            try
            {
                int objIndex = GetItemIndexByRt(rectTransform);
                if (m_OnFlushItem != null)
                    m_OnFlushItem(objIndex, itemDataIndex);
            }
            catch (Exception e)
            {
                Debug.LogError($"LoopScrollRectMulti：滑动组件：{e}");
                throw;
            }
        }

        protected override GameObject GetObjectFromPool(int itemDataIndex)
        {
            int itemIndex = GetObjSiblingIndex(itemDataIndex);
            if (m_GetObjTypeByItemIndexHandler == null)
            {
                Debug.LogError("LoopScrollRectMulti：SetGetObjNameByItemIndexHandler还没设置过获取type的函数！");
            }
            string type = m_GetObjTypeByItemIndexHandler.Invoke(itemDataIndex);
            Stack<RectTransform> objPool = GetObjPoolByType(type);
            GameObject item = null;
            if (objPool.Count > 0)
            {
                RectTransform rt = objPool.Pop();
                item = rt.gameObject;
                m_ShowObjs.Insert(itemIndex,item.transform as RectTransform);
                item.SetActive(true);
                rt.SetSiblingIndex(itemIndex);
                return item;
            }
            //TODO:ysc,这里到时候要换成lua的创建代码
            if (m_OnCreateItemHandler == null)
            {
                Debug.LogError("LoopScrollRectMulti：SetOnCreateItemHandler还没设置创建Item函数！");
            }
            
            item = m_OnCreateItemHandler.Invoke(itemDataIndex);
            RectTransform rectTransform = item.transform as RectTransform;
            int objHashCode = rectTransform.GetHashCode();
            if (!m_ObjIndexDictByType.TryGetValue(type, out Dictionary<int, int> objIndexDict))
            {
                objIndexDict = new Dictionary<int, int>();
                m_ObjIndexDictByType.Add(type,objIndexDict);
            }
            objIndexDict.Add(objHashCode,objIndexDict.Count);
            m_ObjTypeDict.Add(objHashCode,type);
            rectTransform.SetParent(m_Content);
            m_ShowObjs.Insert(itemIndex,rectTransform);
            return item;
        }

        protected override void ReturnObjectToPool(RectTransform rectTransform)
        {
            int objHashCode = rectTransform.GetHashCode();
            string type = m_ObjTypeDict[objHashCode];
            if (!m_ObjPoolByType.TryGetValue(type, out Stack<RectTransform> objPool))
            {
                objPool = new Stack<RectTransform>();
                m_ObjPoolByType.Add(type,objPool);
            }
            objPool.Push(rectTransform);
            m_ShowObjs.Remove(rectTransform);
            rectTransform.gameObject.SetActive(false);
        }

        // 多预制不支持TempPool
        protected override RectTransform GetFromTempPool(int itemIdx)
        {
            RectTransform nextItem = GetObjectFromPool(itemIdx).transform as RectTransform;
            ProvideData(nextItem, itemIdx);
            return nextItem;
        }

        protected override void ReturnToTempPool(bool fromStart, int count)
        {
            Debug.Assert(m_ShowObjs.Count >= count);
            if (fromStart)
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    RectTransform childRt = m_ShowObjs[i] as RectTransform;
                    ReturnObjectToPool(childRt);
                }
            }
            else
            {
                int t = m_ShowObjs.Count - count;
                for (int i = m_ShowObjs.Count - 1; i >= t; i--)
                {
                    RectTransform childRt = m_ShowObjs[i] as RectTransform;
                    ReturnObjectToPool(childRt);
                }
            }
        }

        protected override void ClearTempPool()
        {
        }
    }
}