using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Enums;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using System.Linq;

public class ColonyPlayer : Agent
{
    public int points = 0;
    private int totalRes = 0;
    public Dictionary<Resource, int> resources;
    public Color color = Color.blue;

    private BoardController bc;
    private GameController gc;

    public override void Initialize()
    {
        bc = GameObject.FindGameObjectWithTag("GameController").GetComponent<BoardController>();
        gc = GameController.singleton;
        resources = Enums.DefaultResDict;
        points = 0;      
    }

    public override void OnEpisodeBegin()
    {
        resources = Enums.DefaultResDict;
        points = 0;
        Notifier.singleton.Notify(name + "'s team is " + GetComponent<BehaviorParameters>().TeamId);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Add the amount of resources as inputs
        foreach(Resource res in resources.Keys)
        {
            sensor.AddObservation(resources[res]);
        }


        // For each gridpoint, get the following info : if there is a building and the value per resource
        foreach(GridPoint gp in bc.gridPoints.Values)
        {
            if (gp.isMiddle) { continue; }

            // If we have a building : 1, if someone else has a building : -1, otherwise 0
            if(gp.building == null) { sensor.AddObservation(0); }
            else if(gp.building.Owner == this) { sensor.AddObservation(1); }
            else { sensor.AddObservation(-1); }

            foreach(Resource res in GetResourcesAsList())
            {
                sensor.AddObservation(gp.resourceValues[res] / 3f); // Normalize for the fact that we can be attached to 3 tiles
            }
        }
    }

    public override void Heuristic(float[] actionsOut)
    {
        actionsOut = new float[1];
        actionsOut[0] = 1;
        Notifier.singleton.Notify("Agent " + name + " had to takes an action.");
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        Action action = Enums.GetActionByNumber(Mathf.FloorToInt(vectorAction[0]));
        Notifier.singleton.Notify(name + " takes action " + action.ToString());
        int gridIndex = Mathf.FloorToInt(vectorAction[1]);
        GridPoint gp = bc.gridPoints.Values.ToList()[gridIndex];
        if (action == Action.BuildVillage)
        {
            gc.CreateVillageOrCity(gp, false);
        }
        else if(action == Action.BuildCity)
        {
            gc.CreateVillageOrCity(gp, true);
        }
    }

    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker)
    {
        // Mask action for which we don't have the res
        List<int> buildInts = new List<int>();
        // Add actions to the action mask if we can't afford to build it
        if(totalRes < 5 || resources[Resource.Ore] < 3 || resources[Resource.Grain] < 2) { buildInts.Add(Enums.GetActionNumber(Action.BuildCity)); }
        if(totalRes < 4 ||  resources[Resource.Grain] < 1 || resources[Resource.Wood] < 1 || resources[Resource.Wool] < 1 || resources[Resource.Stone] < 1) { buildInts.Add(Enums.GetActionNumber(Action.BuildVillage)); }
        if(totalRes < 2 || resources[Resource.Stone] < 1 || resources[Resource.Wood] < 1) { buildInts.Add(GetActionNumber(Action.BuildVillage)); }
        actionMasker.SetMask(0, buildInts);

        List<int> gridPointsInts = new List<int>();
        List<GridPoint> gridPoints = bc.gridPoints.Values.ToList();
        for (int i = 0; i < gridPoints.Count; i++)
        {
            GridPoint gp = gridPoints[i];
            if(gp.isMiddle || gp.building != null)
            {

            }
        }
    }

    public void GiveResources(Resource res, int number = 1)
    {
        resources[res] += number;
        totalRes += number;
    }
}
