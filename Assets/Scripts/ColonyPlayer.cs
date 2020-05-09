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
        resources = new Dictionary<Resource, int>();
        foreach (Resource res in Enums.GetResourcesAsList()) { resources.Add(res, 0); }
        points = 0;
    }

    public override void OnEpisodeBegin()
    {
        resources = new Dictionary<Resource, int>();
        foreach (Resource res in Enums.GetResourcesAsList()) { resources.Add(res, 0); }
        points = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(true);
    }

    public override void Heuristic(float[] actionsOut)
    {
        actionsOut = new float[1];
        actionsOut[0] = 1;
        Notifier.singleton.Notify("Agent " + name + " had to takes an action.");
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        Notifier.singleton.Notify("Agent " + name + " had to takes an action.");
    }

    public void GiveResources(Resource res, int number = 1)
    {
        resources[res] += number;
    }
}
