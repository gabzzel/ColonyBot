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
            pm.CurrentPlayer.RequestDecision();
            initialPlayers.RemoveAt(initialPlayers.Count - 1);
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

    public Building CreateVillageOrCity(NonTileGridPoint ntgp, bool city, ColonyPlayer cp)
    {
        if(ntgp == null) { throw new System.Exception("Cannot create village on null Gridpoint!"); }
        GameObject villageObject = city ? 
            Instantiate(cityPrefab, ntgp.position, Quaternion.identity, cp.transform) : 
            Instantiate(villagePrefab, ntgp.position, Quaternion.identity, cp.transform);
        Building building = villageObject.GetComponent<Building>();
        building.Owner = cp;
        ntgp.Building = building;
        cp.AddReward(1f);
        return building;
    }

    /// <summary>
    /// Create a street directed towards some destination GridPoint for a certain Player
    /// </summary>
    /// <param name="dest"> Where the street will go to. </param>
    /// <param name="cp"> For which player the street will be build </param>
    public void CreateStreet(NonTileGridPoint dest, ColonyPlayer cp, NonTileGridPoint start = null)
    {
        if(dest == null) { throw new System.Exception("Cannot create street to null GridPoint!"); }

        start = dest.FindConnectionWithPlayer(cp);

        if(start == null) { throw new System.Exception("Cannot build a street from nowhere..."); }

        Vector2 position = new Vector2((dest.position.x - start.position.x) / 2f, (dest.position.y - start.position.y) / 2f) + start.position;
        float rotation = 90f;
        if(dest.position.x < start.position.x && start.position.y < dest.position.y || start.position.y > dest.position.y && dest.position.x > start.position.x) { rotation = -30f; }
        else if(dest.position.x > start.position.x && start.position.y < dest.position.y || start.position.y > dest.position.y && start.position.x > dest.position.x)
        {
            rotation = 30f;
        }
        GameObject street = Instantiate(streetPrefab, position, Quaternion.Euler(0, 0, rotation), cp.transform);
        Building b = street.GetComponent<Building>();
        b.Owner = cp;
        dest.Connect(start, b);
        start.Connect(dest, b);
    }

    void GiveResourcesToPlayers(int diceRoll)
    {
        List<Tile> relevantTiles = bc.GetTilesByNumber(diceRoll); // Get the tiles with this number
        foreach(Tile t in relevantTiles)
        {
            HashSet<NonTileGridPoint> neighbouringGridPoints = t.GridPoint.ConnectedNTGPs; // Get all tiles where there is possibly a building
            foreach(NonTileGridPoint ntgp in neighbouringGridPoints)
            {
                if(ntgp.Building != null)
                {
                    Building b = ntgp.Building;
                    int number = b.Type == BuildingType.Village ? 1 : 2;
                    b.Owner.GiveResources(t.Resource, number);
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
