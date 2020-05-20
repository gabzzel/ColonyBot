using System.Collections.Generic;
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
    public List<NonTileGridPoint> nonTileGridPoints = new List<NonTileGridPoint>();
    public List<TileGridPoint> tileGridPoints = new List<TileGridPoint>();

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
            tileGridPoints.Add(tgp);
            allGridPoints.Add(colRow, tgp);
        }
        else
        {
            NonTileGridPoint ntgp = new NonTileGridPoint(globalPos, colRow);
            nonTileGridPoints.Add(ntgp);
            allGridPoints.Add(colRow, ntgp);
        } 
    }

    /// <summary>
    /// Connects all grid points to their neighbours
    /// </summary>
    void ConnectGridPoints()
    {
        Vector2Int[] tgpConnections = new Vector2Int[]
        {
            new Vector2Int(0, 1), // Above
            new Vector2Int(1, 1), // Right up 
            new Vector2Int(1, 0), // Right low
            new Vector2Int(0, -1), // Below
            new Vector2Int(-1, 0), // Left low
            new Vector2Int(-1, 1), // Left up
        };
        Vector2Int[] ntgpConnections = new Vector2Int[]
        {
            new Vector2Int(0,1),
            new Vector2Int(-1,0),
            new Vector2Int(1,0)
        };

        // Connect NTGP's to their NTGP neighbours
        foreach(NonTileGridPoint ntgp in nonTileGridPoints)
        {
            foreach(NonTileGridPoint ntgp2 in nonTileGridPoints)
            {
                foreach(Vector2Int dir in ntgpConnections)
                {
                    if(ntgp2.colRow == ntgp.colRow + dir)
                    {
                        ntgp.Connect(ntgp2);
                        ntgp2.Connect(ntgp);
                    }
                }
            }
        }

        // Connect TGP's to their NTGP neighbours
        foreach(TileGridPoint tgp in tileGridPoints)
        {
            foreach(NonTileGridPoint ntgp in nonTileGridPoints)
            {
                foreach(Vector2Int dir in tgpConnections)
                {
                    if(ntgp.colRow == tgp.colRow + dir)
                    {
                        ntgp.Connect(tgp);
                        tgp.Connect(ntgp);
                    }
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
        foreach (TileGridPoint tgp in tileGridPoints)
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
        foreach (NonTileGridPoint gp in nonTileGridPoints)
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
            if (neighbour.Building != null) { return false; }
            if (!free && !neighbour.HasStreetConnectionForPlayer(player)) { return false; }
        }
    
        return true;
    }

    private bool PossibleStreetBuildingSite(NonTileGridPoint gp, ColonyPlayer player)
    {
        return gp.FindConnectionWithPlayer(player) != null;
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
}