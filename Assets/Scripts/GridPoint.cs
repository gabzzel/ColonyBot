using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridPoint
{
    public Vector2 position = Vector2.zero;
    public Vector2Int colRow = Vector2Int.zero;
    public bool isMiddle = false;
    public List<GridPoint> connectedTo = new List<GridPoint>();
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
}

