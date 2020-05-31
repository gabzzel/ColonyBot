using System;
using System.Collections.Generic;
using UnityEngine;
using static Utility;

public class Trader : MonoBehaviour
{
    public bool dirty = false;
    public bool isBank = false;
    List<Trader> otherTraders = new List<Trader>();

    private float[] resourceValues = new float[5];
    private float[] availability = new float[5];
    float availabilityMax = 1f;
    private float[] usefulness = new float[5];
    float useFulnessMax = 1f;

    float mean = 0.5f;
    ColonyPlayer player = null;

    private void Awake()
    {
        foreach (Trader trader in FindObjectsOfType<Trader>())
        {
            if (trader != this)
            {
                otherTraders.Add(trader);
            }
        }
        if (!isBank)
        {
            player = GetComponent<ColonyPlayer>();
        }

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

    public void UpdateResourceValues()
    {
        mean = 0.5f;
        /* Availability */
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
        availabilityMax = float.MinValue;
        // Determine max value for normalisation

        for (int i = 0; i < availability.Length; i++)
        {
            if (availability[i] > availabilityMax) { availabilityMax = availability[i]; }

            // Normalize the availability and multiply by our current resources in hand. 
            // We divide 1 by the availability because the value of the resource is the opposite of the avaibility.
            // (> available) means (< value)

            for (int resourceID = 0; resourceID < player.resources.Length; resourceID++)
            {
                availability[resourceID] = CalculateAvailabilityValue(resourceID, player.resources[resourceID]);
            }

        }
        /* Usefulness */
        usefulness = new float[5];
        usefulness[Wood] += player.buildingOptions[Village] + player.buildingOptions[Street];
        usefulness[Stone] += player.buildingOptions[Village] + player.buildingOptions[Street];
        usefulness[Wool] += player.buildingOptions[Village];
        usefulness[Grain] += player.buildingOptions[Village] + 2 * player.buildingOptions[City];
        usefulness[Ore] += 3 * player.buildingOptions[City];

        useFulnessMax = Max(usefulness);

        for (int i = 0; i < usefulness.Length; i++)
        {
            usefulness[i] /= useFulnessMax;
            resourceValues[i] = usefulness[i] * availability[i];
        }

        dirty = false;
    }


    public void StartTrading()
    {
        // Get all possible proposals based on our resources
        //List<Proposal> props = GetAllPossibleProposals(true);
        UpdateResourceValues();
        SortedList<float, Proposal> props = GetAllPossibleProposals();
        int tries = 0;
        while (props.Count > 0 && tries < 1000)
        {
            tries++;
            if (dirty) { UpdateResourceValues(); } // Update our resource values before trading
            Proposal prop = props.Values[props.Count - 1]; // Get the best proposal for us
            props.RemoveAt(props.Count - 1); // Remove it from the list

            // Propose it to all other traders
            foreach (Trader t in otherTraders)
            {
                // If they accept..
                if (t.Accept(prop))
                {
                    // Give and remove the resources according to the proposal
                    player.GiveResources(prop.resToTake, prop.numToTake);
                    player.GiveResources(prop.resToGive, -1 * prop.numToGive);
                    string notification = prop.ToString(this, t);
                    Notifier.singleton.Notify(notification);
                    props = GetAllPossibleProposals(); // Update the new proposal we can go for
                    break;
                }
            }
        }
    }

    public bool Accept(Proposal proposal)
    {
        // Take is TAKEN FROM US and give = GIVEN TO US

        // If we are the bank, we only accept trades that are 4 to 1. 
        if (isBank && proposal.numToGive == 4 && proposal.numToTake == 1) { return true; }
        else if (isBank) { return false; }

        // Accept everything for now, if we can afford it
        if (player.resources[proposal.resToTake] < proposal.numToTake) { return false; }
        else if (WeighProposal(proposal) > 0f)
        {
            player.resources[proposal.resToTake] -= proposal.numToTake;
            player.resources[proposal.resToGive] += proposal.numToGive;
            return true; // TODO!
        }
        return false;
    }

    public SortedList<float, Proposal> GetAllPossibleProposals()
    {
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

        SortedList<float, Proposal> proposals = new SortedList<float, Proposal>();

        // Our number to give has a minimum of 1
        for (int numToGive = 1; numToGive <= 4; numToGive++)
        {
            if (numToGive > player.resources[minRes]) { continue; }
            // Trades do to 4 maximum. A trade of 4 to 1 is only accepted by the bank
            for (int numToTake = 1; numToTake <= 4; numToTake++)
            {
                Proposal prop = new Proposal(minRes, maxRes, numToGive, numToTake);
                float value = WeighOwnProposal(prop);
                if (value > 0f && !proposals.ContainsKey(value)) { proposals.Add(value, prop); }
            }
        }

        return proposals;
    }

    // Calculate the value of a resource at a hypothetical number of available resources of the type res
    private float CalculateAvailabilityValue(int resourceID, int number)
    {
        if (availability[resourceID] == 0 || number == 0 || this.availabilityMax == float.MinValue) { return 1f; }
        else { return 1f / (availability[resourceID] / (this.availabilityMax * number)); }

        // x = (... / availabilityMax)
        //float x = (1f / availability[res]) / player.resources[res];
        //float _new = x * number; // The availability value if we would have 'number' amount of resources of this type
        //return _new;
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
        float newGive = CalculateAvailabilityValue(proposal.resToGive, player.resources[proposal.resToGive] + proposal.numToGive) * usefulness[proposal.resToGive];
        float newTake = CalculateAvailabilityValue(proposal.resToTake, Math.Max(player.resources[proposal.resToTake] - proposal.numToTake, 0)) * usefulness[proposal.resToTake];

        // The differences with the mean of the current and hypothetical 'new' values
        float oldGiveDiff = Mathf.Abs(mean - resourceValues[proposal.resToGive]);
        float oldTakeDiff = Mathf.Abs(mean - resourceValues[proposal.resToTake]);
        float newGiveDiff = Mathf.Abs(mean - newGive);
        float newTakeDiff = Mathf.Abs(mean - newTake);

        // If the old difference is bigger than the new difference, we are getting a good trade.
        return (oldGiveDiff - newGiveDiff) + (oldTakeDiff - newTakeDiff);
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
        this.resToGive = resourceToGive;
        this.resToTake = resourceToTake;
        this.numToGive = ntg;
        this.numToTake = ntt;
    }
    public override string ToString()
    {
        return numToGive + " " + resToGive + " => " + numToTake + " " + resToTake;
    }

    public string ToString(Trader from, Trader to)
    {
        return numToGive + " " + resToGive + " (" + from.ToString() + ") => " + numToTake + " " + resToTake + " (" + to.ToString() + ")";
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
