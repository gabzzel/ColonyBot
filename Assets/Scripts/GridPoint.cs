using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Enums;

public class GridPoint
{
    public Vector2 position = Vector2.zero;
    public Vector2Int colRow = Vector2Int.zero;
    public bool isMiddle = false;
    public List<GridPoint> connectedTo = new List<GridPoint>();
    public Building building = null;
    private Tile tile = null;

    public GridPoint(Vector2 position, Vector2Int colRow)
    {
        this.position = position;
        this.colRow = colRow;
        isMiddle = position.y % 3 == 1 || (position.y + 0.5) % 3 == 0;
    }

    public Tile GetTile()
    {
        if (!isMiddle)
        {
            Debug.LogWarning(this.ToString() + " is not eligible for a tile!");
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
        return connectedTo;
    }

    public List<Tile> GetNeighbouringTiles()
    {
        List<Tile> result = new List<Tile>();
        foreach (GridPoint neighbour in connectedTo)
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
        Dictionary<Resource, float> values = new Dictionary<Resource, float>();
        foreach(Resource res in GetResourcesAsList())
        {
            values.Add(res, 0);
        }
        foreach(Tile t in GetNeighbouringTiles())
        {
            values[t.resource] += t.GetValue();
        }
        return values;
    }

    public float GetValue()
    {
        float result = 0;
        foreach(Tile t in GetNeighbouringTiles())
        {
            result += t.GetValue();
        }
        return result;
    }
}

