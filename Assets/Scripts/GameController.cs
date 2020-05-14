using UnityEngine;
using System.Collections.Generic;
using static Enums;
using Unity.MLAgents;

public class GameController : MonoBehaviour
{
    public static GameController singleton = null;

    public int pointsToWin = 12;
    [Range(1, 4, order = 1)] public int numberOfPlayers = 3;
    public float stepTime = 1f;
    private float stepTimer = 0f;

    [SerializeField] private GameObject villagePrefab = null;
    [SerializeField] private GameObject cityPrefab = null;
    [SerializeField] private GameObject streetPrefab = null;
    private BoardController bc = null;
    private PlayerManager pm = null;
    private UIController uic = null;

    private List<int> initialPlayers = new List<int>();

    private void Awake()
    {
        if(singleton == null) { singleton = this; }
        else { Destroy(this.gameObject); }

        bc = GetComponent<BoardController>();
        pm = GetComponent<PlayerManager>();
        uic = GameObject.FindGameObjectWithTag("UIController").GetComponent<UIController>();
        Academy.Instance.AutomaticSteppingEnabled = false;
        Academy.Instance.OnEnvironmentReset += NewGame;
    }


    private void Start()
    {
        LoadSettings();
    }

    public void LoadSettings()
    {
        EnvironmentParameters ep = Academy.Instance.EnvironmentParameters;
        numberOfPlayers = Mathf.FloorToInt(ep.GetWithDefault("number_of_players", 4f));
        pointsToWin = Mathf.FloorToInt(ep.GetWithDefault("points_to_win", 12f));
        bc.useStandard = ep.GetWithDefault("standard_board", 0f) == 1 ? true : false;
        bc.allowHighChanceNeighbours = ep.GetWithDefault("allow_high_chance_neighbours", 0f) == 1 ? true : false;
        stepTime = Mathf.Max(ep.GetWithDefault("step_time", 1f), 0);
    }

    public void NewGame()
    {
        Notifier.singleton.Notify("New Game Started!");
        // 1. Create a new board
        bc.CreateFilledBoard();
        // 2. Initialize players
        pm.Initialize(numberOfPlayers);
        // 2.1 Destroy all buildings
        for (int i = GameObject.FindGameObjectsWithTag("Building").Length - 1; i >= 0; i--)
        {
            GameObject building = GameObject.FindGameObjectsWithTag("Building")[i];
            Destroy(building);
        }
        // 3. Initialize UI of the players
        uic.Initialize(pm.players);
        // 4. Decide the initial order of free placements
        DecideInitialPlayerOrder();
    }

    private void FixedUpdate()
    {
        ColonyPlayer winner = pm.PlayerHasWon();
        if(winner == null)
        {
            stepTimer += Time.fixedDeltaTime;
            if(stepTimer >= stepTime)
            {
                NextStep();
                stepTimer = 0f;
            }
        }
        else
        {
            EndGame(winner);
        }
    }

    public void NextStep()
    {
        if(initialPlayers.Count > 0)
        {
            pm.currentPlayer = initialPlayers[initialPlayers.Count - 1];
            initialPlayers.RemoveAt(initialPlayers.Count - 1);
            pm.CurrentPlayer.RequestDecision();
        }
        else
        {
            // Perform 1 step / round where every player gets a turn and dices are rolled before their turns
            ColonyPlayer currentPlayer = pm.CurrentPlayer;

            // If the current player passed last turn, roll the dice
            if (currentPlayer.turnPhase == TurnPhase.Pass) { PerformDiceRoll(); }

            // Request an action from the current player
            currentPlayer.RequestDecision();
        }

        uic.UpdateStepText(Academy.Instance.StepCount);
        uic.UpdateAllPlayers(pm.players);
        Academy.Instance.EnvironmentStep();

    }
    
    void DecideInitialPlayerOrder()
    {
        List<int> firstHalf = new List<int>();
        int r = Random.Range(0, numberOfPlayers);
        for (int i = 0; i < numberOfPlayers; i++)
        {
            int value = (i + r) % numberOfPlayers;
            firstHalf.Add(value);
        }
        List<int> secondHalf = new List<int>(firstHalf);
        secondHalf.Reverse();
        firstHalf.AddRange(secondHalf);
        initialPlayers = firstHalf;
    }

    public void PerformDiceRoll()
    {
        int dice = ThrowDice();
        uic.UpdateDiceRoll(dice);
        Notifier.singleton.Notify("Dice rolled! Outcome: " + dice);
        GiveResourcesToPlayers(dice);
        uic.UpdateAllPlayers(pm.players);
    }

    /// <summary>
    /// Throw 2 dices!
    /// </summary>
    /// <returns> A random number similar to 2 dice throws, summed. </returns>
    private int ThrowDice()
    {
        int dice1 = Random.Range(1, 7);
        int dice2 = Random.Range(1, 7);
        return dice1 + dice2;
    }

    public Building CreateVillageOrCity(GridPoint gp, bool city, ColonyPlayer cp)
    {
        if(gp == null)
        {
            Debug.LogWarning("Cannot create village on null Gridpoint!");
            return null;
        }
        GameObject villageObject = city ? 
            Instantiate(cityPrefab, gp.position, Quaternion.identity, cp.transform) : 
            Instantiate(villagePrefab, gp.position, Quaternion.identity, cp.transform);
        Building b = villageObject.GetComponent<Building>();
        b.Owner = cp;
        cp.AddReward(1f);
        return b;
    }

    /// <summary>
    /// Create a street directed towards some destination GridPoint for a certain Player
    /// </summary>
    /// <param name="dest"> Where the street will go to. </param>
    /// <param name="cp"> For which player the street will be build </param>
    public void CreateStreet(GridPoint dest, ColonyPlayer cp)
    {
        if(dest == null) { Debug.LogError("Cannot create street to null GridPoint!"); return; }

        GridPoint start = null;

        // Find a starting point which either has a building on it, or is connected with a street to the destination
        foreach(KeyValuePair<GridPoint, Building> connection in dest.connectedTo)
        {
            if(connection.Key.building != null && connection.Key.building.Owner == cp)
            {
                start = connection.Key;
                break;
            }
            foreach(KeyValuePair<GridPoint, Building> connection2 in connection.Key.connectedTo)
            {
                if(connection2.Value != null && connection2.Value.Owner == cp)
                {
                    start = connection.Key;
                    break;
                }
            }
            if(start != null) { break; }
        }

        Vector2 position = new Vector2(dest.position.x - start.position.x, dest.position.y - start.position.y);
        float rotation = dest.position.y > start.position.y ? 30f : -30f;
        GameObject street = Instantiate(streetPrefab, position, Quaternion.Euler(0, 0, rotation), cp.transform);
        street.GetComponent<Building>().Owner = cp;
    }

    void GiveResourcesToPlayers(int diceRoll)
    {
        List<Tile> relevantTiles = bc.GetTilesByNumber(diceRoll); // Get the tiles with this number
        foreach(Tile t in relevantTiles)
        {
            List<GridPoint> neighbouringGridPoints = t.gridPoint.GetNeighbouringGridPoints(); // Get all tiles where there is possibly a building
            foreach(GridPoint gp in neighbouringGridPoints)
            {
                if(gp.building != null)
                {
                    Building b = gp.building;
                    int number = b.Type == BuildingType.Village ? 1 : 2;
                    b.Owner.GiveResources(t.resource, number);
                }
            }
        }
    }

    public void EndGame(ColonyPlayer winner)
    {
        Notifier.singleton.Notify("Game Ended!");
        Notifier.singleton.Notify(winner.name + " has won!");
        winner.AddReward(10f);
        pm.SetAllPlayersDone();
    }
}
