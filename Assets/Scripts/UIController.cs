using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.UI;
using static Utility;

public class UIController : MonoBehaviour
{
    public static UIController singleton = null;

    public List<GameObject> players = new List<GameObject>();
    [SerializeField] private Text diceRollText = null;
    [SerializeField] private Text stepText = null;
    [SerializeField] private GameObject LogContainer = null;

    private void Awake()
    {
        if(singleton == null) { singleton = this; }
        else { Destroy(this); }
    }

    public void Initialize(List<ColonyPlayer> ps)
    {
        // Set the player UI active based on the number of players
        for (int i = 0; i < players.Count; i++)
        {
            players[i].SetActive(i < GameController.singleton.numberOfPlayers && GameController.singleton.showUI);
        }

        LogContainer.SetActive(GameController.singleton.showUI);

        UpdateAllPlayers(ps);
        UpdateDiceRoll(0);
    }

    /// <summary>
    /// Updates the UI of all players
    /// </summary>
    /// <param name="ps"> The list of all players </param>
    public void UpdateAllPlayers(List<ColonyPlayer> ps)
    {
        if (!GameController.singleton.showUI) { return; }
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
        PlayerUI pui = player.GetComponent<PlayerUI>();

        pui.name.text = p.name;
        pui.name.color = p.color;
        pui.points.text = "P: " + p.Points;
        pui.wood.text = "L: " + p.resources[Lumber];
        pui.ore.text = "O: " + p.resources[Ore];
        pui.wool.text = "W: " + p.resources[Wool];
        pui.grain.text = "G: " + p.resources[Grain];
        pui.stone.text = "B: " + p.resources[Brick];
        //pui.knights.text = "K: " + p.availableKnights + " / " + p.usedKnights;
        //pui.knights.color = p.LargestArmy ? Color.green : Color.black;
        //pui.VPC.text = "VPC: " + p.developmentPoints;
    }

    public void NotifyOfBuilding(int playerID, string message)
    {
        PlayerUI pui = players[playerID].GetComponent<PlayerUI>();
        Text text = pui.buildings;
        text.text += message + " #" + Academy.Instance.StepCount + "\n";
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

    public void UpdateStepText()
    {
        stepText.text = "E " + (Academy.Instance.EpisodeCount - 1)+ " S " + Academy.Instance.StepCount;
    }

    public void ShowUI(bool value)
    {
        foreach(GameObject player in players)
        {
            player.SetActive(value);
        }
    }
}
