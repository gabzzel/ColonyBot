using System.Collections.Generic;
using UnityEngine;
using static Utility;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Linq;
using System;
using Unity.MLAgents.Policies;

public class ColonyPlayer : Agent
{

    [SerializeField] private int playerID = 0;

    /// <summary>
    /// The current available resources, indexed by their type (Lumber = 0, Brick = 1 etc.)
    /// </summary>
    public int[] resources;
    /// <summary>
    /// The available buildings in the stockpile, indexed by building type (Street = 0, Village = 1, City = 2)
    /// </summary>
    public int[] availableBuildings;
    //public int availableKnights = 0;
    //public int usedKnights = 0;
    //public int developmentPoints = 0;
    public Color color = Color.blue;
    public TurnPhase turnPhase = TurnPhase.Pass;
    //private bool largestArmy = false;
    public Trader trader = null;
    public HashSet<int> harbors = new HashSet<int>();

    private HashSet<int> prevMask = new HashSet<int>();

    public int ID { get { return playerID; } }
    public float Points { get { return GetCumulativeReward(); } }
    public Building LastBuilding
    {
        get
        {
            if(Buildings.Count == 0) { return null; }
            return Buildings[Buildings.Count - 1];
        }
    }
    public int LastBuildingType { get { return LastBuilding.Type; } }
    public List<Building> Buildings { get; } = new List<Building>();
    public int TotalResources
    {
        get
        {
            int number = 0;
            foreach(int res in resources)
            {
                number += res;
            }
            return number;
        }
    }

    private void Awake()
    {
        trader = GetComponent<Trader>();
        trader.isBank = false;
    }

    private void Start()
    {
        if (!Academy.IsInitialized || !Academy.Instance.IsCommunicatorOn)
        {
            ResetAll();
        }
    }

    public void ResetAll()
    {
        resources = new int[5];
        availableBuildings = new int[] { 15, 5, 4 };
        SetReward(0f);
        turnPhase = TurnPhase.Pass;
        Buildings.Clear();
        trader = GetComponent<Trader>();
        trader.Initialize();
        //availableKnights = 0;
        //developmentPoints = 0;
        //usedKnights = 0;
        harbors.Clear();
    }

    public override void OnEpisodeBegin() { ResetAll(); }

    public override void CollectObservations(VectorSensor sensor)
    {
        // RESOURCE INPUTS
        foreach(int res in resources) { sensor.AddObservation(res); }
        
        // KNIGHT CARD INPUTS
        //sensor.AddObservation(availableKnights);
        //sensor.AddObservation(usedKnights);
        //sensor.AddObservation(largestArmy);

        // GRIDPOINT INPUTS
        // For each gridpoint, get the following info : if there is a building and the value per resource
        foreach(NonTileGridPoint ntgp in BoardController.singleton.nonTileGridPoints)
        {
            // 1. If we have a building : 1, if someone else has a building : -1, otherwise 0
            if(ntgp.Building == null) { sensor.AddObservation(0); }
            else { sensor.AddObservation(ntgp.Building.Owner == this ? 1 : -1); }

            // 2. If the gridpoint is also a harbor
            sensor.AddObservation(ntgp.harbor != NoHarbor);

            // 3. Whether this NTGP is affected by the robber
            if (BoardController.singleton.RobberLocation == null) { sensor.AddObservation(0); }
            else { sensor.AddObservation(BoardController.singleton.RobberLocation.connectedNTGPs.Contains(ntgp.index)); }

            // 4 to 8. The resource-values of every gridpoint
            for (int resourceID = 0; resourceID < resources.Length; resourceID++)
            {
                sensor.AddObservation(ntgp.ResourceValue(resourceID) / 3f);  // Normalize for the fact that we can be attached to 3 tiles
            }
        }
    }

    public override void Heuristic(float[] actionsOut)
    {
        List<int> availableActions = new List<int>();

        if(Buildings.Count < 4)
        {
            HashSet<int> actionMask = InitialActionMask();
            if(actionMask.Count == 163) { availableActions.Add(Pass); }
            for (int i = 0; i < 3 * 54 + 1; i++) { if (!actionMask.Contains(i)) { availableActions.Add(i); } }
        }
        else
        {
            HashSet<int> mask = NormalActionMask();
            for (int i = 0; i < 163; i++)
            {
                if (!mask.Contains(i)) { availableActions.Add(i); }
            }
        }

        if(availableActions.Count == 0)
        {
            throw new Exception("Cannot choose action from a empty list!");
        }

        int randomIndex = UnityEngine.Random.Range(0, availableActions.Count);
        actionsOut[0] = availableActions[randomIndex];
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        // If we have won, don't do anything but notify everyone
        if (Points >= GameController.singleton.pointsToWin) { PlayerManager.singleton.NotifyOfWin(this); return; }

        // Don't do anything if the game isn't started yet
        if (!GameController.singleton.GameStarted)
        {
            if (Academy.IsInitialized && Academy.Instance.StepCount > 1) { EndEpisode(); }
            Notifier.singleton.Notify(name + " was told to take an action before the start of the game!");
            return;
        }

        int actionNum = Mathf.FloorToInt(vectorAction[0]);
        if (prevMask.Contains(actionNum) || prevMask.Count == 163) { actionNum = Pass; }

        // If we pass...
        if (actionNum == Pass)
        {
            Notifier.singleton.Notify(name + " passes.");
            turnPhase = TurnPhase.Pass;
            PlayerManager.singleton.NotifyOfPass();
            return; // Don't do anything else
        }
        // If we do not pass, say that we are building now
        else
        {
            turnPhase = TurnPhase.Build;

            // 1. Get the building type and the gridpoint out of the action number
            int[] convertedAction = ConvertAction(actionNum);
            int buildingType = convertedAction[0];
            int gridPointNum = convertedAction[1];
            gridPointNum = BoardController.ntgpIndexes[gridPointNum];
            NonTileGridPoint ntgp = (NonTileGridPoint)BoardController.singleton.allGridPoints[gridPointNum];
            Notifier.singleton.Notify("Player " + name + " : " + BuildingNames[buildingType] + " - " + ntgp.ToString());

            // 2. Remove the resources and the building from our stockpile
            availableBuildings[buildingType] -= 1;
            if (Buildings.Count >= 4) { RemoveResourcesForBuilding(buildingType); }

            // 3. Give the order to build the building at the desired GridPoint
            if (buildingType == Village) { Buildings.Add(GameController.singleton.CreateVillageOrCity(ntgp, false, this, Buildings.Count < 4)); }
            else if (buildingType == City)
            {
                availableBuildings[Village] += 1;
                Buildings.Add(GameController.singleton.CreateVillageOrCity(ntgp, true, this, false));
                harbors.Add(ntgp.harbor);
            }
            else if (buildingType == Street) { Buildings.Add(GameController.singleton.CreateStreet(ntgp, this, Buildings.Count < 4 ? LastBuilding.Position : null, Buildings.Count < 4)); }
        }
    }

    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker)
    {
        if (Buildings.Count < 4) 
        {
            prevMask = InitialActionMask();
            actionMasker.SetMask(0, prevMask);
        }
        else
        {
            prevMask = NormalActionMask();
            actionMasker.SetMask(0, prevMask);
        }
    }

    private HashSet<int> InitialActionMask()
    {
        // If we are in the inital phase and have build 2 buildings or 4 buildings
        if((Buildings.Count == 2 || Buildings.Count == 4) && turnPhase == TurnPhase.Build)
        {
            HashSet<int> mask = new HashSet<int>();
            for (int i = 1; i < 163; i++)
            {
                mask.Add(i);
            }
            return mask;
        }


        HashSet<int> actionMask = new HashSet<int> { Pass }; //,BuyDevelopmentCard, PlayKnightCard };
        for (int i = 0; i < BoardController.ntgpIndexes.Count; i++)
        {
            int ntgpIndex = BoardController.ntgpIndexes[i];
            NonTileGridPoint ntgp = (NonTileGridPoint)BoardController.singleton.allGridPoints[ntgpIndex];
            for (int buildingType = 0; buildingType < 3; buildingType++)
            {
                int actionNumber = ConvertAction(buildingType, i);

                // We have to build a village!
                if (Buildings.Count % 2 == 0 && buildingType != Village) { actionMask.Add(actionNumber); }
                // If we want to build a village, but this NTGP or one of it's neighbours is occupied
                else if (Buildings.Count % 2 == 0 && (ntgp.Building != null || ntgp.NeighbourOccupied())) { actionMask.Add(actionNumber); }
                // We need to build a street!
                else if (Buildings.Count % 2 == 1 && buildingType != Utility.Street) { actionMask.Add(actionNumber); }
                // If we want to build a street, but there is no valid street connection between this and the last building. (1 means that they are connected but not by a street)
                else if (Buildings.Count % 2 == 1 && BoardController.singleton.connections[ntgp.index, LastBuilding.Position.index] != 1) { actionMask.Add(actionNumber); }
            }
        }
        if(actionMask.Count >= 163)
        {
            throw new Exception("All actions are masked in the initial masking phase!");
        }
        return actionMask;
    }

    private HashSet<int> NormalActionMask()
    {
        HashSet<int> result = new HashSet<int>();

        for (int i = 0; i < 54; i++)
        {
            NonTileGridPoint ntgp = (NonTileGridPoint)BoardController.singleton.allGridPoints[BoardController.ntgpIndexes[i]];
            for (int buildingType = 0; buildingType < 3; buildingType++)
            {
                int actionNumber = ConvertAction(buildingType, i);
                if (!HasResourcesToBuild(buildingType) || availableBuildings[buildingType] <= 0) { result.Add(actionNumber); }
                else if (!BoardController.singleton.PossibleBuildingSite(ntgp, this, buildingType)) { result.Add(actionNumber); }
            }
        }

        if (result.Contains(Pass)) { result.Remove(Pass); } // We can always pass!

        return result;
    }

    public void GiveResources(int resourceID, int number = 1)
    {
        // Correct the resourceID, because our array starts with Lumber at 0, while the array of all resources starts with Lumber at 1
        resources[resourceID] += number;
        trader.dirty = true;
    }

    private bool HasResourcesToBuild(int buildingType)
    {
        if(buildingType == Village)
        {
            return (resources[Grain] >= 1 && resources[Brick] >= 1 && resources[Lumber] >= 1 && resources[Wool] >= 1);
        }
        else if(buildingType == Street)
        {
            return (resources[Brick] >= 1 && resources[Lumber] >= 1);
        }
        else if(buildingType == City)
        {
            return resources[Grain] >= 2 && resources[Ore] >= 3;
        }
        return false;
    }

    public void RemoveResourcesForBuilding(int buildingType)
    {
        switch (buildingType)
        {
            case Utility.Street:
                if(resources[Lumber] <= 0 || resources[Brick] <= 0) { throw new Exception(name + " does not have the resources for a street!"); }
                resources[Lumber]--;
                resources[Brick]--; 
                break;
            case Village:
                if (resources[Lumber] <= 0 || resources[Brick] <= 0 || resources[Grain] <= 0 || resources[Wool] <= 0) { throw new Exception(name + " does not have the resources for a village!"); }
                resources[Lumber]--;
                resources[Wool]--;
                resources[Brick]--;
                resources[Grain]--;
                break;
            case City:
                if (resources[Ore] <= 2 || resources[Grain] <= 1) { throw new Exception(name + " does not have the resources for a city!"); }
                resources[Ore] -= 3;
                resources[Grain] -= 2;
                break;
            default:
                break;
        }

        trader.dirty = true;
    } 

    public int RemoveResource(int resourceID = Desert)
    {
        if (resourceID != Desert)
        {
            if (resources[resourceID] < 1) { throw new Exception("Cannot remove " + ResourceNames[resourceID] + " from " + name + " because stockpile is too low."); }
            resources[resourceID]--;
            return resourceID;
        }

        // Remove a random resource
        else
        {    
            int totalres = TotalResources;
            if (totalres == 0) { return Desert; }
            int randomResIndex = UnityEngine.Random.Range(1, totalres);
            int soFar = 0;

            for (int i = 0; i < resources.Length; i++)
            {
                soFar += resources[i];
                if(soFar >= randomResIndex) { resources[i]--; return i; }
            }
        }
        return Desert;
    }

    /// <summary>
    /// Convert an actionNumber to a pair of buildingtype and NTGP index.
    /// </summary>
    /// <param name="actionNumber"> The number of the action. </param>
    /// <returns> A pair, where the key is the buildingtype and the value is the gridpoint index. </returns>
    public static int[] ConvertAction(int actionNumber)
    {
        int[] result = new int[2];
        result[0] = (actionNumber - 1) % 3;
        result[1] = Mathf.FloorToInt(((float)actionNumber - 1f) / 3f);
        return result;
    }

    public static int ConvertAction(int buildingType, int gridPointNumber)
    {
        return 1 + gridPointNumber * 3 + buildingType;
    }
}