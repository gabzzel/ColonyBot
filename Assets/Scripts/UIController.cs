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
        Image background = player.transform.GetChild(0).GetComponent<Image>();
        PlayerUI pui = player.GetComponent<PlayerUI>();

        background.color = p.color;
        background.SetAllDirty();
        pui.name.text = p.name;
        pui.points.text = "P: " + p.Points;
        pui.wood.text = "Wd: " + p.resources[Wood];
        pui.ore.text = "Or: " + p.resources[Ore];
        pui.wool.text = "Wl: " + p.resources[Wool];
        pui.grain.text = "Gr: " + p.resources[Grain];
        pui.stone.text = "St: " + p.resources[Stone];
        pui.knights.text = "Kn.: " + p.availableKnights + " / " + p.usedKnights;
        pui.knights.color = p.LargestArmy ? Color.green : Color.black;
        pui.VPC.text = "VPC: " + p.developmentPoints;
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
        stepText.text = "E " + Academy.Instance.EpisodeCount + " S " + Academy.Instance.StepCount;
    }
}
