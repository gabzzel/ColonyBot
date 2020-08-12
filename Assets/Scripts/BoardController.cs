using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Utility;
using Random = UnityEngine.Random;

public class BoardController : MonoBehaviour
{
    public static BoardController singleton = null;

    [SerializeField] private List<Sprite> resourceSprites = new List<Sprite>();

    [SerializeField] private Vector2 offset = Vector2.zero;

    [SerializeField] private GameObject tilePrefab = null;
    [SerializeField] private GameObject gridPointIndicator = null;
    [SerializeField] private GameObject harborIndicator = null;
    [SerializeField] private GameObject board = null;
    public List<GridPoint> allGridPoints = new List<GridPoint>();
    public List<NonTileGridPoint> nonTileGridPoints = new List<NonTileGridPoint>();
    public List<TileGridPoint> tileGridPoints = new List<TileGridPoint>();

    public int[,] connections = new int[73, 73];
    /// <summary>
    /// The indexes of all TileGridPoints in the set of all gridpoints
    /// </summary>
    public static List<int> tgpIndexes = new List<int> { 4, 9, 12, 16, 19, 22, 26, 29, 33, 36, 39, 43, 46, 50, 53, 56, 60, 63, 68 };
    /// <summary>
    /// The indexes of all NonTileGridPoints in the set of all gridpoints
    /// </summary>
    public static List<int> ntgpIndexes = new List<int> { 0, 1, 2, 3, 5, 6, 7, 8, 10, 11, 13, 14, 15, 17, 18, 20, 21, 23, 24, 25, 27, 28, 30, 31, 32, 34, 35, 37, 38, 40, 41, 42, 44, 45, 47, 48, 49, 51, 52, 54, 55, 57, 58, 59, 61, 62, 64, 65, 66, 67, 69, 70, 71, 72 };
    public static List<int> coastalNTGPs = new List<int>() { 0, 1, 5, 6, 13, 14, 23, 31, 40, 48, 57, 65, 64, 70, 69, 72, 71, 67, 66, 59, 58, 49, 41, 32, 24, 15, 7, 8, 2, 3 };

    public Tile[] bestRobberTiles = new Tile[4];
    private TileGridPoint robberLocation = null;

    [SerializeField] private List<Tile> tiles = new List<Tile>();

    [Header("Generate Options")]
    public bool useStandard = false;
    public bool allowHighChanceNeighbours = false;
    public bool drawGridPointIndicators = true;
    public bool drawHarbors = true;
    public bool randomHarbors = false;
    public bool fixDesertToMiddle = false;
    readonly int[] standardResources = new int[] { Wool, Lumber, Brick, Grain, Brick, Grain, Ore, Lumber, Grain, Desert, Lumber, Brick, Wool, Ore, Ore, Grain, Lumber, Wool, Wool };
    readonly List<int> standardNumbers = new List<int> { 9, 3, 2, 3, 8, 4, 5, 5, 6, 0, 6, 10, 12, 11, 8, 9, 11, 4, 10 };
    //readonly List<int> standardHarbors = new List<int> { }

    /* Properties */
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
    public List<Sprite> ResourceSprites { get { return resourceSprites; } }

    private void Awake()
    {
        if (singleton == null) { singleton = this; }
        else { Destroy(this); }
    }

    public void CreateFilledBoard()
    {
        CreateEmptyBoard();
        DetermineConnections();
        DistributeResources();
        DistributeNumbers();
        DistributeHarbors();
        foreach (int i in ntgpIndexes)
        {
            NonTileGridPoint ntgp = (NonTileGridPoint)allGridPoints[i];
            ntgp.UpdateValue();
        }

    }

    void DistributeResources()
    {
        if (useStandard)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                int resourceID = standardResources[i];
                tiles[i].GetComponent<Tile>().SetResource(resourceID);
            }
        }
        else
        {
            List<int> indexes = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
            if (fixDesertToMiddle) { tiles[9].SetResource(Desert); }
            else { indexes.Add(9); }

            // Go trough all tiles
            for (int i = 0; i < tiles.Count; i++)
            {
                int r = Random.Range(0, indexes.Count); // Get a random index
                int index = indexes[r]; // Get the index of the resources
                int resourceID = standardResources[index]; // The random resource
                tiles[i].SetResource(resourceID);
                indexes.RemoveAt(r);
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
            List<int> indexes = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
            //List<int> standardNumbersCopy = standardNumbers.ToArray().ToList();
            
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
                int tries = 0;
                while (CheckHighChanceNeighbours())
                {
                    tries++;
                    indexes = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
                    //Debug.Log("Generating new board, while the old one wasn't fair!");
                    // Go trough all tiles
                    for (int i = 0; i < tiles.Count; i++)
                    {
                        Tile t = tiles[i];

                        if (t.Resource != Desert)
                        {
                            int r = Random.Range(0, indexes.Count); // Get a random index
                            int index = indexes[r]; // Get the index of the number
                            int num = standardNumbers[index]; // The random number
                            t.SetNumber(num);
                            indexes.RemoveAt(r);
                        }
                        else
                        {
                            t.SetNumber(0);
                        }
                    }
                    if (tries > 1000) { throw new Exception("We couldn't create a board, even after 1000 tries!"); }
                }
            }

            else
            {
                indexes = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
                // Go trough all tiles
                for (int i = 0; i < tiles.Count; i++)
                {
                    Tile t = tiles[i];
                    if (t.Resource != Desert)
                    {
                        int r = Random.Range(0, indexes.Count); // Get a random index
                        int index = indexes[r]; // Get the index of the number
                        int num = standardNumbers[index]; // The random number
                        t.SetNumber(num);
                        indexes.RemoveAt(r);
                    }
                    else
                    {
                        t.SetNumber(0);
                    }
                }
            }            
        }
    }

    void DistributeHarbors()
    {
        List<int> harbors = new List<int>() { Lumber, Brick, Grain, Ore, Wool, RandomHarbor, RandomHarbor, RandomHarbor, RandomHarbor };
        int startCoastalIndex = Random.Range(0, coastalNTGPs.Count); // A random index of the coastalNTGPs list
        int prev = NoHarbor;
        int prevprev = NoHarbor;
        bool skip = false;

        for (int i = 2; i < coastalNTGPs.Count; i++)
        {
            int currentCoastalIndex = startCoastalIndex + i;
            if (currentCoastalIndex >= coastalNTGPs.Count) { currentCoastalIndex -= coastalNTGPs.Count; }
            int currentGI = coastalNTGPs[currentCoastalIndex];
            NonTileGridPoint ntgp = (NonTileGridPoint)allGridPoints[currentGI];

            if (prev == NoHarbor && prevprev != NoHarbor || i == 2)
            {
                // Choose a random harbor type
                int randomHarborIndex = Random.Range(0, harbors.Count);
                int harbor = harbors[randomHarborIndex];
                ntgp.harbor = harbor;
                harbors.RemoveAt(randomHarborIndex);
                if (drawHarbors)
                {
                    GameObject h = Instantiate(harborIndicator, allGridPoints[currentGI].position, Quaternion.identity);
                    h.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = HarborNames[harbor];
                }
            }
            else if (prevprev == NoHarbor && prev != NoHarbor)
            {
                ntgp.harbor = prev;
                if (drawHarbors)
                {
                    GameObject h = Instantiate(harborIndicator, allGridPoints[currentGI].position, Quaternion.identity);
                    h.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = HarborNames[ntgp.harbor];
                }
                if (!skip)
                {
                    i++;
                    skip = true;
                }
            }

            prevprev = prev;
            prev = ntgp.harbor;
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
        board = new GameObject("Board");
        board.transform.parent = this.transform;
        // 2. Create the grid points
        CreateGridPoints();
        // 3. Connect the grid points to their neighbours
        // ConnectGridPoints();
        // 4. Create the tiles objects and link them to the correct TileGridPoints
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

        for (int i = GameObject.FindGameObjectsWithTag("Harbor").Length - 1; i >= 0; i--)
        {
            Destroy(GameObject.FindGameObjectsWithTag("Harbor")[i]);
        }
    }

    void CreateGridPoints()
    {
        allGridPoints.Clear();
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

        if ((col % 2 == 1 && (row + 1) % 3 == 0) || (col % 2 == 0 && (row - 1) % 3 == 0))
        {
            TileGridPoint tgp = new TileGridPoint(globalPos, new Vector2Int(col, row));
            tileGridPoints.Add(tgp);
            allGridPoints.Add(tgp);
            tgp.id = col * 9 + row;
            tgp.index = allGridPoints.Count - 1;
            tgp.isMiddle = true;
        }
        else
        {
            NonTileGridPoint ntgp = new NonTileGridPoint(globalPos, new Vector2Int(col, row));
            nonTileGridPoints.Add(ntgp);
            allGridPoints.Add(ntgp);
            ntgp.id = col * 9 + row;
            ntgp.index = allGridPoints.Count - 1;
        }

        if (drawGridPointIndicators)
        {
            GameObject g = Instantiate(gridPointIndicator, globalPos, Quaternion.identity);
            g.name = "GridPoint " + (allGridPoints.Count - 1);
            g.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = allGridPoints[allGridPoints.Count - 1].colRow.ToString();
        }
    }

    /*
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
    */
    void DetermineConnections()
    {
        connections = new int[73, 73];
        for (int i = 0; i < 73; i++)
        {
            for (int j = 0; j < 73; j++)
            {
                GridPoint from = allGridPoints[i];
                GridPoint to = allGridPoints[j];

                int extra = from.colRow.x % 2;

                if ((to.id == from.id + 1 && to.colRow.x == from.colRow.x) || (to.id == from.id + extra + 8 || to.id == from.id + extra + 9) && to.colRow.x == from.colRow.x + 1)
                {
                    from.Connect(j, to.isMiddle);
                    to.Connect(i, from.isMiddle);
                }
            }
        }
    }

    void CreateTiles()
    {
        tiles.Clear();
        foreach (TileGridPoint tgp in tileGridPoints)
        {
            GameObject tileObject = Instantiate(tilePrefab, tgp.position, tilePrefab.transform.rotation, board.transform);
            tileObject.name = "Tile " + (tiles.Count + 1) + " @ " + tgp.position.ToString();
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

    /// <summary>
    /// Check whether a certain GridPoint is eligible for building some building.
    /// </summary>
    /// <param name="gp"> The GridPoint which to check. </param>
    /// <param name="player"> The ColonyPlayer that wants to place the building. </param>
    /// <param name="buildingType"> The type of building that the player want to place. </param>
    /// <param name="free"> If the buildingtype is village, can we place it wherever we want? </param>
    /// <returns> Whether this GridPoint is eligible. </returns>
    public bool PossibleBuildingSite(NonTileGridPoint gp, ColonyPlayer player, int buildingType, bool free = false)
    {
        if (buildingType == Utility.Street) { return PossibleStreetBuildingSite(gp, player); }
        else if (buildingType == Village) { return PossibleVillageBuildingSite(gp, player, free); }
        else { return PossibleCityBuildingSite(gp, player); }
    }

    private bool PossibleCityBuildingSite(NonTileGridPoint gp, ColonyPlayer player)
    {
        // If there is no building on this gridpoint, there is no village to replace for a city
        if (gp.Building == null) { return false; }
        // If the building on this gridpoint is not a village, we may not replace it
        else if (gp.Building.Type != Village) { return false; }
        // If the building owner is not us, we cannot replace the village
        else if (gp.Building.Owner != player) { return false; }
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
        if (gp.Building != null) { return false; }

        if (!free && !gp.HasStreetConnectionForPlayer(player.ID)) { return false; } //

        int notOurs = 0;
        foreach (int neighbourIndex in gp.connectedNTGPs)
        {
            NonTileGridPoint neighbour = (NonTileGridPoint)allGridPoints[neighbourIndex];
            //if (neighbour.Building != null || !free && !neighbour.HasStreetConnectionForPlayer(player.ID)) { return false; }
            if (neighbour.Building != null) { return false; }

            // We check for every neighbour if the NTGP connection is occupied by someone other than the current player.
            // If that is so, we remember that value. If we come across this value again, we cannot build here, as we would be interupting someones street
            int value = connections[gp.index, neighbourIndex];
            if(value != player.ID + 2 && value > 1)
            {
                if(notOurs == 0) { notOurs = value; }
                else if(notOurs == value) { notOurs = int.MaxValue; }
            }
        }

        // If 2 or 3 NTGP connections with this gridpoint are occupied by a street of someone else, we
        if(notOurs == int.MaxValue) { return false; }

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
        return start != null && connections[start.index, end.index] == 1 && connections[end.index, start.index] == 1 && (end.Building == null || end.OccupiedBy(player));
    }

    public TileGridPoint GetBestTileForRobber(ColonyPlayer player)
    {
        TileGridPoint best = null;
        float highest = float.MinValue;

        foreach (TileGridPoint tgp in tileGridPoints)
        {
            // We cannot place the robber on the current location
            if (tgp == RobberLocation) { continue; }

            float value = 0f;
            foreach (int ntgpIndex in tgp.connectedNTGPs)
            {
                NonTileGridPoint ntgp = (NonTileGridPoint)allGridPoints[ntgpIndex];
                // If there is no building here, just skip this one.
                if (ntgp.Building == null) { continue; }
                if (!ntgp.OccupiedBy(player))
                {
                    // Possibly multiplied by the points of the player?
                    value += tgp.Value * ntgp.Building.Type;
                }
            }

            if (value > highest)
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

        foreach (int neighbourIndex in tgp.connectedNTGPs)
        {
            NonTileGridPoint ntgp = (NonTileGridPoint)allGridPoints[neighbourIndex];
            if (ntgp.Building != null && !ntgp.OccupiedBy(player))
            {
                players.Add(ntgp.Building.Owner);
            }
        }

        if (players.Count == 0) { throw new System.Exception("Cannot return player when no players are connected to " + tgp.ToString()); }
        return players[Random.Range(0, players.Count)];
    }

    /*
    public int LongestRoadLength(ColonyPlayer player)
    {
        Stack<Street> stack = new Stack<Street>();
        HashSet<Street> seen = new HashSet<Street>();

        foreach(Building b in player.Buildings)
        {
            if(b.Type == Utility.Street)
            {
                stack.Push((Street)b);
            } 
        }

        while(stack.Count > 0)
        {
            Street street = stack.Pop();

        }

    }

    */
}