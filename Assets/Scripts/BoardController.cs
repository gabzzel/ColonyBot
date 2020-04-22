using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BoardController : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab = null;
    [SerializeField] private GameObject board = null;
    public Dictionary<Vector2Int, GridPoint> gridPoints = new Dictionary<Vector2Int, GridPoint>();
    private List<Tile> tiles = new List<Tile>();

    int diagonal = 5;
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
    List<int> standardNumbers = new List<int> { 9, 3, 2, 3, 8, 4, 5, 5, 6, 0, 6, 10, 12, 11, 8, 9, 11, 4, 10};

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
                    tiles[i].SetNumber(num);
                    indexes.RemoveAt(r);
                }
            }

            // Make sure we have good board
            if (!allowHighChanceNeighbours)
            {
                while (CheckHighChanceNeighbours())
                {
                    indexes = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
                    Debug.Log("Generating new board, while the old one wasn't fair!");
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
        foreach(Tile t in tiles)
        {
            // If this tile has a high chance of being rolled...
            if(t.number == 6 || t.number == 8)
            {
                foreach(Tile neighbour in t.GetNeighbouringTiles())
                {
                    if(neighbour.number == 6 || neighbour.number == 8)
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
        gridPoints.Clear();
        // Als x = Round(diagonal / 2), dan moet y tot diagonal gaan
        // Als x = 0, dan moet y tot 3 gaan
        // Als x = diagonal, dan moet y ook tot 3
        for (int x = 0; x <= diagonal * 2; x++)
        {
            //float maxY = (diagonal + 3) - Mathf.Abs(diagonal - 2 * x);
            // If x 
            if(x > 1 && x < 9)
            {
                for (int y = 2 - (x % 2); y < diagonal * 2; y++)
                {
                    CreateHexagonPoints(x, y);
                }
            }
            else if(x == 1 || x == 9)
            {
                for (int y = 3; y <= 7; y++)
                {
                    CreateHexagonPoints(x, y);
                }
            }
            else if(x == 0 || x == 10)
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
        Vector2 globalPos = new Vector2(x, y);
        Vector2Int colRow = new Vector2Int(col, row);
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
            foreach(Vector2Int conn in connections)
            {
                if(gridPoints.TryGetValue(gp.colRow + conn, out connectTo))
                {
                    gp.connectedTo.Add(connectTo);
                    //Debug.Log("Connecting " + gp.ToString() + " with " + connectTo);
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
        foreach(GridPoint gp in gridPoints.Values)
        {
            if (gp.isMiddle)
            {
                GameObject tileObject = Instantiate(tilePrefab, gp.position, tilePrefab.transform.rotation, GameObject.Find("Board").transform);
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
        foreach(Tile t in tiles)
        {
            if(t.number == number)
            {
                result.Add(t);
            }
        }
        return result;
    }
}


[CustomEditor(typeof(BoardController))]
public class BoardControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BoardController bc = (BoardController)target;

        if (GUILayout.Button("Create Empty Board"))
        {
            bc.CreateEmptyBoard();
        }

        if(GUILayout.Button("Create Filled Board"))
        {
            bc.CreateFilledBoard();
        }

        base.DrawDefaultInspector();
    }
}