using System.Collections;
using System.Collections.Generic;
using LoopScrollRect;
using UnityEngine;
using UnityEngine.UI;

public class controller : MonoBehaviour
{
    public List<GameObject> ItemList = new List<GameObject>();
    public LoopScrollRectBase ScrollRect;
    public int ClickIndex = -1;
    public GameObject CreateItem;
    void Start()
    {
        ScrollRect = this.GetComponent<LoopScrollRectBase>();
        ScrollRect.SetOnFlushItemHandler(OnFlushItem);
        ScrollRect.SetOnCreateItemHandler(OnCreateItem);
    }

    public void OnFlushItem(int itemIndex, int itemDataIndex)
    {
        GameObject item = ItemList[itemIndex];
        bool isActive = itemDataIndex == ClickIndex;
        item.GetComponent<MenuClick>().Init(itemDataIndex,isActive);
        RectTransform rectTransform = item.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(itemDataIndex % 2 == 0 ? 200 : 100,itemDataIndex % 2 == 0 ? 100 : 50) ;
    }

    public GameObject OnCreateItem(int index)
    {
        GameObject item = Instantiate(CreateItem);
        ItemList.Add(item);
        return item;
    }
}
