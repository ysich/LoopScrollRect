using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuOptionItem : MonoBehaviour
{
    public bool IsActive = false;
    public GameObject OpenItem;
    public InitMenuOption initMenuOption;

    private int itemDataIndex;

    private void Start()
    {
        initMenuOption = gameObject.GetComponentInParent<InitMenuOption>();
    }

    public void Init(int itemDataIndex)
    {
        transform.GetComponentInChildren<TextMeshProUGUI>().text = itemDataIndex.ToString() ;
    }
    
    private void SetNormalState(int itemDataIndex)
    {
        this.itemDataIndex = itemDataIndex;
        gameObject.name = itemDataIndex.ToString();
    }
    public void SetOpenState(int itemDataIndex)
    {
        SetNormalState(itemDataIndex);
        OpenItem.SetActive(true);
    }

    public void SetHideState(int itemDataIndex)
    {
        SetNormalState(itemDataIndex);
        OpenItem.SetActive(false);
    }
    public void BtnOnClick()
    {
        // IsActive = !IsActive;
        // if (IsActive)
        // {
        //     initMenuOption.OnClickIndex = itemDataIndex;
        //     initMenuOption.ScrollRect.UpdateSelectItemIndex(itemDataIndex);
        // }
        // else
        // {
        //     initMenuOption.OnClickIndex = -1;
        //     initMenuOption.ScrollRect.UpdateSelectItemIndex(-1);
        // }
        // initMenuOption.ScrollRect.RefreshCells();
    }
}
