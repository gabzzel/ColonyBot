using UnityEngine;
using static Enums;

public class Building : MonoBehaviour
{
    private ColonyPlayer owner = null;
    public BuildingType type = BuildingType.Village;

    public ColonyPlayer Owner
    {
        get { return owner; }
        set
        {
            this.GetComponent<SpriteRenderer>().color = value.color;
            owner = value;
        }
    }
}
