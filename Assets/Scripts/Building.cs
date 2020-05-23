using UnityEngine;
using static Enums;

public class Building : MonoBehaviour
{
    private ColonyPlayer owner = null;
    [SerializeField] private BuildingType type = BuildingType.Village;
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
    public BuildingType Type { get { return type; } }
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
        if(type != BuildingType.Street)
        {
            return type.ToString()[0] + " @ " + Position.colRow.ToString();
        }
        else
        {
            return "Street";
        }
    }

}
