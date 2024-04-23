using System;
using System.Collections;
using System.Collections.Generic;
using LoopScrollRect;
using UnityEngine;
using UnityEngine.UI;

public class InitMenuOption : MonoBehaviour
{
    public LoopScrollRectBase ScrollRect;

    public GameObject CreateItem;
    
    public int OnClickIndex = -1;
    
    public int totalCount = 20;
    private List<GameObject> m_GameObjects = new List<GameObject>();
    private void Awake()
    {
        ScrollRect.SetOnCreateItemHandler(OnCreateItemHandler);
        ScrollRect.SetOnFlushItemHandler(OnFlushItemHandler);
        ScrollRect.RefillCells(totalCount);
    }

    private GameObject OnCreateItemHandler(int itemDataIndex)
    {
        GameObject newItem = Instantiate(CreateItem);
        m_GameObjects.Add(newItem);
        return newItem;
    }

    private void OnFlushItemHandler(int itemIndex, int itemDataIndex)
    {
        GameObject gameObject = m_GameObjects[itemIndex];
        MenuOptionItem menuOptionItem = gameObject.GetComponent<MenuOptionItem>();
        menuOptionItem.Init(itemDataIndex);
        if (itemDataIndex == OnClickIndex)
        {
            //设置展开状态
            menuOptionItem.SetOpenState(itemDataIndex);
        }
        else
        {
            //设置默认状态
            menuOptionItem.SetHideState(itemDataIndex);
        }
    }
}
