using System;
using System.Linq;
using System.Collections.Generic;

public static class Enums
{
    public static Dictionary<Resource, int> DefaultResDict
    {
        get
        {
            return new Dictionary<Resource, int>()
            {
                {Resource.Wood, 0 },
                {Resource.Stone, 0 },
                {Resource.Wool, 0 },
                {Resource.Grain, 0 },
                {Resource.Ore, 0 }
            };
        }
    }  

    public enum Resource
    {
        None,
        Wood,
        Stone,
        Wool,
        Grain,
        Ore
    }

    public enum Action
    {
        Pass,
        BuildVillage,
        BuildRoad,
        BuildCity
    }

    public enum BuildingType
    {
        Street,
        Village,
        City
    }

    /// <summary>
    /// Get the index of a certain action
    /// </summary>
    /// <param name="action"> The action of which to get the index of </param>
    /// <returns> The index / number of the action </returns>
    public static int GetActionNumber(Action action)
    {
        List<Action> actions = Enum.GetValues(typeof(Action)).Cast<Action>().ToList();
        return actions.IndexOf(action);
    }

    public static Action GetActionByNumber(int i)
    {
        List<Action> actions = Enum.GetValues(typeof(Action)).Cast<Action>().ToList();
        return actions[i];
    }

    public static Action GetAction(float[] array)
    {
        if (array.Length != Enum.GetValues(typeof(Action)).Cast<Action>().ToArray().Length)
        {
            throw new Exception("Cannot convert array to action: Length mismatch");
        }
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] >= 1)
            {
                return GetActionByNumber(i);
            }
        }
        return Action.Pass;
    }

    public static List<Resource> GetResourcesAsList(bool includeNone = false)
    {
        List<Resource> l = Enum.GetValues(typeof(Resource)).Cast<Resource>().ToList();
        if (!includeNone) { l.Remove(Resource.None); }
        return l;
    }
}
