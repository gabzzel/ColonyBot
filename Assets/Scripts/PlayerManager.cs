using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager singleton = null;

    public List<ColonyPlayer> players = new List<ColonyPlayer>();
    public int currentPlayer = 0;

    private void Awake()
    {
        if(singleton == null) { singleton = this; }
        else { Destroy(this); }
    }

    public ColonyPlayer CurrentPlayer
    {
        get { return players[currentPlayer]; }
    }

    public void Initialize(int numberOfPlayers)
    {
        if(numberOfPlayers < 1 || numberOfPlayers > 4) { throw new Exception(numberOfPlayers + " is not a valid number of players!"); }

        for (int i = 0; i < players.Count; i++)
        {
            players[i].gameObject.SetActive(i < numberOfPlayers);
            players[i].ResetAll();
        }
    }

    public void RequestAction()
    {
        if (Academy.Instance.IsCommunicatorOn) { CurrentPlayer.RequestDecision(); }
        else { Notifier.singleton.Notify(GetCurrentPlayerName() + " action requested, but no communicator present."); }
    }

    public string GetCurrentPlayerName()
    {
        return CurrentPlayer.name;
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
        foreach(ColonyPlayer cp in players) { if(cp.Points >= 12f) { return cp; } }
        return null;
    }

    public void SetAllPlayersDone() { foreach(ColonyPlayer cp in players) { cp.EndEpisode(); } }
}
