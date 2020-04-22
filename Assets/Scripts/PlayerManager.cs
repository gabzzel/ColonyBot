using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public List<Player> players = new List<Player>();
    public int playerInControl = 0;
    private List<string> defaultNames = new List<string>() { "Jip", "Janneke", "Bob", "Willempje" };

    public void Initialize(int numberOfPlayers)
    {
        if(numberOfPlayers < 2 || numberOfPlayers > 4)
        {
            Debug.LogWarning(numberOfPlayers + " is not a valid number of player!");
            return;
        }

        for (int i = 0; i < numberOfPlayers; i++)
        {
            players.Add(CreatePlayer(defaultNames[i]));
        }
    }

    Player CreatePlayer(string name)
    {
        GameObject playerObj = new GameObject(name);
        playerObj.AddComponent(typeof(Player));
        Player p = playerObj.GetComponent<Player>();
        p.name = name;
        return p;
    }

    public Enums.Action RequestAction()
    {
        return players[playerInControl].RequestAction();
    }

    public string GetCurrentPlayerName()
    {
        return players[0].name;
    }
}
