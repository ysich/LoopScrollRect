/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2024-03-25 14:34:53
-- 概述:
---------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Calculate.Demo
{
    public class CalculateDemo_ExpandTips:MonoBehaviour
    {
        public ExpandTipsCalculateScrollRect calculateScrollRect;
        public int totalCount = 20;
        public Button btnRefresh;

        public Transform content;
        public GameObject instanceItemObj;
        public GameObject instanceTipsObj;

        private List<RectTransform> m_ItemList = new List<RectTransform>();
        private RectTransform tipsItem;
        
        public void Start()
        {
            calculateScrollRect.SetOnCreateItemHandler(CreateItem);
            calculateScrollRect.SetOnFlushItemHandler(OnFlushItemHandler);

            //选中
            calculateScrollRect.SetOnClickItemHandler(OnClickItemHandler);
            //创建tips
            calculateScrollRect.SetOnCreateExpandTipsHandler(OnCreateTipsHandler);

            btnRefresh.onClick.AddListener(RefreshScrollRect);
        }

        private void RefreshScrollRect()
        {
            calculateScrollRect.RefillCells(totalCount);
            // totalCount++;
        }

        private RectTransform OnCreateTipsHandler()
        {
            GameObject tips = Instantiate(instanceTipsObj, content);
            RectTransform rectTransform = tips.transform as RectTransform;
            tipsItem = rectTransform;
            return rectTransform;
        }

        private RectTransform CreateItem()
        {
            GameObject nextItemObj = Instantiate(instanceItemObj, content);
            RectTransform rectTransform = nextItemObj.transform as RectTransform;
            m_ItemList.Add(rectTransform);
            return rectTransform;
        }

        private void OnFlushItemHandler(int objIndex, int dataIndex)
        {
            RectTransform item = m_ItemList[objIndex];
            item.gameObject.name = dataIndex.ToString();
        }

        private void OnClickItemHandler(int objIndex,int dataIndex)
        {
            Debug.Log($"ClickItem:objIndex-{objIndex},dataIndex-{dataIndex}");
        }
    }
}