using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Enums;

public class NonTileGridPoint : GridPoint
{
    private Dictionary<Resource, float> resourceValues = new Dictionary<Resource, float>();
    private Dictionary<NonTileGridPoint, Building> connections = new Dictionary<NonTileGridPoint, Building>();
    private List<TileGridPoint> connectedTileGridPoints = new List<TileGridPoint>();

    private float totalValue = 0f;
    private Building building = null;

    public NonTileGridPoint(Vector2 position, Vector2Int colRow)
    {
        this.position = position;
        this.colRow = colRow;
        resourceValues = Enums.DefaultResDictFloat;
    }

    public float Value
    {
        get { return totalValue; }
    }
    public Building Building
    {
        get { return building; }
        set
        {
            if(value == null) { throw new System.Exception("Cannot build 'null' on a GridPoint."); }
            else if(value.Type == BuildingType.Village && building != null) { throw new System.Exception("Cannot build two villages on one GridPoint."); }
            else if(value.Type == BuildingType.Street) { throw new System.Exception("Cannot build a street ON a GridPoint!"); }
            else if(value.Type == BuildingType.City && value.Owner != building.Owner) { throw new System.Exception("Cannot build city on someone else's village!"); }
            else {
                if (building != null) { GameObject.Destroy(building.gameObject); }
                building = value;
            }
        }
    }
    public List<TileGridPoint> ConnectedTGPs { get { return connectedTileGridPoints; } }
    public Dictionary<NonTileGridPoint, Building> ConnectedNTGPs { get { return connections; } }

    /// <summary>
    /// Get the value of this NonTileGridPoint of a certain resource
    /// </summary>
    /// <param name="resource"> The resource of which to get the value </param>
    /// <returns> The value of the resource </returns>
    public float ResourceValue(Resource resource)
    {
        return resourceValues[resource];
    }

    /// <summary>
    /// Connect this NTGP to a TGP. Also updates the values of this NTGP accordingly.
    /// </summary>
    /// <param name="tgp"> To which TileGridPoint to connect. </param>
    public void Connect(TileGridPoint tgp)
    {
        if(connectedTileGridPoints.Count >= 3) { throw new System.Exception("Cannot connect more than 3 TGP's to " + ToString()); }
        connectedTileGridPoints.Add(tgp);
        if(tgp.Resource != Resource.None)
        {
            resourceValues[tgp.Resource] += tgp.Value;
            totalValue += tgp.Value / 3f;
        }
        
    }

    /// <summary>
    /// Connect this NTGP to another NTGP, possible by a street.
    /// </summary>
    /// <param name="ntgp"></param>
    /// <param name="street"> The Street between this and the other. </param>
    public void Connect(NonTileGridPoint ntgp, Building street = null)
    {
        if(ntgp == null) { throw new System.Exception("Cannot connect NTGP to null NTGP!"); }
        else if (connections.ContainsKey(ntgp)) { connections[ntgp] = street; }
        else { connections.Add(ntgp, null); }
    }

    /// <summary>
    /// Check whether this NTGP is connected to another NTGP.
    /// </summary>
    /// <param name="ntgp"> The connection to check. </param>
    /// <param name="withStreet"> Whether they should be connected by a street </param>
    /// <returns></returns>
    public bool IsConnectedTo(NonTileGridPoint ntgp, bool withStreet)
    {
        return connections.ContainsKey(ntgp) && (!withStreet || connections[ntgp] == null);
    }

    /// <summary>
    /// Check whether this NTGP is connected to another NTGP by a street owned by a certain player.
    /// </summary>
    /// <param name="ntgp"> The connection to check. </param>
    /// <param name="player"> The player whos street it should be. </param>
    /// <returns> Whether the two are connected via a street owned by ColonyPlayer player. </returns>
    public bool IsConnectedTo(NonTileGridPoint ntgp, ColonyPlayer player)
    {
        if (!connections.ContainsKey(ntgp)) { return false; }
        else if(player == null) { throw new System.Exception("Cannot check a connection if player is null!"); }
        else if(connections[ntgp] == null) { return false; }
        else if(connections[ntgp].Owner != player) { return false; }
        return true;
    }

    /// <summary>
    /// Check whether this GridPoint is occupied by a certain player (using a village or city)
    /// </summary>
    /// <param name="player"> The Player for which to check. </param>
    /// <returns> Whether this NTGP is occupied by the player </returns>
    public bool OccupiedBy(ColonyPlayer player)
    {
        if(Building == null) { return false; }
        else if(player == null) { throw new System.Exception("Cannot check occupation for null player!"); }
        return Building.Owner == player;
    }

    /// <summary>
    /// Check if this gridpoint is eligible for village placement
    /// </summary>
    /// <param name="player"> The player for which to check </param>
    /// <returns> Whether this gridpoint is eligible </returns>
    public bool HasStreetConnectionForPlayer(ColonyPlayer player)
    {
        // Go through all neighbours...
        foreach (KeyValuePair<NonTileGridPoint, Building> connection in ConnectedNTGPs)
        {
            // If we are connected to a neighbour with a street that belongs to us, return true
            if (connection.Value != null && connection.Value.Owner == player) { return true; }
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

        foreach(NonTileGridPoint neighbour in ConnectedNTGPs.Keys)
        {
            // If our neighbour is occupied by our player, we are golden.
            if (neighbour.OccupiedBy(player)) { return neighbour; }

            // If not, we should go over every street connection and find one with our player
            foreach(NonTileGridPoint neighbour2 in neighbour.ConnectedNTGPs.Keys)
            {
                Building street = neighbour.ConnectedNTGPs[neighbour2];
                if(street != null && street.Owner == player) { return neighbour; }
            }
        }

        return null;
    }

    public override string ToString()
    {
        return "(NonTile) GridPoint @ " + colRow.ToString();
    }
}
