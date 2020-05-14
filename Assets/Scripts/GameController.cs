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
        // 3. Initialize UI of the players
        uic.Initialize(pm.players);
        //InitialPlacements();
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
            //NextStep();
        }
        else
        {
            EndGame(winner);
        }
    }

    public void NextStep()
    {
        // Perform 1 step / round where every player gets a turn and dices are rolled before their turns
        ColonyPlayer currentPlayer = pm.CurrentPlayer;

        // If the current player passed last turn, roll the dice
        if(currentPlayer.turnPhase == TurnPhase.Pass) { PerformDiceRoll(); }

        // Request an action from the current player
        currentPlayer.RequestAction();

        uic.UpdateStepText(Academy.Instance.StepCount);
        uic.UpdateAllPlayers(pm.players);
        Academy.Instance.EnvironmentStep();
    }
    
    void InitialPlacements()
    {
        int r = Random.Range(0, numberOfPlayers);
        pm.currentPlayer = r;
        Notifier.singleton.Notify("Player " + (r + 1) + " may go first.");

        pm.players[pm.currentPlayer].RequestDecision();

        /*

        // Get a position from the players
        for (int i = 0; i < pm.players.Count; i++)
        {
            //GridPoint gp = pm.RequestBuildingPosition(bc.GetPossibleBuildingSites(pm.players[pm.currentPlayer], true));
            CreateVillage(gp);
            string notification = "Player " + (pm.currentPlayer + 1) + " - Village @ " + gp.colRow;
            Notifier.singleton.Notify(notification);
            if (i < pm.players.Count - 1) { pm.NextPlayer(); }
        }

        for (int i = 0; i < pm.players.Count; i++)
        {
            //GridPoint gp = pm.RequestBuildingPosition(bc.GetPossibleBuildingSites(pm.players[pm.currentPlayer], true));
            CreateVillage(gp);
            string notification = "Player " + (pm.currentPlayer + 1) + " - Village @ " + gp.colRow;
            Notifier.singleton.Notify(notification);
            if (pm.currentPlayer != r) { pm.PreviousPlayer(); }
        }

        */
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

    public void CreateVillageOrCity(GridPoint gp, bool city)
    {
        if(gp == null)
        {
            Debug.LogWarning("Cannot create village on null Gridpoint!");
            return;
        }
        ColonyPlayer currentPlayer = pm.players[pm.currentPlayer];
        GameObject villageObject = city ? 
            Instantiate(cityPrefab, gp.position, Quaternion.identity, currentPlayer.transform) : 
            Instantiate(villagePrefab, gp.position, Quaternion.identity, currentPlayer.transform);
        Building b = villageObject.GetComponent<Building>();
        b.Owner = currentPlayer;
        currentPlayer.AddReward(1f);
    }

    public void CreateStreet(GridPoint dest)
    {
        if(dest == null) { Debug.LogError("Cannot create street to null GridPoint!"); return; }
        ColonyPlayer player = pm.CurrentPlayer;

        GridPoint start = null;

        // Find a starting point which either has a building on it, or is connected with a street
        foreach(KeyValuePair<GridPoint, Building> connection in dest.connectedTo)
        {
            if(connection.Key.building != null && connection.Key.building.Owner == player)
            {
                start = connection.Key;
                break;
            }
            foreach(KeyValuePair<GridPoint, Building> connection2 in connection.Key.connectedTo)
            {
                if(connection2.Value != null && connection2.Value.Owner == player)
                {
                    start = connection.Key;
                    break;
                }
            }
            if(start != null) { break; }
        }

        Vector2 position = new Vector2(dest.position.x - start.position.x, dest.position.y - start.position.y);
        float rotation = dest.position.y > start.position.y ? 30f : -30f;
        GameObject street = Instantiate(streetPrefab, position, Quaternion.Euler(0, 0, rotation), player.transform);
        street.GetComponent<Building>().Owner = player;
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
