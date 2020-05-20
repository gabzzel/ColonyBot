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
    public Dictionary<Resource, int> resources;
    public Dictionary<BuildingType, int> availableBuildings;
    public Color color = Color.blue;
    public TurnPhase turnPhase = TurnPhase.Pass;

    private List<Building> buildings = new List<Building>();

    public float Points { get { return GetCumulativeReward(); } }
    public Building LastBuilding
    {
        get
        {
            if(buildings.Count == 0) { return null; }
            return buildings[buildings.Count - 1];
        }
    }
    public BuildingType LastBuildingType { get { return LastBuilding.Type; } }

    private void Start()
    {
        if (!Academy.IsInitialized || !Academy.Instance.IsCommunicatorOn)
        {
            ResetAll();
        }
    }

    public void ResetAll()
    {
        resources = DefaultResDictInt;
        availableBuildings = DefaultBuildingDict;
        SetReward(0f);
        turnPhase = TurnPhase.Pass;
        buildings.Clear();
    }

    public override void OnEpisodeBegin() { ResetAll(); }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Add the amount of resources as inputs
        foreach(Resource res in resources.Keys)
        {
            sensor.AddObservation(resources[res]);
        }

        // For each gridpoint, get the following info : if there is a building and the value per resource
        foreach(NonTileGridPoint ntgp in BoardController.singleton.nonTileGridPoints)
        {
            // If we have a building : 1, if someone else has a building : -1, otherwise 0
            if(ntgp.Building == null) { sensor.AddObservation(0); }
            else if(ntgp.Building.Owner == this) { sensor.AddObservation(1); }
            else { sensor.AddObservation(-1); }

            foreach(Resource res in GetResourcesAsList())
            {
                sensor.AddObservation(ntgp.ResourceValue(res) / 3f); // Normalize for the fact that we can be attached to 3 tiles
            }
        }
    }

    public override void Heuristic(float[] actionsOut)
    {
        HashSet<int> actionMask = NormalActionMasks();
        List<int> availableActions = new List<int>();
        for (int i = 0; i < 3*54+1; i++) { if (!actionMask.Contains(i)) { availableActions.Add(i); } }
        int randomIndex = Random.Range(0, availableActions.Count);
        actionsOut[0] = availableActions[randomIndex];
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        int actionNum = Mathf.FloorToInt(vectorAction[0]);
        
        // If we pass...
        if(actionNum == 0 && buildings.Count >= 4)
        {
            turnPhase = TurnPhase.Pass; // Save that we passed
            Notifier.singleton.Notify(name + " passes.");
            PlayerManager.singleton.NextPlayer(); // Give the turn to the next player
            return; // Don't do anything else
        }
        // If we do not pass, say that we are building now
        else { turnPhase = TurnPhase.Build; }
        
        // 1. Get the building type and the gridpoint out of the action number
        BuildingType buildingType = GetBuildingTypeByNumber(Mathf.FloorToInt(((float)actionNum-1f) % 3f));
        int gridPointNum = Mathf.FloorToInt(((float)actionNum-1f) / 3f);
        NonTileGridPoint gp = BoardController.singleton.nonTileGridPoints[gridPointNum];
        Notifier.singleton.Notify("Player " + name + " : " + buildingType + " - " + gp.ToString());

        // 2. Remove the resources and the building from our stockpile
        availableBuildings[buildingType] -= 1;
        if (buildings.Count >= 4) { RemoveResourcesForBuilding(buildingType); }

        // 3. Give the order to build the building at the desired GridPoint
        if (buildingType == BuildingType.Village) { buildings.Add(GameController.singleton.CreateVillageOrCity(gp, false, this)); }
        else if (buildingType == BuildingType.City)
        {   
            availableBuildings[BuildingType.Village] += 1;
            buildings.Add(GameController.singleton.CreateVillageOrCity(gp, true, this));
        }
        else if(buildingType == BuildingType.Street) { buildings.Add(GameController.singleton.CreateStreet(gp, this)); }
    }

    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker)
    {
        actionMasker.SetMask(0, NormalActionMasks());
    }

    private HashSet<int> NormalActionMasks()
    {
        HashSet<int> impossibleActions = new HashSet<int>();

        if(buildings.Count < 4) { impossibleActions.Add(0); }

        List<NonTileGridPoint> gps = BoardController.singleton.nonTileGridPoints;
        // For every action...
        for (int i = 0; i < gps.Count; i++)
        {
            NonTileGridPoint gp = gps[i];
            // For every gridpoint...
            for (int j = 0; j < 3; j++)
            {                
                BuildingType buildingType = GetBuildingTypeByNumber(j);
                int actionNumber = i * 3 + j + 1;

                // We are in the initial building phase.
                if(buildings.Count < 4)
                {
                    // If we have build nothing, or a village and a street..
                    if(buildings.Count % 2f == 0f && (
                        // And we are not building  a village, or we can't build a village here
                        buildingType != BuildingType.Village || !BoardController.singleton.PossibleBuildingSite(gp, this, buildingType, true)))
                    {
                        impossibleActions.Add(actionNumber);
                    }
                    else if (buildings.Count % 2f == 1f && (buildingType != BuildingType.Street || !gp.IsConnectedTo(LastBuilding.Position, false)))
                    {
                        impossibleActions.Add(actionNumber);
                    }
                }

                // We are not in the initial building phase.
                else
                {
                    // If we don't have enough resources for this building...
                    if (!HasResourcesToBuild(buildingType)) { impossibleActions.Add(actionNumber); }
                    // If we can't place the building here...
                    else if(!BoardController.singleton.PossibleBuildingSite(gp, this, buildingType)) { impossibleActions.Add(actionNumber); }
                    // If we don't have any buildings of this type left to build...
                    else if(availableBuildings[buildingType] <= 0) { impossibleActions.Add(actionNumber); }
                }
            }
        }
        return impossibleActions;
    }

    public void GiveResources(Resource res, int number = 1)
    {
        resources[res] += number;
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

        foreach(Resource res in GetResourcesAsList())
        {
            if(resources[res] < 0) { throw new System.Exception("The " + res + " stockpile of " + name + " is below zero, so we cannot build " + buildingType); }
        }
    } 
}