using UnityEngine;
using System;

public class Line : IEquatable<Line>
{
    public GridPoint p1;
    public GridPoint p2;
    public Vector2 position = Vector2.zero;
    public float rotation = 0;
    public GameObject street = null;

    public Line(GridPoint p1, GridPoint p2, GameObject street)
    {
        this.p1 = p1;
        this.p2 = p2;
        position = p1.position - p2.position;
        rotation = Vector2.SignedAngle(p1.position, p2.position);
        this.street = street;
    }

    public bool Equals(Line other)
    {
        if(other == null) { return false; }
        return other == this;
    }

    public override bool Equals(object obj)
    {
        return this.Equals((Line)obj);
    }

    public static bool operator ==(Line line1, Line line2){

        return (line1.p1 == line2.p1 && line1.p2 == line2.p2) || (line1.p2 == line2.p1 && line1.p1 == line2.p2);
    }

    public static bool operator != (Line line1, Line line2)
    {
        return !(line1 == line2);
    }

    public GridPoint GetOther(GridPoint yourself)
    {
        if(p1 == yourself) { return p2; }
        else if(p2 == yourself) { return p1; }

        return null;
    }
}
