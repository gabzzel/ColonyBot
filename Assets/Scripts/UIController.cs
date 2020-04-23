using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

public class UIController : MonoBehaviour
{
    public List<GameObject> players = new List<GameObject>();
    private GameController gc = null;
    [SerializeField] private Text diceRollText = null;

    private void Awake()
    {
        if(players.Count < 4)
        {
            players.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                players.Add(transform.GetChild(i).gameObject);
            }
        }

        gc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
    }

    public void Initialize(List<Player> ps)
    {
        // Set the player UI active based on the number of players
        for (int i = 0; i < players.Count; i++)
        {
            players[i].SetActive(i < ps.Count);
        }

        UpdateAllPlayers(ps);
        UpdateDiceRoll(0);
    }

    public void NewGame()
    {
        gc.NewGame();
    }

    /// <summary>
    /// Updates the UI of all players
    /// </summary>
    /// <param name="ps"> The list of all players </param>
    public void UpdateAllPlayers(List<Player> ps)
    {
        for (int i = 0; i < ps.Count; i++)
        {
            UpdatePlayer(players[i], ps[i]);
        }
    }

    /// <summary>
    /// Updates the UI of a specific player
    /// </summary>
    /// <param name="player"> The Gameobject that parents all UI concerning the player. </param>
    /// <param name="p"> The Player Object that contains all info of the player. </param>
    void UpdatePlayer(GameObject player, Player p)
    {
        Image background = player.GetComponent<Image>(); 
        Text name = player.transform.Find("Name").GetComponent<Text>();
        Text points = player.transform.Find("Points").GetComponent<Text>();

        background.color = p.color;
        background.SetAllDirty();
        name.text = "Name: " + p.name;
        points.text = "Points: " + p.points;

        foreach(Resource res in GetResourcesAsList())
        {
            if(res == Resource.None) { continue; }
            Text resText = player.transform.Find(res.ToString()).GetComponent<Text>();
            resText.text = res.ToString() + ": " + p.resources[res];
        }
    }

    public void UpdateDiceRoll(int result)
    {
        if(result == 0)
        {
            diceRollText.text = "Roll dice!";
        }
        else
        {
            diceRollText.text = "Roll dice! \n Previous: " + result;
        }
        
    }
}
