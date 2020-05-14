﻿using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static Enums;

public class BoardController : MonoBehaviour
{
    [SerializeField] private Vector2 offset = Vector2.zero;

    [SerializeField] private GameObject tilePrefab = null;
    [SerializeField] private GameObject gridPointIndicator = null;
    [SerializeField] private GameObject board = null;
    public Dictionary<Vector2Int, GridPoint> gridPoints = new Dictionary<Vector2Int, GridPoint>();
    [SerializeField] private List<Tile> tiles = new List<Tile>();

    public bool useStandard = false;
    public bool allowHighChanceNeighbours = false;

    List<Enums.Resource> standardResources = new List<Enums.Resource>
        {
            Enums.Resource.Wool, // 0
            Enums.Resource.Wood, // 1
            Enums.Resource.Stone, // 2
            Enums.Resource.Grain, // 3
            Enums.Resource.Stone, // 4
            Enums.Resource.Grain, // 5
            Enums.Resource.Ore, // 6
            Enums.Resource.Wood, // 7
            Enums.Resource.Grain, // 8
            Enums.Resource.None, // 9
            Enums.Resource.Wood, // 10
            Enums.Resource.Stone,
            Enums.Resource.Wool,
            Enums.Resource.Ore,
            Enums.Resource.Ore,
            Enums.Resource.Grain,
            Enums.Resource.Wood,
            Enums.Resource.Wool,
            Enums.Resource.Wool
        };
    List<int> standardNumbers = new List<int> { 9, 3, 2, 3, 8, 4, 5, 5, 6, 0, 6, 10, 12, 11, 8, 9, 11, 4, 10 };

    public void CreateFilledBoard()
    {
        CreateEmptyBoard();
        DistributeResources();
        DistributeNumbers();
    }

    void DistributeResources()
    {
        if (useStandard)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].GetComponent<Tile>().SetResource(standardResources[i]);
            }
        }
        else
        {
            List<int> indexes = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
            tiles[9].SetResource(Enums.Resource.None);

            // Go trough all tiles
            for (int i = 0; i < tiles.Count; i++)
            {
                if (i != 9)
                {
                    int r = Random.Range(0, indexes.Count); // Get a random index
                    int index = indexes[r]; // Get the index of the resources
                    Enums.Resource res = standardResources[index]; // The random resource
                    tiles[i].SetResource(res);
                    indexes.RemoveAt(r);
                }
            }
        }
    }

    void DistributeNumbers()
    {
        if (useStandard)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].GetComponent<Tile>().SetNumber(standardNumbers[i]);
            }
        }
        else
        {
            List<int> indexes = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
            tiles[9].SetNumber(0);

            // Go trough all tiles
            for (int i = 0; i < tiles.Count; i++)
            {
                if (i != 9)
                {
                    int r = Random.Range(0, indexes.Count); // Get a random index
                    int index = indexes[r]; // Get the index of the number
                    int num = standardNumbers[index]; // The random number
                    tiles[i].SetNumber(num); // THIS ALSO UPDATES THE VALUES OF THE GRIDPOINTS! see tile.cs
                    indexes.RemoveAt(r);
                }
            }

            // Make sure we have good board
            if (!allowHighChanceNeighbours)
            {
                while (CheckHighChanceNeighbours())
                {
                    indexes = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
                    //Debug.Log("Generating new board, while the old one wasn't fair!");
                    // Go trough all tiles
                    for (int i = 0; i < tiles.Count; i++)
                    {
                        if (i != 9)
                        {
                            int r = Random.Range(0, indexes.Count); // Get a random index
                            int index = indexes[r]; // Get the index of the number
                            int num = standardNumbers[index]; // The random number
                            tiles[i].SetNumber(num);
                            indexes.RemoveAt(r);
                        }
                    }
                }
            }
        }
    }

    bool CheckHighChanceNeighbours()
    {
        // Go through all tiles...
        foreach (Tile t in tiles)
        {
            // If this tile has a high chance of being rolled...
            if (t.number == 6 || t.number == 8)
            {
                foreach (Tile neighbour in t.GetNeighbouringTiles())
                {
                    if (neighbour.number == 6 || neighbour.number == 8)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public void CreateEmptyBoard()
    {
        // 0. Clear all!
        Clear();
        // 1. Create a board as parent
        CreateBoard();
        // 2. Create the grid points
        CreateGridPoints();
        // 3. Connect the grid points to their neighbours
        ConnectGridPoints();
        // 4. Create the tiles objects and link them to the correct GridPoints
        CreateTiles();


        int number = 0;
        foreach(GridPoint p in gridPoints.Values)
        {
            if (!p.isMiddle) { number++; }
        }
        Debug.Log(number);
    }

    void Clear()
    {
        // Destroy the Board
        if (board != null)
        {
            if (Application.isPlaying) { Destroy(board); }
            else { DestroyImmediate(board); }
        }
        // Clear the Lists etc.
        tiles.Clear();
        gridPoints.Clear();
    }

    void CreateGridPoints()
    {
        if (gridPoints.Count > 0) { gridPoints.Clear(); }
        // Als x = Round(diagonal / 2), dan moet y tot diagonal gaan
        // Als x = 0, dan moet y tot 3 gaan
        // Als x = diagonal, dan moet y ook tot 3
        for (int x = 0; x <= 10; x++)
        {
            //float maxY = (diagonal + 3) - Mathf.Abs(diagonal - 2 * x);
            // If x 
            if (x > 1 && x < 9)
            {
                for (int y = 2 - (x % 2); y < 10; y++)
                {
                    CreateHexagonPoints(x, y);
                }
            }
            else if (x == 1 || x == 9)
            {
                for (int y = 3; y <= 7; y++)
                {
                    CreateHexagonPoints(x, y);
                }
            }
            else if (x == 0 || x == 10)
            {
                CreateHexagonPoints(x, 5);
                CreateHexagonPoints(x, 6);
            }
        }
    }

    void CreateHexagonPoints(int col, int row)
    {
        float y = row;
        if (col % 2 == 1) { y += 0.5f; }
        float x = col * 1.5f / Mathf.Sqrt(3);
        Vector2 globalPos = new Vector2(x * tilePrefab.transform.lossyScale.x / 2, y * tilePrefab.transform.lossyScale.y / 2) + offset;
        Vector2Int colRow = new Vector2Int(col, row);
        //GameObject g = Instantiate(gridPointIndicator, globalPos, Quaternion.identity);
        //g.name = "GridPoint " + colRow;
        GridPoint gp = new GridPoint(globalPos, colRow);
        gridPoints.Add(colRow, gp);
    }

    /// <summary>
    /// Connects all grid points to their neighbours
    /// </summary>
    void ConnectGridPoints()
    {
        Vector2Int[] connections = new Vector2Int[]
        {
            new Vector2Int(0, 1), // Above
            new Vector2Int(-1, 0), // Up left 
            new Vector2Int(-1, -1), // Below left
            new Vector2Int(0, -1), // Below
            new Vector2Int(1, -1), // Below right
            new Vector2Int(1, 0), // Up right
        };

        foreach (GridPoint gp in gridPoints.Values)
        {
            GridPoint connectTo = null;
            foreach (Vector2Int conn in connections)
            {
                if (gridPoints.TryGetValue(gp.colRow + conn, out connectTo))
                {
                    gp.connectedTo.Add(connectTo, null);
                }
            }
        }
    }

    /// <summary>
    /// Helper function to help organise the hierarchy
    /// </summary>
    void CreateBoard()
    {
        board = new GameObject("Board");
        board.transform.parent = this.transform;
    }

    void CreateTiles()
    {
        tiles.Clear();
        foreach (GridPoint gp in gridPoints.Values)
        {
            if (gp.isMiddle)
            {
                GameObject tileObject = Instantiate(tilePrefab, gp.position, tilePrefab.transform.rotation, board.transform);
                tileObject.name = "Tile " + (tiles.Count + 1) + " @ " + gp.colRow.ToString();
                Tile tile = tileObject.GetComponent<Tile>();
                gp.SetTile(tile);
                tiles.Add(tile);
            }
        }
    }

    public List<Tile> GetTilesByNumber(int number)
    {
        List<Tile> result = new List<Tile>();
        foreach (Tile t in tiles)
        {
            if (t.number == number)
            {
                result.Add(t);
            }
        }
        return result;
    }

    public List<GridPoint> GetGridPointsWithBuildingOfPlayer(ColonyPlayer player)
    {
        List<GridPoint> result = new List<GridPoint>();
        foreach (GridPoint gp in gridPoints.Values)
        {
            if (gp.building != null && gp.building.Owner == player)
            {
                result.Add(gp);
            }
        }
        if (result.Count == 0) { return null; }
        return result;
    }


    /// <summary>
    /// Check whether a certain GridPoint is eligible for building some building.
    /// </summary>
    /// <param name="gp"> The GridPoint which to check. </param>
    /// <param name="player"> The ColonyPlayer that wants to place the building. </param>
    /// <param name="buildingType"> The type of building that the player want to place. </param>
    /// <returns> Whether this GridPoint is eligible. </returns>
    public bool PossibleBuildingSite(GridPoint gp, ColonyPlayer player, BuildingType buildingType)
    {
        switch (buildingType)
        {
            case BuildingType.Street:
                return PossibleStreetBuildingSite(gp, player);
            case BuildingType.Village:
                return PossibleVillageBuildingSite(gp, player);
            case BuildingType.City:
                return PossibleCityBuildingSite(gp, player);
            default:
                return false;
        }
    }

    private bool PossibleCityBuildingSite(GridPoint gp, ColonyPlayer player)
    {
        // If there is no building on this gridpoint, there is no village to replace for a city
        if(gp.building == null) { return false; }
        // If the building on this gridpoint is not a village, we may not replace it
        else if(gp.building.Type != Enums.BuildingType.Village) { return false; }
        // If the building owner is not us, we cannot replace the village
        else if(gp.building.Owner != player) { return false; }
        else { return true; }
    }

    /// <summary>
    /// Check whether a certain gridpoint is eligible for Village placement by a certain player
    /// </summary>
    /// <param name="gp"> The GridPoint where the player want to place the village. </param>
    /// <param name="player"> The ColonyPlayer that wants to place the village </param>
    /// <returns></returns>
    private bool PossibleVillageBuildingSite(GridPoint gp, ColonyPlayer player)

    {
        // If there is already a building on this gridpoint, we cannot build here
        if(gp.building != null) { return false; }
        
        // Go through all neighbours
        foreach(GridPoint neighbour in gp.GetNeighbouringGridPoints())
        {
            // If one of our neighbours already has a village or city on it (Distance Rule), we cannot build here
            if (neighbour.building != null) { return false; }
            if (!neighbour.HasStreetConnectionForPlayer(player)) { return false; }
        }
    
        return true;
    }

    private bool PossibleStreetBuildingSite(GridPoint gp, ColonyPlayer player)
    {
        foreach(KeyValuePair<GridPoint, Building> connection in gp.connectedTo)
        {
            // If there is not already a street to this neighbour and our neighbour has a building owned by the player, a street can be build.
            if(connection.Value == null && connection.Key.building != null && connection.Key.building.Owner == player)
            {
                return true;
            }

            // Go through our second level neighbours...
            foreach(KeyValuePair<GridPoint, Building> connection2 in connection.Key.connectedTo)
            {
                // Skip this one if we are looking at the original
                if(connection2.Key == gp) { continue; }
                
                // If there is a street between our neighbour and this 2nd level neighbour, return true
                if(connection2.Value != null && connection.Value.Owner == player) { return true; }
            }
        }

        return false;
    }
}