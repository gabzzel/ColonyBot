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
    public TurnPhase turnPhase = TurnPhase.Pass;

    private BoardController bc;
    private GameController gc;
    private PlayerManager pm;
    private GridPoint lastVillage = null;

    public override void Initialize()
    {
        bc = GameObject.FindGameObjectWithTag("GameController").GetComponent<BoardController>();
        pm = GameObject.FindGameObjectWithTag("GameController").GetComponent<PlayerManager>();
        gc = GameController.singleton;
        resources = Enums.DefaultResDict;
        points = 0;
    }

    public override void OnEpisodeBegin()
    {
        resources = Enums.DefaultResDict;
        points = 0;
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
        actionsOut[0] = 1;
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        int actionNum = Mathf.FloorToInt(vectorAction[0]);
        
        // If we pass...
        if(actionNum == 0)
        {
            turnPhase = TurnPhase.Pass; // Save that we passed
            Notifier.singleton.Notify(name + " passes.");
            pm.NextPlayer(); // Give the turn to the next player
            return; // Don't do anything else
        }
        // If we do not pass, say that we are building now
        else { turnPhase = TurnPhase.Build; }

        BuildingType buildingType = Enums.GetBuildingTypeByNumber(Mathf.FloorToInt((actionNum-1f) % 3));
        int gridPointNum = Mathf.FloorToInt((actionNum-1f) / 3f);
        GridPoint gp = bc.gridPoints.Values.ToList()[gridPointNum];
        Notifier.singleton.Notify("Player " + name + " : " + buildingType + " - " + gp.ToString());
        if (this.transform.childCount >= 4) { RemoveResourcesForBuilding(buildingType); }
        if (buildingType == BuildingType.Village) { gc.CreateVillageOrCity(gp, false, this); lastVillage = gp; }
        else if (buildingType == BuildingType.City) { gc.CreateVillageOrCity(gp, true, this); }
        else if(buildingType == BuildingType.Street) { gc.CreateStreet(gp, this); }
    }

    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker)
    {
        if(this.transform.childCount < 4)
        {
            CollectDiscretActionMasksInitial(actionMasker);
        }
        else
        {
            CollectDiscreteActionMasksNormal(actionMasker);
        }
    }

    private void CollectDiscreteActionMasksNormal(DiscreteActionMasker actionMasker)
    {
        List<int> impossibleActions = new List<int>();
        List<GridPoint> gps = bc.gridPoints.Values.ToList();
        // For every action...
        for (int i = 0; i < 3; i++)
        {
            BuildingType buildingType = GetBuildingTypeByNumber(i);
            // For every gridpoint...
            for (int j = 0; j < 54; j++)
            {
                GridPoint gp = gps[j];
                int actionNumber = i * 54 + j + 1;
                // Check if have the resources and we can  build here
                if (!HasResourcesToBuild(buildingType) || !bc.PossibleBuildingSite(gp, this, buildingType))
                {
                    impossibleActions.Add(actionNumber);
                }
            }
        }
        actionMasker.SetMask(0, impossibleActions);

    }

    private void CollectDiscretActionMasksInitial(DiscreteActionMasker actionMasker)
    {

        List<int> impossibleActions = new List<int>() { 0 }; // Passing is impossible in the initial actions
        List<GridPoint> gps = bc.gridPoints.Values.ToList();

        // For every action...
        for (int i = 0; i < 3; i++)
        {
            BuildingType buildingType = GetBuildingTypeByNumber(i);
            // For every gridpoint...
            for (int j = 0; j < 54; j++)
            {
                GridPoint gp = gps[j];
                int actionNumber = i * 54 + j + 1;
                
                // Our initial villages
                if(buildingType != BuildingType.Village || this.transform.childCount % 2 == 0 && !bc.PossibleBuildingSite(gp, this, BuildingType.Village, true))
                {
                    impossibleActions.Add(actionNumber);
                }
                // Our initial streets
                else if(buildingType != BuildingType.Street || this.transform.childCount % 2 == 1 && (lastVillage == null || !gp.connectedTo.ContainsKey(lastVillage)))
                {
                    impossibleActions.Add(actionNumber);
                }
            }
        }
        actionMasker.SetMask(0, impossibleActions);
    }

    public void GiveResources(Resource res, int number = 1)
    {
        resources[res] += number;
        totalRes += number;
    }

    private bool HasResourcesToBuild(BuildingType buildingType)
    {
        if(buildingType == BuildingType.Village)
        {
            return (resources[Resource.Grain] >= 1 && resources[Resource.Stone] >= 1 && resources[Resource.Wood] >= 1 && resources[Resource.Wool] >= 1);
        }
        else if(buildingType == BuildingType.Street)
        {
            return (resources[Resource.Stone] >= 1 && resources[Resource.Wood] >= 1);
        }
        else if(buildingType == BuildingType.City)
        {
            return resources[Resource.Grain] >= 2 && resources[Resource.Ore] >= 3;
        }
        return false;
    }

    public void RemoveResourcesForBuilding(BuildingType buildingType)
    {
        switch (buildingType)
        {
            case BuildingType.Street:
                resources[Resource.Wood] -= 1;
                resources[Resource.Stone] -= 1; 
                break;
            case BuildingType.Village:
                resources[Resource.Wood] -= 1;
                resources[Resource.Wool] -= 1;
                resources[Resource.Stone] -= 1;
                resources[Resource.Grain] -= 1;
                break;
            case BuildingType.City:
                resources[Resource.Ore] -= 3;
                resources[Resource.Grain] -= 2;
                break;
            default:
                break;
        }
    }

    
}

