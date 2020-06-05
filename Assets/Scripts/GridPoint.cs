using UnityEngine;
using System.Collections.Generic;

public class GridPoint
{
    /// <summary>
    /// The ID of this GridPoint, used for determining neighbours.
    /// </summary>
    public int id = 0;
    /// <summary>
    /// The index of this gridpoint in the BoardController.singleton.allGridPoints list.
    /// </summary>
    public int index = 0;
    public bool isMiddle = false;
    public Vector2 position = Vector2.zero;
    public Vector2Int colRow = Vector2Int.zero;
    /// <summary>
    /// The indexes of our neighbours (NTGP and TGP alike) in the BoardController.singleton.allGridPoints list.
    /// </summary>
    //public HashSet<int> connectedIndexes = new HashSet<int>();
    public HashSet<int> connectedNTGPs = new HashSet<int>();
    public HashSet<int> connectedTGPs = new HashSet<int>();

    /// <summary>
    /// Connect a GridPoint to one of its neighbours by index.
    /// </summary>
    /// <param name="index"> The index of the neighbour in the allGridPoints list. </param>
    public void Connect(int index, bool tgp)
    {
        if (tgp) { connectedTGPs.Add(index); }
        else { connectedNTGPs.Add(index); }
        BoardController.singleton.connections[this.index, index] = 1;
    }

    /*

    public bool isMiddle = false;
    public Dictionary<GridPoint, Building> connectedTo = new Dictionary<GridPoint, Building>();
    public List<GridPoint> connectedTileGridPoints = new List<GridPoint>();
    public Dictionary<Resource, float> resourceValues = new Dictionary<Resource, float>();
    public Building building = null;
    private Tile tile = null;

    public GridPoint(Vector2 position, Vector2Int colRow)
    {
        this.position = position;
        this.colRow = colRow;
        //isMiddle = position.y % 3 == 1 || (position.y + 0.5) % 3 == 0;

        // Either x is odd and (y+1)%3==0 OR x is even and (y-1)%3==0
        isMiddle = (colRow.x % 2 == 1 && (colRow.y + 1) % 3 == 0) || (colRow.x % 2 == 0 && (colRow.y - 1) % 3 == 0);

        foreach (Resource res in Utility.GetResourcesAsList())
        {
            resourceValues.Add(res, 0);
        }
    }

    public Tile GetTile()
    {
        if (!isMiddle)
        {
            Debug.LogWarning(ToString() + " is not eligible for a tile!");
            return null;
        }
        else
        {
            return tile;
        }
    }

    /// <summary>
    /// Set the reference to the tile. Also give a tile a reference back.
    /// </summary>
    /// <param name="t"> The Tile to reference</param>
    public void SetTile(Tile tile)
    {
        if (!isMiddle)
        {
            Debug.LogWarning(ToString() + "is not eligible for tiles.");
            return;
        }
        else if(tile == null)
        {
            Debug.LogWarning(ToString() + " cannot reference null Tile!");
            return;
        }

        this.tile = tile;
        tile.SetGridPoint(this);
    }

    public override string ToString()
    {
        return "GridPoint @ " + colRow.ToString();
    }

    public List<GridPoint> GetNeighbouringGridPoints()
    {   
        return connectedTo.Keys.ToList();
    }

    public List<GridPoint> GetNonMiddleNeighbours()
    {
        List<GridPoint> neighbours = new List<GridPoint>();
        foreach(GridPoint gp in connectedTo.Keys)
        {
            if (!gp.isMiddle) { neighbours.Add(gp); }
        }
        return neighbours;
    }

    /// <summary>
    /// Get the second level neighbours exluding tiles. 
    /// </summary>
    /// <returns> A list containing all second level neighbours, excluding tiles.</returns>
    public List<GridPoint> GetSecondLevelNeighbours()
    {
        List<GridPoint> result = new List<GridPoint>();
        foreach(GridPoint n1 in connectedTo.Keys)
        {
            if (!n1.isMiddle)
            {
                foreach (GridPoint n2 in n1.connectedTo.Keys)
                {
                    if (!n2.isMiddle && n2 != this)
                    {
                        result.Add(n2);
                    }
                }
            }           
        }
        return result;
    }

    public List<GridPoint> GetSecondLevelConnectedByStreets(ColonyPlayer player)
    {
        List<GridPoint> result = new List<GridPoint>();
        // Go through all connected gridpoints (1st level neightbours)...
        foreach(KeyValuePair<GridPoint, Building> connection in connectedTo)
        {
            // If we have no street between us and our neighbour that is owned by the current player, continue
            if(connection.Value == null || connection.Value.Owner != player) { continue; }

            // If we have a valid street connection, loop through all the 2nd level neighbours...
            foreach (KeyValuePair<GridPoint, Building> connection2 in connection.Key.connectedTo)
            {
                // If our neighbour's neighbour does not have a valid connection to our neighbour, continue
                if (connection2.Value == null || connection2.Value.Owner != player) { continue; }
                result.Add(connection2.Key);
            }
        }
        return result;
    }

    public List<Tile> GetNeighbouringTiles()
    {
        List<Tile> result = new List<Tile>();
        foreach (GridPoint neighbour in connectedTo.Keys)
        {
            if (neighbour.isMiddle)
            {
                result.Add(neighbour.tile);
            }
        }
        return result;
    }

    /// <summary>
    /// Get the value of this gridpoint per resource
    /// </summary>
    /// <returns></returns>
    public Dictionary<Resource, float> GetResourceValues()
    {
        return resourceValues;
    }

    /// <summary>
    /// Update the resource values of our neighbours when our tile has been set. This is called by our tile when our tile is given a number.
    /// </summary>
    public void UpdateNeighbourValues()
    {
        foreach(GridPoint neighbour in GetNeighbouringGridPoints())
        {
            if (!neighbour.isMiddle && tile.resource != Resource.None)
            {
                neighbour.resourceValues[tile.resource] += tile.value;
            }    
        }
    }

    public float GetValue()
    {
        float result = 0;
        foreach(Tile t in GetNeighbouringTiles())
        {
            result += t.value;
        }
        return result;
    }

    /// <summary>
    /// Check if this gridpoint is eligible for village placement
    /// </summary>
    /// <param name="player"> The player for which to check </param>
    /// <returns> Whether this gridpoint is eligible </returns>
    public bool HasStreetConnectionForPlayer(ColonyPlayer player)
    {
        // Go through all neighbours...
        foreach(KeyValuePair<GridPoint, Building> connection in connectedTo)
        {
            // If we are connected to a neighbour with a street that belongs to us, return true

            if(connection.Value != null && connection.Value.Owner == player) { return true; }
        }
        return false;
    }

    */

}

