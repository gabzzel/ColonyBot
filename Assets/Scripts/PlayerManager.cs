using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using static Enums;
using Unity.MLAgents.Policies;

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
            players[i].Initialize();
            players[i].GetComponent<BehaviorParameters>().TeamId = i;
        }
    }

    public void RequestAction()
    {
        if (Academy.Instance.IsCommunicatorOn)
        {
            players[currentPlayer].RequestDecision();
        }
        else
        {
            Notifier.singleton.Notify(GetCurrentPlayerName() + " action requested, but no communicator present.");
        }
    }

    public string GetCurrentPlayerName()
    {
        return players[currentPlayer].name;
    }

    public void NextPlayer(bool notify = true)
    {
        currentPlayer++;
        if(currentPlayer >= GameController.singleton.numberOfPlayers) { currentPlayer = 0; }
        if (notify) { Notifier.singleton.Notify("Player " + (currentPlayer + 1) + "'s turn!"); }
    }

    public void PreviousPlayer(bool notify = true)
    {
        currentPlayer--;
        if(currentPlayer < 0) { currentPlayer = GameController.singleton.numberOfPlayers - 1; }
        if (notify) { Notifier.singleton.Notify("Player " + (currentPlayer + 1) + "'s turn!"); }
    }

    public ColonyPlayer PlayerHasWon()
    {
        foreach(ColonyPlayer cp in players)
        {
            if(cp.points >= 12)
            {
                return cp;
            }
        }
        return null;
    }

    public void SetAllPlayersDone()
    {
        foreach(ColonyPlayer cp in players)
        {
            cp.EndEpisode();
        }
    }

}
