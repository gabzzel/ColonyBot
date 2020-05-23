using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TradeManager : MonoBehaviour
{
    public static TradeManager singleton = null;
    public static List<Trader> traders = new List<Trader>();

    private void Awake()
    {
        if(singleton == null) { singleton = this; }
        else { Destroy(this); }
    }
}
