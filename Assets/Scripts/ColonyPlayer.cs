using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Enums;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class ColonyPlayer : Agent
{
    public int points = 0;
    public Dictionary<Resource, int> resources;
    public Color color = Color.blue;

    private BoardController bc;

    public override void Initialize()
    {
        bc = GameObject.FindGameObjectWithTag("GameController").GetComponent<BoardController>();
    }

    public override void OnEpisodeBegin()
    {
        resources = new Dictionary<Resource, int>();
        foreach (Resource res in Enums.GetResourcesAsList()) { resources.Add(res, 0); }
        points = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        
    }

    public override void Heuristic(float[] actionsOut)
    {
        
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        
    }

    public void GiveResources(Resource res, int number = 1)
    {
        resources[res] += number;
    }
}
