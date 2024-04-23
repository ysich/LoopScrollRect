using System;
using System.Collections;
using System.Collections.Generic;
using Calculate;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CalculateDemo_Expand : MonoBehaviour
{
    public ExpandMultiCalculateScrollRect calculateScrollRect;
    public Button btnRefresh;

    public Transform content;
    public GameObject instanceItemObj;
    public GameObject instanceExpandObj;

    private List<RectTransform> m_BtnList = new List<RectTransform>();
    private List<RectTransform> m_ExpandList = new List<RectTransform>();

    private List<CalculateGroupData> m_RefreshData = new List<CalculateGroupData>()
    {
        new CalculateGroupData(){itemCount = 5,isExpand = true},
        new CalculateGroupData(){itemCount = 4,isExpand = false},
        new CalculateGroupData(){itemCount = 3,isExpand = true},
        new CalculateGroupData(){itemCount = 2,isExpand = false},
        new CalculateGroupData(){itemCount = 1,isExpand = true},
        new CalculateGroupData(){itemCount = 2,isExpand = false},
        new CalculateGroupData(){itemCount = 3,isExpand = true},
    };
    private void Start()
    {
        calculateScrollRect.SetOnCreateItemHandler(OnCreateItemHandler);
        calculateScrollRect.SetOnCreateExpandItemHandler(OnCreateExpandItemHandler);
        calculateScrollRect.SetOnFlushItemHandler(OnFlushItemHandler);
        calculateScrollRect.SetOnClickItemHandler(OnFlushItemHandler);

        btnRefresh.onClick.AddListener(OnBtnRefresh);
    }

    private RectTransform OnCreateItemHandler()
    {
        GameObject nextItemObj = Instantiate(instanceItemObj, content);
        RectTransform rectTransform = nextItemObj.transform as RectTransform;
        m_BtnList.Add(rectTransform);
        return rectTransform;
    }

    private RectTransform OnCreateExpandItemHandler()
    {
        GameObject nextItemObj = Instantiate(instanceExpandObj, content);
        RectTransform rectTransform = nextItemObj.transform as RectTransform;
        m_ExpandList.Add(rectTransform);
        return rectTransform;
    }
    
    private void OnFlushItemHandler(int objIndex, int dataIndex)
    {
        RectTransform item = m_BtnList[objIndex];
        bool isSelected = calculateScrollRect.IsSelected(dataIndex);
        
        Transform tag = item.Find("Tag");
        Image image = tag.GetComponent<Image>();
        image.color = isSelected ? Color.green : Color.red;
    }

    private void OnBtnRefresh()
    {
        calculateScrollRect.RefillCells(m_RefreshData);   
    }
}
