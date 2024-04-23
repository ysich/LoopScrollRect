using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Calculate;
using UnityEngine.UI;

public class CalculateDemo_Normal : MonoBehaviour
{
   public CalculateScrollRect calculateScrollRect;
   public int totalCount = 20;
   public Button btnRefresh;
   public Button btnScrollTo;

   public Transform content;
   public GameObject instanceItemObj;

   private List<RectTransform> m_ItemList = new List<RectTransform>();

   public int scrollToIndex = 0;

   public void Start()
   {
      calculateScrollRect.SetOnCreateItemHandler(CreateItem);
      calculateScrollRect.SetOnFlushItemHandler(OnFlushItemHandler);

      //单选
      calculateScrollRect.SetOnClickItemHandler(OnClickItemHandler);

      btnRefresh.onClick.AddListener(RefreshScrollRect);
      btnScrollTo.onClick.AddListener(OnScrollToIndex);
   }

   private void RefreshScrollRect()
   {
      calculateScrollRect.RefillCells(totalCount);
      // totalCount++;
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


   private void OnScrollToIndex()
   {
      calculateScrollRect.ScrollToCell(scrollToIndex,1000);
   }
}
