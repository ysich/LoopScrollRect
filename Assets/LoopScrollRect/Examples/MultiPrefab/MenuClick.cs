using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuClick : MonoBehaviour
{
    // Start is called before the first frame update
    public bool IsActive = false;
    private int itemDataIndex = -2;
    public InitMultiScrollRect InitMultiScrollRect;
    void Start()
    {
        InitMultiScrollRect = gameObject.GetComponentInParent<InitMultiScrollRect>();
    }

    public void Init(int itemDataIndex,bool IsActive)
    {
        this.itemDataIndex = itemDataIndex;
        gameObject.name = itemDataIndex.ToString();
        this.IsActive = IsActive;
    }

    public void BtnOnClick()
    {
        IsActive = !IsActive;
        if (IsActive)
            InitMultiScrollRect.OnClickIndex = itemDataIndex;
        else
            InitMultiScrollRect.OnClickIndex = -2;
        InitMultiScrollRect.ScrollRectMulti.RefillCells(InitMultiScrollRect.ScrollRectMulti.totalCount);
    }
}
