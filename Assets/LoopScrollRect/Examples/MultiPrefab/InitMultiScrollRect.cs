using System;
using System.Collections;
using System.Collections.Generic;
using LoopScrollRect;
using UnityEngine;
using UnityEngine.UI;

public class InitMultiScrollRect : MonoBehaviour
{
    public LoopScrollRectMulti ScrollRectMulti;

    public List<GameObject> createItemList = new List<GameObject>();

    public int OnClickIndex = 0;

    public Dictionary<string, List<GameObject>> itemlistByType = new Dictionary<string, List<GameObject>>();

    public int totalCount = 10;
    private void Awake()
    {
        ScrollRectMulti.SetGetObjTypeByItemIndexHandler(OnGetObjTypeByItemIndexHandler);
        ScrollRectMulti.SetOnCreateItemHandler(OnCreateItemHandler);
        ScrollRectMulti.SetOnFlushItemHandler(OnFlushItemHandler);

        ScrollRectMulti.RefillCells(totalCount);
    }

    private string OnGetObjTypeByItemIndexHandler(int itemDataIndex)
    {
        if (itemDataIndex == OnClickIndex + 1)
        {
            return createItemList[1].name;
        }
        return createItemList[0].name;
    }
    private GameObject OnCreateItemHandler(int itemDataIndex)
    {
        string type = OnGetObjTypeByItemIndexHandler(itemDataIndex);
        if (!itemlistByType.TryGetValue(type, out List<GameObject> itemList))
        {
            itemList = new List<GameObject>();
            itemlistByType.Add(type,itemList);
        }
        //TODO：ysc，这部流程简化
        GameObject targetItem = null;
        foreach (var createItem in createItemList)
        {
            if (createItem.name == type)
            {
                targetItem = createItem;
            }
        }
        //=====
        GameObject item = Instantiate(targetItem);
        itemList.Add(item);
        return item;
    }

    private void OnFlushItemHandler(int itemIndex, int itemDataIndex)
    {
        //这里的itemIndex直接换算成对应type内的itemIndex，在lua层再封装一下直接return item就行了。
        string type = OnGetObjTypeByItemIndexHandler(itemDataIndex);
        GameObject item = itemlistByType[type][itemIndex];
        if (type == "MenuItem")
        {
            MenuClick menuClick = item.GetComponent<MenuClick>();
            bool isActive = OnClickIndex == itemDataIndex;
            menuClick.Init(itemDataIndex,isActive);
        }
        else
        {
            Text text = gameObject.GetComponentInChildren<Text>();
            text.text = OnClickIndex.ToString();
        }
        
    }
}
