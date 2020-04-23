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

    public Action RequestAction()
    {
        return Action.Pass;
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
