using System.Collections.Generic;
using UnityEngine;
using static Utility;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Linq;
using System;

public class ColonyPlayer : Agent
{

    [SerializeField] private int playerID = 0;

    /// <summary>
    /// The current available resources, indexed by their type (Wood = 0, Stone = 1 etc.)
    /// </summary>
    public int[] resources;
    /// <summary>
    /// The available buildings in the stockpile, indexed by building type (Street = 0, Village = 1, City = 2)
    /// </summary>
    public int[] availableBuildings;
    public int[] buildingOptions;
    /// <summary>
    /// The knight-cards still available to play.
    /// </summary>
    public int availableKnights = 0;
    public int usedKnights = 0;
    /// <summary>
    /// Points obtained by buying Development Cards and drawing a victory point.
    /// </summary>
    public int developmentPoints = 0;
    public Color color = Color.blue;
    public TurnPhase turnPhase = TurnPhase.Pass;
    private bool largestArmy = false;

    private readonly List<Building> buildings = new List<Building>();
    public Trader trader = null;

    public int ID { get { return playerID; } }
    public float Points { get { return GetCumulativeReward(); } }
    public Building LastBuilding
    {
        get
        {
            if(buildings.Count == 0) { return null; }
            return buildings[buildings.Count - 1];
        }
    }
    public int LastBuildingType { get { return LastBuilding.Type; } }
    public List<Building> Buildings { get { return buildings; } }
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
    public bool LargestArmy
    {
        get { return largestArmy; }

        set
        {
            if(usedKnights < 3) { throw new Exception("Cannot set largest army value if we do not have 3 knights used! "); }
            largestArmy = value;
            AddReward(value ? 2f : -2f);
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
        buildingOptions = new int[3];
        SetReward(0f);
        turnPhase = TurnPhase.Pass;
        buildings.Clear();
        trader = GetComponent<Trader>();
        trader.Initialize();
        availableKnights = 0;
        developmentPoints = 0;
        usedKnights = 0;
    }

    public override void OnEpisodeBegin() { ResetAll(); }

    public override void CollectObservations(VectorSensor sensor)
    {
        // RESOURCE INPUTS
        foreach(int res in resources) { sensor.AddObservation(res); }
        
        // KNIGHT CARD INPUTS
        sensor.AddObservation(availableKnights);
        sensor.AddObservation(usedKnights);
        sensor.AddObservation(largestArmy);

        // GRIDPOINT INPUTS
        // For each gridpoint, get the following info : if there is a building and the value per resource
        foreach(NonTileGridPoint ntgp in BoardController.singleton.nonTileGridPoints)
        {
            // If we have a building : 1, if someone else has a building : -1, otherwise 0
            if(ntgp.Building == null) { sensor.AddObservation(0); }
            else { sensor.AddObservation(ntgp.Building.Owner == this ? 1 : -1); }

            // Whether this NTGP is affected by the robber
            if (BoardController.singleton.RobberLocation == null) { sensor.AddObservation(0); }
            else { sensor.AddObservation(BoardController.singleton.RobberLocation.connectedIndexes.Contains(ntgp.index)); }

            for (int resourceID = 0; resourceID < resources.Length; resourceID++)
            {
                sensor.AddObservation(ntgp.ResourceValue(resourceID) / 3f);  // Normalize for the fact that we can be attached to 3 tiles
            }
        }
    }

    public override void Heuristic(float[] actionsOut)
    {
        List<int> availableActions = new List<int>();

        if(buildings.Count < 4)
        {
            List<int> actionMask = InitialActionMask();
            for (int i = 0; i < 3 * 54 + 1; i++) { if (!actionMask.Contains(i)) { availableActions.Add(i); } }
        }
        else
        {
            HashSet<int> mask = NormalActionMask();
            for (int i = 0; i < 165; i++)
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
            Notifier.singleton.Notify(name + " was told to take an action before the start of the game!");
            return;
        }

        int actionNum = Mathf.FloorToInt(vectorAction[0]);

        // If we pass...
        if (actionNum == Pass)
        {
            Notifier.singleton.Notify(name + " passes.");
            turnPhase = TurnPhase.Pass;
            PlayerManager.singleton.NotifyOfPass();
            return; // Don't do anything else
        }
        // If we do not pass, say that we are building now
        else if (actionNum > 0 && actionNum < 163)
        {
            turnPhase = TurnPhase.Build;

            // 1. Get the building type and the gridpoint out of the action number
            int[] convertedAction = ConvertAction(actionNum);
            int buildingType = convertedAction[0];
            int gridPointNum = convertedAction[1];
            gridPointNum = BoardController.ntgpIndexes[gridPointNum];
            NonTileGridPoint gp = (NonTileGridPoint)BoardController.singleton.allGridPoints[gridPointNum];
            Notifier.singleton.Notify("Player " + name + " : " + BuildingNames[buildingType] + " - " + gp.ToString());

            // 2. Remove the resources and the building from our stockpile
            availableBuildings[buildingType] -= 1;
            if (buildings.Count >= 4) { RemoveResourcesForBuilding(buildingType); }

            // 3. Give the order to build the building at the desired GridPoint
            if (buildingType == Village) { buildings.Add(GameController.singleton.CreateVillageOrCity(gp, false, this)); }
            else if (buildingType == City)
            {
                availableBuildings[Village] += 1;
                buildings.Add(GameController.singleton.CreateVillageOrCity(gp, true, this));
            }
            else if (buildingType == Street) { buildings.Add(GameController.singleton.CreateStreet(gp, this)); }
        }
        else if(actionNum == BuyDevelopmentCard)
        {
            turnPhase = TurnPhase.Build;
            GameController.singleton.DrawDevelopmentCard(this);
            Notifier.singleton.Notify(name + " buys DevCard.");
            resources[Grain]--; resources[Ore]--; resources[Wool]--;
        }
        else if(actionNum == PlayKnightCard)
        {
            turnPhase = TurnPhase.Build;
            availableKnights--;
            usedKnights++;
            GameController.singleton.ExecuteRobberPhase(true);
            Notifier.singleton.Notify(name + " plays KnightCard.");
            if (usedKnights >= 3) { PlayerManager.singleton.CheckLargestArmy(); }
        }
    }

    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker)
    {
        if(buildings.Count < 4) { actionMasker.SetMask(0, InitialActionMask()); }
        else
        {
            actionMasker.SetMask(0, NormalActionMask());
        }
    }

    private List<int> InitialActionMask()
    {
        List<int> actionMask = new List<int>{ Pass, BuyDevelopmentCard, PlayKnightCard };
        for (int i = 0; i < BoardController.ntgpIndexes.Count; i++)
        {
            int ntgpIndex = BoardController.ntgpIndexes[i];
            NonTileGridPoint ntgp = (NonTileGridPoint)BoardController.singleton.allGridPoints[ntgpIndex];
            for (int buildingType = 0; buildingType < 3; buildingType++)
            {
                int actionNumber = ConvertAction(buildingType, i);

                // We have to build a village!
                if (buildings.Count % 2 == 0 && buildingType != Village) { actionMask.Add(actionNumber); }
                // If we want to build a village, but this NTGP or one of it's neighbours is occupied
                else if (buildings.Count % 2 == 0 && (ntgp.Building != null || ntgp.NeighbourOccupied())) { actionMask.Add(actionNumber); }
                // We need to build a street!
                else if (buildings.Count % 2 == 1 && buildingType != Street) { actionMask.Add(actionNumber); }
                // If we want to build a street, but there is no valid street connection between this and the last building. (1 means that they are connected but not by a street)
                else if (buildings.Count % 2 == 1 && BoardController.singleton.connections[ntgp.index, LastBuilding.Position.index] != 1) { actionMask.Add(actionNumber); }
            }
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

        if(resources[Wool] < 1 || resources[Ore] < 1 || resources[Grain] < 1) { result.Add(BuyDevelopmentCard); }
        if(availableKnights == 0) { result.Add(PlayKnightCard); }

        return result;
    }

    public void GiveResources(int resourceID, int number = 1)
    {
        // Correct the resourceID, because our array starts with Wood at 0, while the array of all resources starts with Wood at 1
        resources[resourceID] += number;
        trader.dirty = true;
    }

    private bool HasResourcesToBuild(int buildingType)
    {
        if(buildingType == Village)
        {
            return (resources[Grain] >= 1 && resources[Stone] >= 1 && resources[Wood] >= 1 && resources[Wool] >= 1);
        }
        else if(buildingType == Street)
        {
            return (resources[Stone] >= 1 && resources[Wood] >= 1);
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
            case Street:
                if(resources[Wood] <= 0 || resources[Stone] <= 0) { throw new Exception(name + " does not have the resources for a street!"); }
                resources[Wood]--;
                resources[Stone]--; 
                break;
            case Village:
                if (resources[Wood] <= 0 || resources[Stone] <= 0 || resources[Grain] <= 0 || resources[Wool] <= 0) { throw new Exception(name + " does not have the resources for a village!"); }
                resources[Wood]--;
                resources[Wool]--;
                resources[Stone]--;
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