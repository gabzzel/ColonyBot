using System;

public static class Utility
{
     /* Building Types */
    public const int Street = 0;
    public const int Village = 1;
    public const int City = 2;

    public static string[] BuildingNames = new string[] { "Street", "Village", "City" };

    /* Resource Types */
    public const int Wood = 0;
    public const int Stone = 1;
    public const int Wool = 2;
    public const int Grain = 3;
    public const int Ore = 4;
    public const int Desert = 5;

    public static string[] ResourceNames = new string[] { "Wood", "Stone", "Wool", "Grain", "Ore", "Desert"};

    /* Development Card Types */
    public const int Knight = 0;
    public const int VictoryPoint = 1;
    public const int Unusable = 2;

    /* Action indexes */
    public const int Pass = 0;
    // 1 - 163 are reserved for building!
    public const int BuyDevelopmentCard = 163;
    public const int PlayKnightCard = 164;

    public enum TurnPhase
    {
        Build,
        Pass
    }

    /// <summary>
    /// Get the maximum value in some int[]
    /// </summary>
    /// <param name="array"> The array in which to find the maximum value </param>
    /// <returns> The maximum value </returns>
    public static int Max(int[] array)
    {
        if(array == null || array.Length == 0) { throw new Exception("Cannot determine maximum value in array with no length!"); }
        int max = int.MinValue;
        foreach(int value in array) { if(value > max) { max = value; } }
        return max;
    }

    public static float Max(float[] array)
    {
        if (array == null || array.Length == 0) { throw new Exception("Cannot determine maximum value in array with no length!"); }
        float max = float.MinValue;
        foreach (float value in array) { if (value > max) { max = value; } }
        return max;
    }

    public static int Min(int[] array)
    {
        if (array == null || array.Length == 0) { throw new Exception("Cannot determine maximum value in array with no length!"); }
        int min = int.MaxValue;
        foreach (int value in array) { if (value < min) { min = value; } }
        return min;
    }

    public static float Min(float[] array)
    {
        if (array == null || array.Length == 0) { throw new Exception("Cannot determine maximum value in array with no length!"); }
        float min = float.MaxValue;
        foreach (float value in array) { if (value < min) { min = value; } }
        return min;
    }
}
