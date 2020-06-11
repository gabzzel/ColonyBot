using System.Collections.Generic;
using UnityEngine;
using static Utility;

public class Building : MonoBehaviour
{
    private ColonyPlayer owner = null;
    [SerializeField] private int type = -1;
    private NonTileGridPoint gridPoint;

    public ColonyPlayer Owner
    {
        get { return owner; }
        set
        {
            this.GetComponent<SpriteRenderer>().color = value.color;
            owner = value;
        }
    }
    public int Type { get { return type; } }
    public NonTileGridPoint Position
    {
        get { return gridPoint; }
        set
        {
            if(value == null || value.Building != this) { throw new System.Exception("Cannot set position of building because " + value.ToString() + " is null or already taken."); }
            gridPoint = value;
        }
    }

    public override string ToString()
    {
        if(type != Utility.Street)
        {
            return type.ToString()[0] + " @ " + Position.position.ToString();
        }
        else
        {
            return "Street";
        }
    }

}