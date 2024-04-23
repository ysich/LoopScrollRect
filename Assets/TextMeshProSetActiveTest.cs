using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextMeshProSetActiveTest : MonoBehaviour
{
    public Button Button;
    private TextMeshProUGUI[] TextMeshProUguis;
    public bool isShow;
    
    // Start is called before the first frame update
    void Start()
    {
        Button.onClick.AddListener(OnBtnTest);
        TextMeshProUguis = transform.GetComponentsInChildren<TextMeshProUGUI>();
    }

    private void OnBtnTest()
    {
        TextMeshProUguis = transform.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var item in TextMeshProUguis)
        {
            item.gameObject.SetActive(isShow);
        }
        isShow = !isShow;
    }
}
