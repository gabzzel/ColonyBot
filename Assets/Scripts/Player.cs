using UnityEngine;
using static Enums;
using System.Collections.Generic;

[System.Serializable]
public class Player : MonoBehaviour
{
    public new string name = "";
    public Color color = Color.black;
    public int points = 0;
    public Dictionary<Resource, int> resources = new Dictionary<Resource, int>();

    public Action RequestAction(List<Action> possibleActions)
    {
        if (possibleActions.Contains(Action.Pass))
        {
            return Action.Pass;
        }
        else
        {
            return Action.BuildVillage;
        }
    }

    public GridPoint RequestBuildingPosition(List<GridPoint> possiblePositions)
    {
        if(possiblePositions == null || possiblePositions.Count == 0)
        {
            Debug.LogWarning(name + " was asked to place a building, but no possible positions where given!");
            return null;
        }
        int r = Random.Range(0, possiblePositions.Count);
        return possiblePositions[r];
    }

    public void Initialize(string name, Color color)
    {
        this.name = name;
        this.color = color;
        Debug.Log(name + " now has color " + color.ToString());
        points = 0;
        resources.Clear();
        InitializeResources();
    }

    void InitializeResources()
    {
        foreach(Resource res in GetResourcesAsList())
        {
            resources.Add(res, 0);
        }
    }

    public void AddToResource(Resource res, int number)
    {
        resources[res] += number;
    }
}
