﻿using UnityEngine;
using static Utility;

public class NonTileGridPoint : GridPoint
{
    private readonly float[] resourceValues = new float[5];
    //private Dictionary<NonTileGridPoint, Building> connections = new Dictionary<NonTileGridPoint, Building>();
    //private List<TileGridPoint> connectedTileGridPoints = new List<TileGridPoint>();

    private Building building = null;

    public NonTileGridPoint(Vector2 position, Vector2Int colRow)
    {
        this.position = position;
        resourceValues = new float[5];
        this.colRow = colRow;
    }

    public float Value { get; private set; } = 0f;
    public Building Building
    {
        get { return building; }
        set
        {
            if(value == null) { throw new System.Exception("Cannot build 'null' on a GridPoint."); }
            else if(value.Type == Village && building != null) { throw new System.Exception("Cannot build two villages on one GridPoint."); }
            else if(value.Type == Street) { throw new System.Exception("Cannot build a street ON a GridPoint!"); }
            else if(value.Type == City && value.Owner != building.Owner) { throw new System.Exception("Cannot build city on someone else's village!"); }
            else {
                if (building != null) { GameObject.Destroy(building.gameObject); }
                building = value;
            }
        }
    }
    //public List<TileGridPoint> ConnectedTGPs { get { return connectedTileGridPoints; } }
    //public Dictionary<NonTileGridPoint, Building> ConnectedNTGPs { get { return connections; } }

    /// <summary>
    /// Get the value of this NonTileGridPoint of a certain resource
    /// </summary>
    /// <param name="resourceID"> The resource id of which to get the value </param>
    /// <returns> The value of the resource </returns>
    public float ResourceValue(int resourceID) { return resourceValues[resourceID]; }

    /// <summary>
    /// Connect this NTGP to a TGP. Also updates the values of this NTGP accordingly.
    /// </summary>
    /// <param name="tgp"> To which TileGridPoint to connect. </param>
    public override void Connect(int index)
    {
        if (BoardController.tgpIndexes.Contains(index))
        {
            TileGridPoint tgp = (TileGridPoint)BoardController.singleton.allGridPoints[index];
            if (tgp.Resource != Desert)
            {
                resourceValues[tgp.Resource] += tgp.Value;
                Value += tgp.Value / 3f;
            }
        }
        base.Connect(index); 
    }

    public void Connect(int neighbourIndex, Building street)
    {
        if(!BoardController.ntgpIndexes.Contains(neighbourIndex)) { throw new System.Exception("Cannot connect " + ToString() + " to NTGP that does not exist!"); }
        else if(street == null) { throw new System.Exception("Cannot connect " + ToString() + " to " + BoardController.singleton.allGridPoints[neighbourIndex].ToString() + " with null street!"); }
        else if (BoardController.singleton.connections[index, neighbourIndex] == 0) { throw new System.Exception("Cannot connect " + ToString() + " with " + BoardController.singleton.allGridPoints[neighbourIndex].ToString() + " because they are not connected!"); }
        else if(BoardController.singleton.connections[index, neighbourIndex] != 1) 
        { 
            throw new System.Exception("Cannot connect " + ToString() + " with " + BoardController.singleton.allGridPoints[neighbourIndex].ToString() + 
                " for " + street.Owner.name + " because there already is a street between them from " + PlayerManager.singleton.players[BoardController.singleton.connections[index, neighbourIndex] - 2].name + "!"); 
        }
        BoardController.singleton.connections[index, neighbourIndex] = 2 + street.Owner.ID; // Correction for the fact that 0 = not connected, 1 = connected and everything above is player street connections
    }

    /// <summary>
    /// Check whether this GridPoint is occupied by a certain player (using a village or city)
    /// </summary>
    /// <param name="player"> The Player for which to check. </param>
    /// <returns> Whether this NTGP is occupied by the player </returns>
    public bool OccupiedBy(ColonyPlayer player)
    {
        if (player == null) { throw new System.Exception("Cannot check occupation for null player!"); }
        return OccupiedBy(player.ID);
    }

    public bool OccupiedBy(int playerID)
    {
        if(Building == null) { return false; }
        return Building.Owner.ID == playerID;
    }

    /// <summary>
    /// Check if this gridpoint is eligible for village placement
    /// </summary>
    /// <param name="player"> The player for which to check </param>
    /// <returns> Whether this gridpoint is eligible </returns>
    public bool HasStreetConnectionForPlayer(int playerID)
    {
        int corrected = 2 + playerID;
        foreach(int neighbourIndex in connectedIndexes)
        {
            if(BoardController.singleton.connections[index, neighbourIndex] == corrected) { return true; }
        }
        return false;
    }

    /// <summary>
    /// Checks which neighbour we can connect to by street to be connected to something from the player
    /// </summary>
    /// <param name="player"> The player whos street or village/city we should connect to </param>
    /// <returns> An NTGP that we can use to connect to the players' village or street </returns>
    public NonTileGridPoint FindConnectionWithPlayer(ColonyPlayer player)
    {
        if(player == null) { throw new System.ArgumentNullException("Player", "Cannot check connection to null player."); }

        foreach(int neighbourIndex in connectedIndexes)
        {
            if (!BoardController.ntgpIndexes.Contains(neighbourIndex)) { continue; }
            NonTileGridPoint neighbour = (NonTileGridPoint)BoardController.singleton.allGridPoints[neighbourIndex];

            // If our neighbour is occupied by our player and we are not yet connected with a street, we are golden.
            if (neighbour.OccupiedBy(player) && BoardController.singleton.connections[index, neighbour.index] == 1) 
            { 
                return neighbour; 
            }

            // If not, we should go over every street connection and find one with our player
            foreach(int secondLevelNeighbourIndex in neighbour.connectedIndexes)
            {
                if(secondLevelNeighbourIndex == index) { continue; }
                if(BoardController.singleton.connections[neighbourIndex, secondLevelNeighbourIndex] == 2 + player.ID && BoardController.singleton.connections[index, neighbour.index] == 1) { return neighbour; }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks whether we have an NTGP neighbour that is currently occupied by any player
    /// </summary>
    /// <returns> True if we have a occupied neighbour, False otherwise. </returns>
    public bool NeighbourOccupied()
    {
        foreach(int neighbourIndex in connectedIndexes)
        {
            if (BoardController.ntgpIndexes.Contains(neighbourIndex))
            {
                NonTileGridPoint neighbour = (NonTileGridPoint)BoardController.singleton.allGridPoints[neighbourIndex];
                if(neighbour.Building != null) { return true; }
            }
        }

        return false;
    }

    /// <summary>
    /// Return an occupied neighbour so we connect to that neighbour with a street.
    /// </summary>
    /// <param name="playerID"> The id of ther player that wants to build a street towards us </param>
    /// <returns> A NTGP that we can connect our street to as starting point. </returns>
    public NonTileGridPoint OccupiedNeighbour(int playerID)
    {
        foreach(int neighbourIndex in connectedIndexes)
        {
            if (!BoardController.ntgpIndexes.Contains(neighbourIndex)) { continue; }
            NonTileGridPoint neighbour = (NonTileGridPoint)BoardController.singleton.allGridPoints[neighbourIndex];
            if (neighbour.OccupiedBy(playerID)) { return neighbour; }
        }
        return null;
    }

    public NonTileGridPoint NeighbourWithConnectionToPlayer(int playerID)
    {
        foreach(int neighbourIndex in connectedIndexes)
        {
            if (BoardController.ntgpIndexes.Contains(neighbourIndex))
            {
                NonTileGridPoint neighbour = (NonTileGridPoint)BoardController.singleton.allGridPoints[neighbourIndex];
                foreach(int secondDegreeIndex in neighbour.connectedIndexes)
                {
                    if (BoardController.ntgpIndexes.Contains(secondDegreeIndex))
                    {
                        if (BoardController.singleton.connections[neighbourIndex, secondDegreeIndex] == 2 + playerID) { return neighbour; }
                    }
                }
            }          
        }
        return null;
    }

    public override string ToString()
    {
        return index.ToString();
    }
}
