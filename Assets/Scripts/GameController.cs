using UnityEngine;

public class GameController : MonoBehaviour
{
    public int pointsToWin = 12;
    public int numberOfPlayers = 3;
    private BoardController bc = null;
    private PlayerManager pm = null;

    private void Awake()
    {
        bc = GetComponent<BoardController>();
        pm = GetComponent<PlayerManager>();
    }

    private void Start()
    {
        bc.CreateFilledBoard();
        pm.Initialize(numberOfPlayers);

        int diceResult = ThrowDice();
        Debug.Log("We threw " + diceResult);
        foreach(Tile t in bc.GetTilesByNumber(diceResult))
        {
            Debug.Log(t.ToString());
        }
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
