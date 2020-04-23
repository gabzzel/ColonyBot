using UnityEngine;

public class GameController : MonoBehaviour
{
    public int pointsToWin = 12;
    public int numberOfPlayers = 3;
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
    }

    public void PerformDiceRoll()
    {
        int dice = ThrowDice();
        uic.UpdateDiceRoll(dice);
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

}
