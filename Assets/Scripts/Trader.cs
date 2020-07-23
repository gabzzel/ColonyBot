using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using static Utility;

public class Trader : MonoBehaviour
{
    public bool dirty = false;
    public bool isBank = false;
    private Trader bank = null;

    [SerializeField] private float[] resourceValues = new float[5];
    [SerializeField] private float[] availability = new float[5];
    float availabilityMax = 1f;
    [SerializeField] private float[] usefulness = new float[5];
    float useFulnessMax = 1f;

    [SerializeField] float mean = 0.5f;
    ColonyPlayer player = null;

    private void Awake()
    {    
        if (!isBank)
        {
            player = GetComponent<ColonyPlayer>();
        }
    }

    private void Start()
    {
        bank = GameController.singleton.gameObject.GetComponent<Trader>();
    }

    public void Initialize()
    {
        resourceValues = new float[5];
        availability = new float[5];
        usefulness = new float[5];
        availabilityMax = 1f;
        useFulnessMax = 1f;
        mean = 0.5f;
    }

    private void UpdateResourceValues()
    {
        mean = Mean(resourceValues);

        #region Availability

        availability = new float[5];
        foreach (Building building in player.Buildings)
        {
            if (building.Type == Street) { continue; }
            NonTileGridPoint ntgp = building.Position;

            for (int resourceID = 0; resourceID < player.resources.Length; resourceID++)
            {
                availability[resourceID] += ntgp.ResourceValue(resourceID) * building.Type;
            }
        }

        availabilityMax = Max(availability);
        for (int i = 0; i < availability.Length; i++)
        {
            availability[i] = availability[i] == 0f ? 1f : 1f / availability[i];
        }

        #endregion

        #region Usefulness

        usefulness = new float[5];
        usefulness[Lumber] += player.availableBuildings[Village] + player.availableBuildings[Street];
        usefulness[Brick] += player.availableBuildings[Village] + player.availableBuildings[Street];
        usefulness[Wool] += player.availableBuildings[Village];
        usefulness[Grain] += player.availableBuildings[Village] + 2 * player.availableBuildings[City];
        usefulness[Ore] += 3 * player.availableBuildings[City];

        useFulnessMax = Max(usefulness);

        for (int i = 0; i < usefulness.Length; i++)
        {
            usefulness[i] = useFulnessMax == 0f ? 1f : usefulness[i] / useFulnessMax;
            resourceValues[i] = CalculateValue(i, player.resources[i]);
        }

        #endregion

        dirty = false;
    }

    public void StartTrading()
    {
        if(Sum(player.resources) > 7)
        {
            Proposal p = GetNextProposal();
            int tries = 0;
            while(p != Proposal.Empty && tries <= 200)
            {
                if (bank.Accept(p))
                {
                    GameController.singleton.availableResources[p.resToTake] -= p.numToTake;
                    GameController.singleton.availableResources[p.resToGive] += p.numToGive;
                    player.resources[p.resToTake] += p.numToTake;
                    player.resources[p.resToGive] -= p.numToGive;
                    Notifier.singleton.Notify(p.ToString(this, bank));
                    p = GetNextProposal();
                }
                else
                {
                    break;
                }

                tries++;
            }

        }
        else
        {
            return;
        }
    }

    public Proposal GetNextProposal()
    {
        int highestResource = -1;
        int highestNumber = int.MinValue;

        int lowestResource = -1;
        int lowestNumber = int.MaxValue;

        // Go through all resources of our player...
        for (int i = 0; i < player.resources.Length; i++)
        {
            int rn = player.resources[i]; // The current resource we are checking

            // If our current resource is the one we have the most of (until now) AND... 
            // ( We have at least 2 and are connected to a trading harbor of this resources OR...
            // We have at least 3 and are connected to a random harbor OR...
            // We have at least 4 )
            if(rn > highestNumber && (rn >= 2 && player.harbors.Contains(i) || rn >= 3 && player.harbors.Contains(RandomHarbor) || rn >= 4))
            {
                highestNumber = rn;
                highestResource = i;
            }
            else if(rn < lowestNumber && rn < 3)
            {
                lowestNumber = rn;
                lowestResource = i;
            }
           
        }

        if(lowestNumber >= 4 || lowestNumber >= highestNumber || highestResource == -1 || highestNumber <= 0 || lowestResource == -1) { return Proposal.Empty; }
        else if(highestNumber >= 2 && player.harbors.Contains(highestResource))
        {
            return new Proposal(highestResource, lowestResource, 2, 1);
        }
        else if(highestNumber >= 3 && player.harbors.Contains(RandomHarbor))
        {
            return new Proposal(highestResource, lowestResource, 3, 1);
        }
        else if(highestNumber >= 4)
        {
            return new Proposal(highestResource, lowestResource, 4, 1);
        }

        return Proposal.Empty;
    }

    public bool Accept(Proposal proposal)
    {
        // Take is TAKEN FROM US and give = GIVEN TO US

        if(proposal == Proposal.Empty) { return false; }

        // Only accept trades if we are the bank
        if (isBank) 
        {
            HashSet<int> harbors = PlayerManager.singleton.CurrentPlayer.harbors;
            GameController gc = GetComponent<GameController>();

            // Always deny if the bank doesn't have the resources
            if(gc.availableResources[proposal.resToTake] < proposal.numToTake) { return false; }
            // If the current player is on a harbor that allows him/her to trade 2:1, only accept those
            if(harbors.Contains(proposal.resToGive) && proposal.numToGive == 2 && proposal.numToTake == 1) { return true; }
            // Else if the current player is on a randomharbor, only allow 3:1 trades
            else if(harbors.Contains(RandomHarbor) && proposal.numToGive == 3 && proposal.numToTake == 1) { return true; }
            // Otherwise, only accept 4:1 ratio trades
            else if(proposal.numToGive == 4 && proposal.numToTake == 1) { return true; }
            else { return false; }
        }

        return false;
    }

    public SortedList<float, Proposal> GetAllPossibleProposals()
    {
        /*
        float max = float.MinValue;
        int maxRes = Desert;

        float min = float.MaxValue;
        int minRes = Desert;

        for (int resourceID = 0; resourceID < resourceValues.Length; resourceID++)
        {
            float value = resourceValues[resourceID];
            if(value > max) { max = value; maxRes = resourceID; }
            else if(value < min && player.resources[resourceID] >= 1) { min = value; minRes = resourceID; }
        }


        if (maxRes == Desert || minRes == Desert || maxRes == minRes) { return new SortedList<float, Proposal>(); }
        */

        SortedList<float, Proposal> proposals = new SortedList<float, Proposal>();

        // The resource to take from the other trader
        for (int rtt = 0; rtt < 5; rtt++)
        {
            // The number of resources to take from the other trader
            for (int ntt = 1; ntt < 5; ntt++)
            {
                // If we can only trade with the bank and we want more than 1, than it's not possible
                if(GameController.singleton.bankTradeOnly && ntt > 1) { continue; }

                for (int rtg = 0; rtg < 5; rtg++)
                {
                    if(rtt == rtg) { continue; }

                    for (int ntg = 0; ntg < 5; ntg++)
                    {
                        // If we can only trade with the bank, we can never accept trades worse than the best trade to the bank
                        if(ntg > player.resources[rtg]) { continue; }
                        else if (GameController.singleton.bankTradeOnly && (player.harbors.Contains(rtg) && ntg != 2 || player.harbors.Contains(RandomHarbor) && ntg != 3 || ntg != 4)) { continue; }
                        
                        Proposal prop = new Proposal(rtg, rtt, ntg, ntt);
                        float value = WeighOwnProposal(prop);
                        if(value > 0f && !proposals.ContainsKey(value))
                        {
                            proposals.Add(value, prop);
                        }
                    }
                }
            }
        }

        /*
        // Our number to give has a minimum of 1
        for (int numToGive = 1; numToGive <= 4; numToGive++)
        {
            if (numToGive > player.resources[minRes]) { continue; }
            // Trades do to 4 maximum. A trade of 4 to 1 is only accepted by the bank
            for (int numToTake = 1; numToTake <= 4; numToTake++)
            {
                Proposal prop = new Proposal(minRes, maxRes, numToGive, numToTake);
                float value = WeighOwnProposal(prop);
                if (value > 0f && !proposals.ContainsKey(value)) 
                { 
                    proposals.Add(value, prop); 
                }
            }
        }
        */


        return proposals;
    }

    // Calculate the value of a resource at a hypothetical number of available resources of the type res
    private float CalculateValue(int resourceID, int number)
    {
        if(number == 0f) { return 1f; }
        return Math.Min(usefulness[resourceID] * availability[resourceID], 1f);
        //return (usefulness[resourceID] + availability[resourceID]) / (2f * number);
        //return usefulness[resourceID] * number;
    }

    private float WeighOwnProposal(Proposal proposal)
    {
        Proposal inverted = new Proposal(proposal.resToTake, proposal.resToGive, proposal.numToTake, proposal.numToGive);
        return WeighProposal(inverted);
    }
    private float WeighProposal(Proposal proposal)
    {
        // Give is GIVEN TO US, take is TAKEN FROM US!

        // Our hypothetical value of the given and taken resources if the proposal would go through
        float newGive = CalculateValue(proposal.resToGive, player.resources[proposal.resToGive] + proposal.numToGive);
        float newTake = CalculateValue(proposal.resToTake, Math.Max(player.resources[proposal.resToTake] - proposal.numToTake, 0));

        return newGive - resourceValues[proposal.resToGive] + (newTake - resourceValues[proposal.resToTake]);

        /*
        // The differences with the mean of the current and hypothetical 'new' values
        float oldGiveDiff = Mathf.Abs(mean - resourceValues[proposal.resToGive]);
        float oldTakeDiff = Mathf.Abs(mean - resourceValues[proposal.resToTake]);
        float newGiveDiff = Mathf.Abs(mean - newGive);
        float newTakeDiff = Mathf.Abs(mean - newTake);

        // If the old difference is bigger than the new difference, we are getting a good trade.
        return (oldGiveDiff - newGiveDiff) + (oldTakeDiff - newTakeDiff);
        */
    }

    public override string ToString()
    {
        if (isBank) { return "The Bank"; }
        else { return player.name; }
    }
}

public class Proposal
{
    public int resToGive; // The resources that the proposer is willing to give
    public int resToTake; // The resources that the proposer wants to get
    public int numToGive = 1; // The amount of resources the proposer is willing to give
    public int numToTake = 1; // The amount of resources the proposer want to get

    /// <summary>
    /// Create a new Proposal.
    /// </summary>
    /// <param name="rtg"> The resource the proposer wants to give in this trade. </param>
    /// <param name="rtt"> The resource the proposal wants to gain from the trade. </param>
    /// <param name="ntg"> The number of resources the proposer is willing to give. </param>
    /// <param name="ntt"> The number of resources the proposer want to get in this trade. </param>
    public Proposal(int resourceToGive, int resourceToTake, int ntg, int ntt)
    {
        if (resourceToGive == resourceToTake) { throw new System.Exception("Cannot create proposal with the same resources as give and take!"); }
        else if(ntg <= 0 || ntt <= 0) { throw new Exception("Cannot create proposal with 0 or less resources to give or take."); }
        else if(resourceToGive < 0 || resourceToGive > 4 || resourceToTake < 0 || resourceToTake > 4) { throw new Exception("Cannot create proposal with undefined resource : " + resourceToGive + " and " + resourceToTake); }
        this.resToGive = resourceToGive;
        this.resToTake = resourceToTake;
        this.numToGive = ntg;
        this.numToTake = ntt;
    }

    private Proposal() 
    {
        resToGive = 0;
        resToTake = 0;
        numToGive = 0;
        numToTake = 0;
    }

    public override string ToString()
    {
        return numToGive + " " + ResourceNames[resToGive] + " => " + numToTake + " " + ResourceNames[resToTake];
    }

    public string ToString(Trader from, Trader to)
    {
        return numToGive + " " + ResourceNames[resToGive] + " (" + from.ToString() + ") => " + numToTake + " " + ResourceNames[resToTake] + " (" + to.ToString() + ")";
    }

    public static Proposal Empty
    {
        get { return new Proposal(); }
    }

    public static bool operator ==(Proposal p1, Proposal p2)
    {
        return p1.resToGive == p2.resToGive && p1.resToTake == p2.resToTake && p1.numToGive == p2.numToGive && p1.numToTake == p2.numToTake;
    }

    public static bool operator !=(Proposal p1, Proposal p2)
    {
        return !(p1 == p2);
    }

    public override bool Equals(object obj)
    {
        return this == (Proposal)obj;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
