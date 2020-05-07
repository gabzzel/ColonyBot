using UnityEngine;
using System.Collections.Generic;
using static Enums;
using Unity.MLAgents;

public class GameController : MonoBehaviour
{
    public int pointsToWin = 12;
    [Range(1, 4, order = 1)] public int numberOfPlayers = 3;

    [SerializeField] private GameObject villagePrefab = null;
    [SerializeField] private GameObject streetPrefab = null;
    private BoardController bc = null;
    private PlayerManager pm = null;
    private UIController uic = null;

    private void Awake()
    {
        bc = GetComponent<BoardController>();
        pm = GetComponent<PlayerManager>();
        uic = GameObject.FindGameObjectWithTag("UIController").GetComponent<UIController>();
        LoadSettings();
        Academy.Instance.OnEnvironmentReset += NewGame;
    }

    public void LoadSettings()
    {
        EnvironmentParameters ep = Academy.Instance.EnvironmentParameters;
        numberOfPlayers = Mathf.FloorToInt(ep.GetWithDefault("number_of_players", 3f));
        pointsToWin = Mathf.FloorToInt(ep.GetWithDefault("points_to_win", 12f));
        bc.useStandard = ep.GetWithDefault("standard_board", 0f) == 1 ? true : false;
        bc.allowHighChanceNeighbours = ep.GetWithDefault("allow_high_chance_neighbours", 0f) == 1 ? true : false;
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
        InitialPlacements();
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

    void CreateVillage(GridPoint gp)
    {
        if(gp == null)
        {
            Debug.LogWarning("Cannot create village on null Gridpoint!");
            return;
        }
        ColonyPlayer currentPlayer = pm.players[pm.currentPlayer];
        GameObject villageObject = Instantiate(villagePrefab, gp.position, Quaternion.identity, currentPlayer.transform);
        Building b = villageObject.GetComponent<Building>();
        b.owner = currentPlayer;
    }

    void CreateStreet(GridPoint start, GridPoint dest)
    {
        if(start == null || dest == null)
        {
            Debug.LogError("Cannot create street between null gridpoint(s)");
            return;
        }
        ColonyPlayer currentPlayer = pm.players[pm.currentPlayer]; // Get the current player
        GameObject streetObject = Instantiate(streetPrefab, start.position - dest.position, Quaternion.identity, currentPlayer.transform); 
        Building b = streetObject.GetComponent<Building>();
        b.owner = currentPlayer; // Set the owner of the street to the current player
        start.connectedTo[dest] = b; // Connect the start to the dest with a street
        dest.connectedTo[start] = b; // Connect the dest to the start with the same street
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
                    int number = b.type == BuildingType.Village ? 1 : 2;
                    b.owner.GiveResources(t.resource, number);
                }
            }
        }
    }

    public void EndGame()
    {
        Notifier.singleton.Notify("Game Ended!");
    }
}
