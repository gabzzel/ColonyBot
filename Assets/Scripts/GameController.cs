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
    public GameObject streetPrefab = null;
    private BoardController bc = null;
    private PlayerManager pm = null;
    private UIController uic = null;

    [SerializeField] private bool gameStarted = false;

    public bool GameStarted { get { return gameStarted; } }

    private void Awake()
    {
        if (singleton == null) { singleton = this; }
        else { Destroy(this.gameObject); }

        bc = GetComponent<BoardController>();
        pm = GetComponent<PlayerManager>();
        uic = GameObject.FindGameObjectWithTag("UIController").GetComponent<UIController>();

        Academy a = Academy.Instance;
        Academy.Instance.AutomaticSteppingEnabled = false;
        Academy.Instance.OnEnvironmentReset += OnEnvironmentReset;
        
    }

    private void OnEnvironmentReset()
    {
        NewGame();
    }

    private void Start()
    {
        if (Academy.IsInitialized && Academy.Instance.IsCommunicatorOn)
        {
            LoadSettings();            
        }
        Academy.Instance.EnvironmentStep();
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
        if (!Academy.IsInitialized)
        {
            throw new System.Exception("Cannot start game without an initialized academy!");
        }

        Notifier.singleton.Notify("New Game Started!");
        // 1. Create a new board
        bc.CreateFilledBoard();
        // 2. Initialize players and set the initial player order
        pm.Initialize(numberOfPlayers);
        // 2.1 Destroy all buildings
        for (int i = GameObject.FindGameObjectsWithTag("Building").Length - 1; i >= 0; i--)
        {
            GameObject building = GameObject.FindGameObjectsWithTag("Building")[i];
            Destroy(building);
        }
        // 3. Initialize UI of the players
        uic.Initialize(pm.players);
        gameStarted = true;
    }

    private void FixedUpdate()
    {
        if (gameStarted)
        {
            stepTimer += Time.fixedDeltaTime;
            if (stepTimer >= stepTime)
            {
                NextStep();
                stepTimer = 0f;
            }
        }
    }

    public void NextStep()
    {
        // Start a lighting fast automatic trade round
        pm.StartTrade();
        // Request an action from the current player. This also gives the turn to the next player and rolls the dice
        pm.RequestAction();        

        uic.UpdateStepText(Academy.Instance.StepCount);
        uic.UpdateAllPlayers(pm.players);
        Academy.Instance.EnvironmentStep();

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
        if (ntgp == null) { throw new System.Exception("Cannot create village on null Gridpoint!"); }

        GameObject villageObject = null;

        if (city)
        {
            if (ntgp.Building == null || ntgp.Building.Type != BuildingType.Village)
            {
                throw new System.Exception("Cannot build a city on " + ntgp.ToString() + " because there is no village here.");
            }
            Destroy(ntgp.Building.gameObject); // Destroy the current village here.
            villageObject = Instantiate(cityPrefab, ntgp.position, Quaternion.identity, cp.transform);
        }
        else
        {
            if (ntgp.Building != null)
            {
                throw new System.Exception("Cannot build a village on " + ntgp.ToString() + " because there is already something there!");
            }
            villageObject = Instantiate(villagePrefab, ntgp.position, Quaternion.identity, cp.transform);
        }

        Building building = villageObject.GetComponent<Building>();
        building.Owner = cp; // Set the correct owner
        ntgp.Building = building; // Set the gridpoint reference to the new building
        building.Position = ntgp;
        cp.AddReward(1f); // Add a reward to the player
        uic.NotifyOfBuilding(pm.players.IndexOf(cp), building);
        return building;
    }

    /// <summary>
    /// Create a street directed towards some destination GridPoint for a certain Player
    /// </summary>
    /// <param name="dest"> Where the street will go to. </param>
    /// <param name="cp"> For which player the street will be build </param>
    public Building CreateStreet(NonTileGridPoint dest, ColonyPlayer cp, NonTileGridPoint start = null)
    {
        if (dest == null) { throw new System.Exception("Cannot create street to null GridPoint!"); }

        start = dest.FindConnectionWithPlayer(cp);

        if (start == null) { throw new System.Exception("Cannot build a street from nowhere..."); }

        Vector2 position = new Vector2((dest.position.x - start.position.x) / 2f, (dest.position.y - start.position.y) / 2f) + start.position;
        float rotation = 90f;
        if (dest.position.x < start.position.x && start.position.y < dest.position.y || start.position.y > dest.position.y && dest.position.x > start.position.x) { rotation = -30f; }
        else if (dest.position.x > start.position.x && start.position.y < dest.position.y || start.position.y > dest.position.y && start.position.x > dest.position.x)
        {
            rotation = 30f;
        }
        GameObject street = Instantiate(streetPrefab, position, Quaternion.Euler(0, 0, rotation), cp.transform);
        Building b = street.GetComponent<Building>();
        b.Owner = cp;
        dest.Connect(start, b);
        start.Connect(dest, b);
        uic.NotifyOfBuilding(pm.players.IndexOf(cp), b);
        return b;
    }

    void GiveResourcesToPlayers(int diceRoll)
    {
        List<Tile> relevantTiles = bc.GetTilesByNumber(diceRoll); // Get the tiles with this number
        foreach (Tile t in relevantTiles)
        {
            HashSet<NonTileGridPoint> neighbouringGridPoints = t.GridPoint.ConnectedNTGPs; // Get all tiles where there is possibly a building
            foreach (NonTileGridPoint ntgp in neighbouringGridPoints)
            {
                if (ntgp.Building != null)
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
        //winner.AddReward(10f);
        pm.SetAllPlayersDone(); // End the episodes
        gameStarted = false;
    }
}
