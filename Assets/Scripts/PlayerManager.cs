using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Enums;

public class PlayerManager : MonoBehaviour
{
    public List<ColonyPlayer> players = new List<ColonyPlayer>();
    public int currentPlayer = 0;

    public void Initialize(int numberOfPlayers)
    {
        if(numberOfPlayers < 1 || numberOfPlayers > 4)
        {
            Debug.LogWarning(numberOfPlayers + " is not a valid number of players!");
            return;
        }

        for (int i = 0; i < players.Count; i++)
        {
            players[i].gameObject.SetActive(i < numberOfPlayers);
        }
    }

    public string GetCurrentPlayerName()
    {
        return players[currentPlayer].name;
    }

    public void NextPlayer(bool notify = true)
    {
        currentPlayer++;
        if(currentPlayer >= players.Count) { currentPlayer = 0; }
        if (notify) { Notifier.singleton.Notify("Player " + (currentPlayer + 1) + "'s turn!"); }
    }

    public void PreviousPlayer(bool notify = true)
    {
        currentPlayer--;
        if(currentPlayer < 0) { currentPlayer = players.Count - 1; }
        if (notify) { Notifier.singleton.Notify("Player " + (currentPlayer + 1) + "'s turn!"); }
    }

}
