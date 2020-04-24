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
    public List<Line> connectedStreets = new List<Line>();
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

    /// <summary>
    /// Get the second level neighbours exluding tiles. 
    /// </summary>
    /// <returns> A list containing all second level neighbours, excluding tiles.</returns>
    public List<GridPoint> GetSecondLevelNeighbours()
    {
        List<GridPoint> result = new List<GridPoint>();
        foreach(GridPoint n1 in connectedTo)
        {
            if (!n1.isMiddle)
            {
                foreach (GridPoint n2 in n1.connectedTo)
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

    /// <summary>
    /// Creates a Line with reference to the street and returns it. Also references it from the destination.
    /// </summary>
    /// <param name="destination"> Where the street should be connected to</param>
    /// <param name="street"> The Created street object </param>
    /// <returns> The created Line </returns>
    public Line CreateStreet(GridPoint destination, GameObject street)
    {
        Line line = new Line(this, destination, street);
        if (!connectedStreets.Contains(line))
        {
            connectedStreets.Add(line);
        }
        if (!destination.connectedStreets.Contains(line))
        {
            destination.connectedStreets.Add(line);
        }
        return line;
    }
}

