﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private GameObject playerParent = null;

    public List<Player> players = new List<Player>();
    public int playerInControl = 0;
    private List<string> defaultNames = new List<string>() { "Jip", "Janneke", "Bob", "Willempje" };
    private List<Color> defaultColors = new List<Color>();

    private void Awake()
    {
        defaultColors = new List<Color>() { new Color(51, 153, 255), new Color(255, 102, 102), new Color(0, 204, 0), Color.yellow };
    }

    public void Initialize(int numberOfPlayers)
    {

        CreatePlayerParent();
        players.Clear();

        if(numberOfPlayers < 1 || numberOfPlayers > 4)
        {
            Debug.LogWarning(numberOfPlayers + " is not a valid number of player!");
            return;
        }

        for (int i = 0; i < numberOfPlayers; i++)
        {
            players.Add(CreatePlayer(defaultNames[i], defaultColors[i]));
        }
    }

    void CreatePlayerParent()
    {
        if(playerParent != null)
        {
            if (Application.isPlaying) { Destroy(playerParent); }
            else { DestroyImmediate(playerParent); }
        }

        playerParent = new GameObject("Players");
    }

    Player CreatePlayer(string name, Color color)
    {
        GameObject playerObj = new GameObject(name);
        playerObj.transform.parent = playerParent.transform;

        playerObj.AddComponent(typeof(Player));
        Player p = playerObj.GetComponent<Player>();
        p.Initialize(name, color);
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
