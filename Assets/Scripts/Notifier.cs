using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Notifier : MonoBehaviour
{
    public static Notifier singleton = null;
    [SerializeField] private GameObject notificationPrefab = null;

    private void Awake()
    {
        if(singleton == null) { singleton = this; }
        else
        {
            Destroy(singleton);
            singleton = this;
        }
    }

    public void Notify(string text)
    {
        GameObject notification = Instantiate(notificationPrefab, transform);
        notification.transform.SetAsFirstSibling(); 
        notification.GetComponent<Text>().text = "[" + Mathf.Round(Time.time) + "s] : " + text;
        this.GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x ,Mathf.Max(300f, transform.childCount * notificationPrefab.GetComponent<RectTransform>().rect.height));
    }
}
