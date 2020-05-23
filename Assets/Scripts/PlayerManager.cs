using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager singleton = null;

    public List<ColonyPlayer> players = new List<ColonyPlayer>();
    private int max = 4;
    private int currentPlayer = 0;
    private Queue<int> queue = new Queue<int>();

    private void Awake()
    {
        if (singleton == null) { singleton = this; }
        else { Destroy(this); }
    }

    public ColonyPlayer CurrentPlayer { get { return players[currentPlayer]; } }
    public int CurrentPlayerNumber { get { return currentPlayer; } }
    public int NextPlayerNumber
    {
        get
        {
            int num = currentPlayer + 1;
            if(num >= max) { return 0; }
            return num;
        }
    }
    public int PreviousPlayerNumber
    {
        get
        {
            int num = currentPlayer - 1;
            if(num < 0) { return max - 1; }
            return num;
        }
    }

    public void Initialize(int numberOfPlayers)
    {
        if (numberOfPlayers < 1 || numberOfPlayers > 4) { throw new Exception(numberOfPlayers + " is not a valid number of players!"); }

        for (int i = 0; i < players.Count; i++)
        {
            players[i].gameObject.SetActive(i < numberOfPlayers);
            players[i].ResetAll();
        }

        max = numberOfPlayers;

        List<int> firstHalf = new List<int>();
        int r = UnityEngine.Random.Range(0, numberOfPlayers);
        for (int i = 0; i < numberOfPlayers; i++)
        {
            int value = (i + r) % numberOfPlayers;
            firstHalf.Add(value);
            firstHalf.Add(value);
        }
        List<int> secondHalf = new List<int>(firstHalf);
        secondHalf.Reverse();
        firstHalf.AddRange(secondHalf);
        foreach(int i in firstHalf)
        {
            queue.Enqueue(i);
        }
        queue.Enqueue(firstHalf[firstHalf.Count - 1]); // Make sure that the player that goes first actually goes first after the initial placements
        currentPlayer = queue.Dequeue();
    }

    public void StartTrade() { CurrentPlayer.trader.StartTrading(); }

    public void RequestAction()
    {
        // Request an action from our current player 
        if (Academy.IsInitialized) { CurrentPlayer.RequestDecision(); }
        else { Notifier.singleton.Notify(CurrentPlayer.name + " action requested, Academy Instance not Initialized."); }

        // If we are in the initial queue
        if (queue.Count > 0)
        {
            currentPlayer = queue.Dequeue(); // Update the current player
        }
    }

    public void NotifyOfPass()
    {
        if(queue.Count == 0)
        {
            currentPlayer = NextPlayerNumber;
        }       

        Notifier.singleton.Notify(CurrentPlayer.name + "'s turn!");

        if(queue.Count == 0)
        {
            GameController.singleton.PerformDiceRoll();
        }
    }

    public void SetAllPlayersDone() { foreach (ColonyPlayer cp in players) { cp.EndEpisode(); } }

    public void NotifyOfWin(ColonyPlayer player)
    {
        GameController.singleton.EndGame(player);
    }
}
