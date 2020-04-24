using UnityEngine;

public class Building : MonoBehaviour
{
    public Player owner = null;
    public GridPoint gridPoint = null;
    public bool city = false;
    
    public void Initialize(Player owner, GridPoint gp)
    {
        this.owner = owner;
        this.gridPoint = gp;
        gp.building = this;
    }
}
