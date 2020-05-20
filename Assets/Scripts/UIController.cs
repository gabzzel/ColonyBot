using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

public class UIController : MonoBehaviour
{

    public static UIController singleton = null;

    public List<GameObject> players = new List<GameObject>();
    private GameController gc = null;
    [SerializeField] private Text diceRollText = null;
    [SerializeField] private Text stepText = null;

    private int steps = 0;

    private void Awake()
    {
        if(singleton == null) { singleton = this; }
        else { Destroy(this); }
        gc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();        
    }

    public void Initialize(List<ColonyPlayer> ps)
    {
        // Set the player UI active based on the number of players
        for (int i = 0; i < players.Count; i++)
        {
            players[i].SetActive(i < GameController.singleton.numberOfPlayers);
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
    public void UpdateAllPlayers(List<ColonyPlayer> ps)
    {
        for (int i = 0; i < GameController.singleton.numberOfPlayers; i++)
        {
            UpdatePlayer(players[i], ps[i]);
        }
    }

    /// <summary>
    /// Updates the UI of a specific player
    /// </summary>
    /// <param name="player"> The Gameobject that parents all UI concerning the player. </param>
    /// <param name="p"> The Player Object that contains all info of the player. </param>
    void UpdatePlayer(GameObject player, ColonyPlayer p)
    {
        Image background = player.transform.GetChild(0).GetComponent<Image>();
        PlayerUI pui = player.GetComponent<PlayerUI>();

        background.color = p.color;
        background.SetAllDirty();
        pui.name.text = p.name;
        pui.points.text = "P: " + p.Points;
        pui.wood.text = "Wd: " + p.resources[Resource.Wood];
        pui.ore.text = "Or: " + p.resources[Resource.Ore];
        pui.wool.text = "Wl: " + p.resources[Resource.Wool];
        pui.grain.text = "Gr: " + p.resources[Resource.Grain];
        pui.stone.text = "St: " + p.resources[Resource.Stone];
    }

    public void NotifyOfBuilding(int i, Building b)
    {
        if(b == null) { throw new System.NullReferenceException("Building b is null!"); }
        PlayerUI pui = players[i].GetComponent<PlayerUI>();
        Text text = pui.buildings;
        text.text += b.ToString() + " #" + steps + "\n";
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

    public void UpdateStepText(int steps)
    {
        stepText.text = "Steps: (#" + steps + ")";
        this.steps = steps;
    }
}
