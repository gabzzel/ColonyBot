using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Enums;

public class TileGridPoint : GridPoint
{
    private HashSet<NonTileGridPoint> connectedNonTileGridPoints = new HashSet<NonTileGridPoint>();
    private Tile tile = null;

    public TileGridPoint(Vector2 position, Vector2Int colRow)
    {
        this.position = position;
        this.colRow = colRow;
    }

    public Tile Tile
    {
        get { return tile; }
        set { if(tile == null) { tile = value; } }
    }
    public Resource Resource
    {
        get {

            if (tile != null) { return tile.Resource; }
            else { throw new System.Exception("No tile associated with this GridPoint"); }
        }
    }
    public float Value { get { return tile.Value; } }
    public HashSet<NonTileGridPoint> ConnectedNTGPs { get { return connectedNonTileGridPoints; } }

    /// <summary>
    /// Check whether this TileGridPoint is connected to a certain NonTileGridPoint.
    /// </summary>
    /// <param name="ntgp"> The NonTileGridPoint to check for. </param>
    /// <returns> Whether the two are connected. </returns>
    public bool IsConnectedTo(NonTileGridPoint ntgp) { return connectedNonTileGridPoints.Contains(ntgp); }

    /// <summary>
    /// Connect this TileGridPoint to a certain TileGridPoint
    /// </summary>
    /// <param name="ntgp"> The NonTileGridPoint to connect to. </param>
    public void Connect(NonTileGridPoint ntgp)
    {
        if (connectedNonTileGridPoints.Count >= 6) { throw new System.Exception("Cannot connect more than 6 NTGP's to this TGP!");  }
        else { connectedNonTileGridPoints.Add(ntgp); }
    }
}
