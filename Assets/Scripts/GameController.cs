using UnityEngine;
using System.Collections.Generic;
using static Utility;
using Unity.MLAgents;
using Unity.Mathematics;
using Random = UnityEngine.Random;
public class GameController : MonoBehaviour
{
    public static GameController singleton = null;

    [Range(6, 12, order = 1)] public int pointsToWin = 12;
    [Range(1, 4, order = 1)] public int numberOfPlayers = 3;
    public bool showUI = true;
    public float stepTime = 1f;
    private float stepTimer = 0f;

    [SerializeField] private GameObject villagePrefab = null;
    [SerializeField] private GameObject cityPrefab = null;
    public GameObject streetPrefab = null;
    [SerializeField] private GameObject robber = null;

    private BoardController bc = null;
    private PlayerManager pm = null;
    private UIController uic = null;

    [SerializeField] private bool gameStarted = false;
    public int[] availableResources = new int[5];
    [SerializeField] private int[] developmentCards = new int[3];

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
        bc.useStandard = ep.GetWithDefault("standard_board", 0f) == 1;
        bc.allowHighChanceNeighbours = ep.GetWithDefault("allow_high_chance_neighbours", 0f) == 1;
        stepTime = Mathf.Max(ep.GetWithDefault("step_time", 1f), 0);
    }

    public void NewGame()
    {
        if (!Academy.IsInitialized)
        {
            throw new System.Exception("Cannot start game without an initialized academy!");
        }

        availableResources = new int[] { 19, 19, 19, 19, 19 };
        developmentCards = new int[] { 14, 5, 6 };
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

        uic.UpdateStepText();
        uic.UpdateAllPlayers(pm.players);
        Academy.Instance.EnvironmentStep();

    }

    public void PerformDiceRoll()
    {
        int dice = ThrowDice();
        uic.UpdateDiceRoll(dice);
        Notifier.singleton.Notify("Dice rolled! Outcome: " + dice);
        if(dice != 7) { GiveResourcesToPlayers(dice); }
        else { ExecuteRobberPhase(false); }
        uic.UpdateAllPlayers(pm.players);
    }

    public void ExecuteRobberPhase(bool knight)
    {
        // Remove half of the resources (rounded down) of every player, if we are not playing a knight
        if (!knight) { pm.RemoveRobberResources(); }

        // Move the robber to the best next tile
        TileGridPoint bestLocation = bc.GetBestTileForRobber(pm.CurrentPlayer);
        bc.RobberLocation = bestLocation;
        robber.transform.position = bestLocation.position;

        // Draw 1 resource of 1 random player and give it to the current player
        ColonyPlayer randomPlayer = bc.GetRandomPlayerConnectedToTile(bestLocation, pm.CurrentPlayer);
        int removedResource = randomPlayer.RemoveResource();
        if (removedResource != Desert) { pm.CurrentPlayer.resources[removedResource]++; }
        Notifier.singleton.Notify(pm.CurrentPlayer.name + " took " + ResourceNames[removedResource] + " from " + randomPlayer.name);
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

    public void DrawDevelopmentCard(ColonyPlayer player)
    {
        int total = 0;
        foreach(int x in developmentCards) { total += x; }
        int random = Random.Range(0, total);
        total = 0;
        for (int i = 0; i < developmentCards.Length; i++)
        {
            total += developmentCards[i];
            if(total > random) { total = i; break; }
        }
        
        if(total == Knight) { player.availableKnights++; developmentCards[Knight]--; }
        else if(total == VictoryPoint) { player.AddReward(1); player.developmentPoints++; developmentCards[VictoryPoint]--; }
        availableResources[Ore]++; availableResources[Wool]++; availableResources[Grain]++;
        // If we draw an ususable card, we do nothing!
    }

    public Building CreateVillageOrCity(NonTileGridPoint ntgp, bool city, ColonyPlayer cp, bool initial)
    {
        if (ntgp == null) { throw new System.Exception("Cannot create village on null Gridpoint!"); }

        GameObject villageObject;
        if (city)
        {
            if (ntgp.Building == null || ntgp.Building.Type != Village)
            {
                throw new System.Exception("Cannot build a city on " + ntgp.ToString() + " because there is no village here.");
            }
            Destroy(ntgp.Building.gameObject); // Destroy the current village here.
            villageObject = Instantiate(cityPrefab, ntgp.position, Quaternion.identity, cp.transform);
            availableResources[Ore] += 3; availableResources[Grain] += 2;
        }
        else
        {
            if (ntgp.Building != null)
            {
                throw new System.Exception("Cannot build a village on " + ntgp.ToString() + " because there is already something there!");
            }
            villageObject = Instantiate(villagePrefab, ntgp.position, Quaternion.identity, cp.transform);
            if (!initial) { availableResources[Stone] += 1; availableResources[Wood] += 1; availableResources[Grain] += 1; availableResources[Wool] += 1; }
        }

        Building building = villageObject.GetComponent<Building>();
        building.Owner = cp; // Set the correct owner
        ntgp.Building = building; // Set the gridpoint reference to the new building
        building.Position = ntgp;
        cp.AddReward(1f); // Add a reward to the player
        uic.NotifyOfBuilding(cp.ID, building.Type.ToString()[0] + " @ " + ntgp.index);
        return building;
    }

    /// <summary>
    /// Create a street directed towards some destination GridPoint for a certain Player
    /// </summary>
    /// <param name="dest"> Where the street will go to. </param>
    /// <param name="cp"> For which player the street will be build </param>
    public Building CreateStreet(NonTileGridPoint dest, ColonyPlayer cp, NonTileGridPoint start, bool initial)
    {
        if (dest == null) { throw new System.Exception("Cannot create street to null GridPoint!"); }

        if (start == null) { start = dest.OccupiedNeighbour(cp.ID); }

        if (start == null) { start = dest.NeighbourWithConnectionToPlayer(cp.ID); }

        if (start == null) { throw new System.Exception("Cannot build a street from nowhere!"); }

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
        dest.Connect(start.index, b);
        start.Connect(dest.index, b);
        uic.NotifyOfBuilding(cp.ID, "S : " + start.index + " to " + dest.index);
        if (!initial) { availableResources[Wood] += 1; availableResources[Stone] += 1; }
        return b;
    }

    void GiveResourcesToPlayers(int diceRoll)
    {
        List<Tile> relevantTiles = bc.GetTilesByNumber(diceRoll); // Get the tiles with this number
        foreach (Tile t in relevantTiles)
        {
            // If the currentgridpoint has the robber on it, don't do anything
            if (t.GridPoint.Robber) { continue; }
            HashSet<int> neighbouringGridPoints = t.GridPoint.connectedIndexes; // Get all tiles where there is possibly a building
            foreach (int ntgpIndex in neighbouringGridPoints)
            {
                if (BoardController.ntgpIndexes.Contains(ntgpIndex))
                {
                    NonTileGridPoint ntgp = (NonTileGridPoint)BoardController.singleton.allGridPoints[ntgpIndex];
                    // If we don't have enough resources to pay someone, we don't. 
                    if (ntgp.Building != null && availableResources[t.Resource] >= ntgp.Building.Type)
                    {
                        Building b = ntgp.Building;
                        b.Owner.GiveResources(t.Resource, b.Type);
                        availableResources[t.Resource] -= b.Type;
                    }
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
