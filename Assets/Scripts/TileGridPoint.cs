using UnityEngine;

public class TileGridPoint : GridPoint
{
    private Tile tile = null;
    private bool robber = false;

    public TileGridPoint(Vector2 position, Vector2Int colRow)
    {
        this.position = position;
        this.colRow = colRow;
    }

    /* Properties */
    public bool Robber
    {
        get { return robber; }
        set
        {
            if (BoardController.singleton.RobberLocation == this && !value)
            {
                throw new System.Exception("Cannot remove robber when we are the robber location!");
            }
            else
            {
                robber = value;
            }
        }
    }
    public Tile Tile
    {
        get { return tile; }
        set { if(tile == null) { tile = value; } }
    }
    public int Resource
    {
        get {

            if (tile != null) { return tile.Resource; }
            else { throw new System.Exception("No tile associated with this GridPoint"); }
        }
    }
    public float Value { get { return tile.Value; } }

    public bool IsNeighbour(TileGridPoint t)
    {
        foreach(int ntgpIndex in connectedNTGPs)
        {
            NonTileGridPoint ntgp = (NonTileGridPoint)BoardController.singleton.allGridPoints[ntgpIndex];
            foreach(int tgp in ntgp.connectedTGPs)
            {
                if(tgp == t.index) { return true; }
            }
        }

        return false;
    }
}
