﻿using System.Collections.Generic;
using UnityEngine;
using static Enums;

public class BoardController : MonoBehaviour
{
    public static BoardController singleton = null;

    [Header("Tile Sprites")]
    [SerializeField] Sprite DesertTile = null;
    [SerializeField] Sprite OreTile = null;
    [SerializeField] Sprite StoneTile = null;
    [SerializeField] Sprite GrainTile = null;
    [SerializeField] Sprite WoodTile = null;
    [SerializeField] Sprite WoolTile = null;
    
    [SerializeField] private Vector2 offset = Vector2.zero;

    [SerializeField] private GameObject tilePrefab = null;
    [SerializeField] private GameObject gridPointIndicator = null;
    [SerializeField] private GameObject board = null;
    public Dictionary<Vector2Int, GridPoint> allGridPoints = new Dictionary<Vector2Int, GridPoint>();
    public Dictionary<Vector2Int, NonTileGridPoint> nonTileGridPoints = new Dictionary<Vector2Int, NonTileGridPoint>();
    public Dictionary<Vector2Int, TileGridPoint> tileGridPoints = new Dictionary<Vector2Int, TileGridPoint>();
    // The total value of every resource on the board. Calculated in DistributeNumbers()
    public Dictionary<Resource, float> TotalValues = new Dictionary<Resource, float>();
    private TileGridPoint robberLocation = null;

    [SerializeField] private List<Tile> tiles = new List<Tile>();

    public bool useStandard = false;
    public bool allowHighChanceNeighbours = false;
    public bool drawGridPointIndicators = true;

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

    public TileGridPoint RobberLocation
    {
        get { return robberLocation; }
        set
        {
            TileGridPoint oldLocation = robberLocation;
            robberLocation = value;
            robberLocation.Robber = true;
            if (oldLocation != null) { oldLocation.Robber = false; }
        }
    }

    private void Awake()
    {
        if(singleton == null) { singleton = this; }
        else { Destroy(this); }
    }

    public void CreateFilledBoard()
    {
        CreateEmptyBoard();
        DistributeResources();
        DistributeNumbers();
        ConnectGridPoints();
    }

    void DistributeResources()
    {
        if (useStandard)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                Resource res = standardResources[i];
                tiles[i].GetComponent<Tile>().SetResource(res, GetSpriteByResource(res));
            }
        }
        else
        {
            List<int> indexes = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
            tiles[9].SetResource(Enums.Resource.None, DesertTile);

            // Go trough all tiles
            for (int i = 0; i < tiles.Count; i++)
            {
                if (i != 9)
                {
                    int r = Random.Range(0, indexes.Count); // Get a random index
                    int index = indexes[r]; // Get the index of the resources
                    Enums.Resource res = standardResources[index]; // The random resource
                    tiles[i].SetResource(res, GetSpriteByResource(res));
                    indexes.RemoveAt(r);
                }
            }
        }
    }

    void DistributeNumbers()
    {
        TotalValues = Enums.DefaultResDictFloat;

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
                            Tile t = tiles[i];
                            t.SetNumber(num);
                            indexes.RemoveAt(r);
                        }
                    }
                }
            }
        }

        foreach(Tile t in tiles)
        {
            if(t.Resource != Resource.None) { TotalValues[t.Resource] += t.Value; }
        }
    }

    bool CheckHighChanceNeighbours()
    {
        // Go through all tiles...
        foreach (Tile t in tiles)
        {
            // If this tile has a high chance of being rolled...
            if (t.Number == 6 || t.Number == 8)
            {
                foreach (Tile neighbour in t.GetNeighbouringTiles())
                {
                    if (neighbour.Number == 6 || neighbour.Number == 8)
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
        // ConnectGridPoints();
        // 4. Create the tiles objects and link them to the correct GridPoints
        CreateTiles();
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
        nonTileGridPoints.Clear();
        tileGridPoints.Clear();
        tiles.Clear();
        allGridPoints.Clear();
    }

    void CreateGridPoints()
    {
        if (allGridPoints.Count > 0) { allGridPoints.Clear(); }
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

        if (drawGridPointIndicators)
        {
            GameObject g = Instantiate(gridPointIndicator, globalPos, Quaternion.identity);
            g.name = "GridPoint " + colRow;
            g.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = colRow.ToString();
        }
        
        if ((colRow.x % 2 == 1 && (colRow.y + 1) % 3 == 0) || (colRow.x % 2 == 0 && (colRow.y - 1) % 3 == 0))
        {
            TileGridPoint tgp = new TileGridPoint(globalPos, colRow);
            tileGridPoints.Add(colRow, tgp);
            allGridPoints.Add(colRow, tgp);
        }
        else
        {
            NonTileGridPoint ntgp = new NonTileGridPoint(globalPos, colRow);
            nonTileGridPoints.Add(colRow, ntgp);
            allGridPoints.Add(colRow, ntgp);
        } 
    }

    /// <summary>
    /// Connects all grid points to their neighbours
    /// </summary>
    void ConnectGridPoints()
    {
        Vector2Int[] directions1 = new Vector2Int[]
        {
            new Vector2Int(0, 1), // Above
            new Vector2Int(1, 1), // Right up
            new Vector2Int(1, 0), // Right low 
            new Vector2Int(0, -1), // Below
            new Vector2Int(-1, 0), // Left low 
            new Vector2Int(-1, 1), // Left up
        };
        Vector2Int[] directions2 = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 0)
        };

        // Connect all TGP's to their neighbouring NTGP's
        foreach(KeyValuePair<Vector2Int, TileGridPoint> tgp in tileGridPoints)
        {
            Vector2Int[] directions = tgp.Key.x % 2 == 1 ? directions1 : directions2;

            foreach(Vector2Int dir in directions)
            {
                Vector2Int position = tgp.Key + dir;
                NonTileGridPoint ntgp = null;
                if(nonTileGridPoints.TryGetValue(position, out ntgp))
                {
                    tgp.Value.Connect(ntgp);
                    ntgp.Connect(tgp.Value);
                }
            }
        }

        Vector2Int[] type1 = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1)
        };
        Vector2Int[] type2 = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0)
        };
        Vector2Int[] type3 = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };
        Vector2Int[] type4 = new Vector2Int[]
        {
            new Vector2Int(1,1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 1)
        };

        // Connect all NTGP's to their correct NTGP neighbours
        foreach(KeyValuePair<Vector2Int, NonTileGridPoint> pair in nonTileGridPoints)
        {
            NonTileGridPoint ntgp = pair.Value;
            NonTileGridPoint neighbour = null;
            if(pair.Key.y % 3 == 2) { directions1 = type1; }
            else if(pair.Key.x % 2 == 1 && pair.Key.y % 3 == 0) { directions1 = type2; }
            else if(pair.Key.x % 2 == 0 && pair.Key.y % 3 == 0) { directions1 = type3; }
            else { directions1 = type4; }

            foreach(Vector2Int direction in directions1)
            {
                if(nonTileGridPoints.TryGetValue(pair.Key + direction, out neighbour))
                {
                    ntgp.Connect(neighbour);
                    neighbour.Connect(ntgp);
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
        foreach (TileGridPoint tgp in tileGridPoints.Values)
        {
            GameObject tileObject = Instantiate(tilePrefab, tgp.position, tilePrefab.transform.rotation, board.transform);
            tileObject.name = "Tile " + (tiles.Count + 1) + " @ " + tgp.colRow.ToString();
            Tile tile = tileObject.GetComponent<Tile>();
            tgp.Tile = tile;
            tile.GridPoint = tgp;
            tiles.Add(tile);
        }
    }

    public List<Tile> GetTilesByNumber(int number)
    {
        List<Tile> result = new List<Tile>();
        foreach (Tile t in tiles)
        {
            if (t.Number == number)
            {
                result.Add(t);
            }
        }
        return result;
    }

    public HashSet<NonTileGridPoint> GetGridPointsWithBuildingOfPlayer(ColonyPlayer player)
    {
        HashSet<NonTileGridPoint> result = new HashSet<NonTileGridPoint>();
        foreach (NonTileGridPoint gp in nonTileGridPoints.Values)
        {
            if (gp.OccupiedBy(player)) { result.Add(gp); }
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
    /// <param name="free"> If the buildingtype is village, can we place it wherever we want? </param>
    /// <returns> Whether this GridPoint is eligible. </returns>
    public bool PossibleBuildingSite(NonTileGridPoint gp, ColonyPlayer player, BuildingType buildingType, bool free = false)
    {
        switch (buildingType)
        {
            case BuildingType.Street:
                return PossibleStreetBuildingSite(gp, player);
            case BuildingType.Village:
                return PossibleVillageBuildingSite(gp, player, free);
            case BuildingType.City:
                return PossibleCityBuildingSite(gp, player);
            default:
                return false;
        }
    }

    private bool PossibleCityBuildingSite(NonTileGridPoint gp, ColonyPlayer player)
    {
        // If there is no building on this gridpoint, there is no village to replace for a city
        if(gp.Building == null) { return false; }
        // If the building on this gridpoint is not a village, we may not replace it
        else if(gp.Building.Type != Enums.BuildingType.Village) { return false; }
        // If the building owner is not us, we cannot replace the village
        else if(gp.Building.Owner != player) { return false; }
        else { return true; }
    }

    /// <summary>
    /// Check whether a certain gridpoint is eligible for Village placement by a certain player
    /// </summary>
    /// <param name="gp"> The GridPoint where the player want to place the village. </param>
    /// <param name="player"> The ColonyPlayer that wants to place the village </param>
    /// <returns></returns>
    public bool PossibleVillageBuildingSite(NonTileGridPoint gp, ColonyPlayer player, bool free)
    {
        // If there is already a building on this gridpoint, we cannot build here
        if(gp.Building != null) { return false; }
        
        // Go through all neighbours
        foreach(NonTileGridPoint neighbour in gp.ConnectedNTGPs.Keys)
        {
            // If one of our neighbours already has a village or city on it (Distance Rule), we cannot build here
            if (neighbour.Building != null)
            {
                return false;
            }
            if (!free && !neighbour.HasStreetConnectionForPlayer(player)) { return false; }
        }
    
        return true;
    }

    /// <summary>
    /// Check whether a NTGP is eligle as a destination for a street.
    /// </summary>
    /// <param name="gp"> The destination of the street ("to"). </param>
    /// <param name="player"> The player that is building the street. </param>
    /// <returns> True if gp is eligible, False otherwise. </returns>
    private bool PossibleStreetBuildingSite(NonTileGridPoint end, ColonyPlayer player)
    {
        NonTileGridPoint start = end.FindConnectionWithPlayer(player); // The "from" / "start" point of the street
        // 1. Our start must exist
        // 2. There must not exist a street already between start and end
        // 3. The "end" / "to" cannot be occupied by another player
        return start != null && start.ConnectedNTGPs[end] == null && (end.Building == null || end.OccupiedBy(player));
    }

    private Sprite GetSpriteByResource(Resource res)
    {
        switch (res)
        {
            case Resource.None:
                return DesertTile;
            case Resource.Wood:
                return WoodTile;
            case Resource.Stone:
                return StoneTile;
            case Resource.Wool:
                return WoolTile;
            case Resource.Grain:
                return GrainTile;
            case Resource.Ore:
                return OreTile;
            default:
                return null;
        }
    }

    public TileGridPoint GetBestTileForRobber(ColonyPlayer player)
    {
        TileGridPoint best = null;
        float highest = float.MinValue;

        foreach(TileGridPoint tgp in tileGridPoints.Values)
        {
            // We cannot place the robber on the current location
            if(tgp == RobberLocation) { continue; }

            float value = 0f;
            foreach(NonTileGridPoint ntgp in tgp.ConnectedNTGPs)
            {
                // If there is no building here, just skip this one.
                if(ntgp.Building == null) { continue; }
                if (!ntgp.OccupiedBy(player))
                {
                    float multi = ntgp.Building.Type == BuildingType.City ? 2f : 1f;
                    // Possibly multiplied by the points of the player?
                    value += tgp.Value * multi;
                }
            }

            if(value > highest)
            {
                best = tgp;
                highest = value;
            }
        }

        return best;
    }

    public ColonyPlayer GetRandomPlayerConnectedToTile(TileGridPoint tgp, ColonyPlayer player)
    {
        List<ColonyPlayer> players = new List<ColonyPlayer>();
        foreach(NonTileGridPoint ntgp in tgp.ConnectedNTGPs)
        {
            if (ntgp.Building != null && !ntgp.OccupiedBy(player))
            {
                players.Add(ntgp.Building.Owner);
            }
        }

        if(players.Count == 0) { throw new System.Exception("Cannot return player when no players are connected to " + tgp.ToString()); }
        return players[Random.Range(0, players.Count)];
    }
}