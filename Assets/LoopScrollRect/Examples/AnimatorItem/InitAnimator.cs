using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InitAnimator : MonoBehaviour
{
    public LoopScrollRectBase ScrollRect;

    public GameObject CreateItem;
    
    public int totalCount = 30;
    
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
        gameObject.name = itemDataIndex.ToString();
    }
}
