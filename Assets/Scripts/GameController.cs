using UnityEngine;
using System.Collections.Generic;
using static Enums;

public class GameController : MonoBehaviour
{
    public int pointsToWin = 12;
    public int numberOfPlayers = 3;
    [SerializeField] private GameObject villagePrefab = null;
    private BoardController bc = null;
    private PlayerManager pm = null;
    private UIController uic = null;

    private void Awake()
    {
        bc = GetComponent<BoardController>();
        pm = GetComponent<PlayerManager>();
        uic = GameObject.FindGameObjectWithTag("UIController").GetComponent<UIController>();
    }

    public void NewGame()
    {
        Debug.Log("New game started!");
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
        // Get a position from the players
        for (int i = 0; i < pm.players.Count; i++)
        {
            GridPoint gp = pm.RequestBuildingPosition(bc.GetPossibleBuildingSites(i, true));
            CreateVillage(gp);
        }
    }

    public void PerformDiceRoll()
    {
        int dice = ThrowDice();
        uic.UpdateDiceRoll(dice);
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
        Player currentPlayer = pm.players[pm.playerInControl];
        GameObject villageObject = Instantiate(villagePrefab, gp.position, Quaternion.identity, currentPlayer.transform);
        Building b = villageObject.GetComponent<Building>();
        b.Initialize(currentPlayer, gp);
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
                    int number = 1;
                    if (b.city) { number = 2; }
                    b.owner.AddToResource(t.resource, number);
                }
            }
        }
    }
}
