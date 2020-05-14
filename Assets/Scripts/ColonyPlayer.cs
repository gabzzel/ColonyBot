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
        int actionNum = Mathf.FloorToInt(vectorAction[0]);
        Notifier.singleton.Notify("Player " + name + " takes action " + actionNum);

        // If we pass...
        if(actionNum == 0)
        {
            turnPhase = TurnPhase.Pass; // Save that we passed
            pm.NextPlayer(); // Give the turn to the next player
            return; // Don't do anything else
        }
        // If we do not pass, say that we are building now
        else { turnPhase = TurnPhase.Build; }

        BuildingType buildingType = Enums.GetBuildingTypeByNumber(Mathf.FloorToInt((actionNum-1f) % 3));
        int gridPointNum = Mathf.FloorToInt((actionNum-1f) / 3f);
        GridPoint gp = bc.gridPoints.Values.ToList()[gridPointNum];
        if (buildingType == BuildingType.Village) { gc.CreateVillageOrCity(gp, false); }
        else if (buildingType == BuildingType.City) { gc.CreateVillageOrCity(gp, true); }
        else if(buildingType == BuildingType.Street) { gc.CreateStreet(gp); }
    }

    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker)
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
                int actionNumber = i * 53 + j + 1;
                // Check if have the resources and we can  build here
                if(!HasResourcesToBuild(buildingType) || !bc.PossibleBuildingSite(gp, this, buildingType))
                {
                    impossibleActions.Add(actionNumber);
                }
            }
        }

        /*
        if(turnPhase == TurnPhase.Build)
        {
            // Mask action for which we don't have the res
            List<int> buildInts = new List<int>();
            // Add actions to the action mask if we can't afford to build it
            if (totalRes < 5 || resources[Resource.Ore] < 3 || resources[Resource.Grain] < 2) { buildInts.Add(GetActionNumber(BuildAction.BuildCity)); }

            if (totalRes < 4 || resources[Resource.Grain] < 1 || resources[Resource.Wood] < 1 || resources[Resource.Wool] < 1 || resources[Resource.Stone] < 1)
            { buildInts.Add(GetActionNumber(BuildAction.BuildVillage)); }

            if (totalRes < 2 || resources[Resource.Stone] < 1 || resources[Resource.Wood] < 1) { buildInts.Add(GetActionNumber(BuildAction.BuildVillage)); }
            buildInts.Remove(0);
            actionMasker.SetMask(0, buildInts);
            actionMasker.SetMask(1, FillRange(54));
        }
        else if(turnPhase == TurnPhase.ChooseGridPoint)
        {
            actionMasker.SetMask(0, FillRange(4));
            List<int> gridPointsInts = new List<int>();
            List<GridPoint> gridPoints = bc.gridPoints.Values.ToList();

            if(prevBuildAction == BuildAction.BuildCity)
            {
                gridPointsInts = ImpossibleForCity(gridPoints);
            }
            else if(prevBuildAction == BuildAction.BuildVillage)
            {
                gridPointsInts = ImpossibleForVillage(gridPoints);
            }
            else if(prevBuildAction == BuildAction.BuildRoad)
            {
                gridPointsInts = ImpossibleForStreet(gridPoints);
            }
            actionMasker.SetMask(1, gridPointsInts);
        }
        */
    }

    public void GiveResources(Resource res, int number = 1)
    {
        resources[res] += number;
        totalRes += number;
    }

    private List<int> FillRange(int stop)
    {
        List<int> result = new List<int>();
        for (int i = 0; i < stop; i++)
        {
            result.Add(i);
        }
        return result;
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

    private List<int> ImpossibleForCity(List<GridPoint> gps)
    {
        List<int> result = new List<int>();
        for (int i = 0; i < gps.Count; i++)
        {
            GridPoint gp = gps[i];
            // We can only place our cities on our own villages
            // So if there is no building, no village and it's not ours, we can't place a city here
            if(gp.building == null || gp.building.Owner != this || gp.building.Type != BuildingType.Village)
            {
                result.Add(i);
            }
        }
        return result;
    }

    private List<int> ImpossibleForVillage(List<GridPoint> gps)
    {
        List<int> result = new List<int>();
        for (int i = 0; i < gps.Count; i++)
        {
            GridPoint gp = gps[i];
            // Village cannot be placed on gridpoint with a building already on it.
            if(gp.building != null) { result.Add(i); continue; }


            // Abide to the distance rule (always 2 in between)
            bool added = false;
            foreach(GridPoint n in gp.GetNeighbouringGridPoints()) {
                if (n.building != null) { result.Add(i); added = true; break; }
            }
            if (added) { continue; }

            bool foundValidConnection = false;
            // Go through all connection to find a connection with one of our buildings
            foreach(KeyValuePair<GridPoint, Building> connection in gp.connectedTo)
            {
                // If our neighbour is not connected to us via a valid connection, it is not interesting
                if(connection.Value == null || connection.Value.Owner != this) { continue; }

                // Go through all second level neighbours 
                foreach(KeyValuePair<GridPoint, Building> connection2 in connection.Key.connectedTo)
                {
                    // If our second level neighbour is us, continue
                    if(connection2.Key == gp) { continue; }
                    // If our second level neighbour has no valid connection to our 1st level neighbour, continue
                    else if(connection2.Value == null || connection.Value.Owner != this) { continue; }
                    // If our second level neighbour has a valid connection to our 1st level neighbour AND there is a building owned by us on that 2nd level neighbour, we are golden
                    else if(connection2.Key.building != null && connection2.Key.building.Owner == this) { foundValidConnection = true; break; }
                }

                if (foundValidConnection) { break; }
            }

            if (!foundValidConnection) { result.Add(i);
 }

        }
        return result;
    }

    private List<int> ImpossibleForStreet(List<GridPoint> gps)
    {
        List<int> result = new List<int>();
        for (int i = 0; i < gps.Count; i++)
        {
            GridPoint gp = gps[i];

            bool connectedToBuilding = false;
            // Loop through all our neighbours
            foreach(GridPoint neighbour in gp.GetNeighbouringGridPoints())
            {
                // If there is a building on it that it owned by us...
                if(neighbour.building != null && neighbour.building.Owner == this)
                {
                    // .. We are connected!
                    connectedToBuilding = true;
                    break; // We can stop looking, we have found a connection
                }
            }
            // If we found a connection to a building, just continue
            if (connectedToBuilding) { continue; }

            bool connectedToStreet = false;
            // Loop through all connections
            foreach (KeyValuePair<GridPoint, Building> connection in gp.connectedTo)
            {
                // If we find a connection to us that is connected by a valid street..
                if(connection.Value != null && connection.Value.Owner == this)
                {
                    connectedToStreet = true;
                    break;

                }
            }

            if(!connectedToStreet) { result.Add(i); }
        }
        return result;
    }
}
