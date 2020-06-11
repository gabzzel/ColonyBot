using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using static Utility;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager singleton = null;

    public List<ColonyPlayer> players = new List<ColonyPlayer>();
    private int max = 4;
    private int currentPlayer = 0;
    private readonly Queue<int> queue = new Queue<int>();

    private void Awake()
    {
        if (singleton == null) { singleton = this; }
        else { Destroy(this); }
    }

    public ColonyPlayer CurrentPlayer { get { return players[currentPlayer]; } }
    public int NextPlayerNumber
    {
        get
        {
            int num = currentPlayer + 1;
            if(num >= max) { return 0; }
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

    public void StartTrade() { if (CurrentPlayer.Buildings.Count >= 4) { CurrentPlayer.trader.StartTrading(); } }

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

    public void RemoveRobberResources()
    {
        foreach(ColonyPlayer player in players)
        {
            if(player.resources == null || player.TotalResources <= 7) { continue; }
            int toRemove = Mathf.FloorToInt(player.TotalResources / 2f);

            for (int i = 0; i < toRemove; i++)
            {
                int removed = player.RemoveResource();
                if(removed != Desert) { GameController.singleton.availableResources[removed]++; }
            }
        }
    }

    /*
    public void CheckLargestArmy()
    {
        // Get the player with the largest army
        ColonyPlayer player = null;
        foreach(ColonyPlayer p in players)
        {
            if (p.LargestArmy) { player = p; }
        }

        // If there is no such player, the current player has the largest army!
        if(player == null) { CurrentPlayer.LargestArmy = true; }

        // If there is a player with the largest army that is not the current player and the number of used knights of that player is lower than the one of the current player...
        else if(player != CurrentPlayer && CurrentPlayer.usedKnights > player.usedKnights)
        {
            player.LargestArmy = false;
            CurrentPlayer.LargestArmy = true;
        }
    }

    */
    public List<ColonyPlayer> GetPlayersInOrder()
    {
        List<ColonyPlayer> result = new List<ColonyPlayer>() { CurrentPlayer };
        int number = currentPlayer;
        for (int i = 0; i < 3; i++)
        {
            number++;
            if(number >= GameController.singleton.numberOfPlayers)
            {
                number = 0;
            }
            result.Add(players[number]);
        }
        return result;
    }
}
