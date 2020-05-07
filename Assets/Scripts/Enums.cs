using System;
using System.Linq;
using System.Collections.Generic;

public static class Enums
{
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

    public static List<Resource> GetResourcesAsList()
    {
        return Enum.GetValues(typeof(Resource)).Cast<Resource>().ToList();
    }
}
