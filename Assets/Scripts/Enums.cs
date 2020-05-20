using System;
using System.Linq;
using System.Collections.Generic;

public static class Enums
{
    public static Dictionary<Resource, int> DefaultResDictInt
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
    public static Dictionary<Resource, float> DefaultResDictFloat
    {
        get
        {
            return new Dictionary<Resource, float>()
            {
                {Resource.Wood, 0f },
                {Resource.Stone, 0f },
                {Resource.Wool, 0f },
                {Resource.Grain, 0f },
                {Resource.Ore, 0f }
            };
        }
    }
    public static Dictionary<BuildingType, int> DefaultBuildingDict
    {
        get
        {
            return new Dictionary<BuildingType, int>()
            {
                {BuildingType.Village, 5 },
                {BuildingType.City, 4 },
                {BuildingType.Street, 15 }
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

    public enum TurnPhase
    {
        Build,
        Pass
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
    public static int GetBuildingTypeNumber(BuildingType buildingType)
    {
        List<BuildingType> actions = Enum.GetValues(typeof(BuildingType)).Cast<BuildingType>().ToList();
        return actions.IndexOf(buildingType);
    }

    /// <summary>
    /// Get the building type by index.
    /// </summary>
    /// <param name="i"> The index of the building type in the enum </param>
    /// <returns> The corresponding building type </returns>
    public static BuildingType GetBuildingTypeByNumber(int i)
    {
        List<BuildingType> types = Enum.GetValues(typeof(BuildingType)).Cast<BuildingType>().ToList();
        return types[i];
    }

    public static BuildingType GetAction(float[] array)
    {
        if (array.Length != Enum.GetValues(typeof(BuildingType)).Cast<BuildingType>().ToArray().Length)
        {
            throw new Exception("Cannot convert array to action: Length mismatch");
        }
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] >= 1)
            {
                return GetBuildingTypeByNumber(i);
            }
        }
        return BuildingType.Village;
    }

    public static List<Resource> GetResourcesAsList(bool includeNone = false)
    {
        List<Resource> l = Enum.GetValues(typeof(Resource)).Cast<Resource>().ToList();
        if (!includeNone) { l.Remove(Resource.None); }
        return l;
    }
}
