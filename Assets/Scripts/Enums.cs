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

    public static int GetActionNumber(Action action)
    {
        List<Action> actions = Enum.GetValues(typeof(Action)).Cast<Action>().ToList();
        return actions.IndexOf(action);
    }

    public static List<Resource> GetResourcesAsList()
    {
        return Enum.GetValues(typeof(Resource)).Cast<Resource>().ToList();
    }
}
