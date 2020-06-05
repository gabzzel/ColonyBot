using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using static Utility;

[System.Serializable]
public class Tile : MonoBehaviour
{
    [SerializeField] private Text text = null;
    private TileGridPoint gridPoint = null;

    public TileGridPoint GridPoint
    {
        get { return gridPoint; }
        set
        {
            if(gridPoint == null) { gridPoint = value; }
        }
    }
    public int Resource { get; private set; } = Desert;
    public int Number { get; private set; } = 0;
    public float Value { get; private set; } = 0f;

    public void SetResource(int resourceID)
    {
        GetComponent<SpriteRenderer>().sprite = BoardController.singleton.ResourceSprites[resourceID];
        Resource = resourceID;
    }

    public void SetNumber(int number)
    {
        this.Number = number;

        if (number != 0)
        {
            text.text = number.ToString();

            if(number == 6 || number == 8)
            {
                text.fontStyle = FontStyle.Bold;
                text.fontSize = 36;
            }
            else
            {
                text.fontStyle = FontStyle.Normal;
                text.fontSize = 30;
            }

            this.Value = (6f - Mathf.Abs(7f - number)) / 36f;
        }
        else
        {
            text.text = "";
        }       
    }

    public List<Tile> GetNeighbouringTiles()
    { 
        List<Tile> result = new List<Tile>();
        foreach(int neighbourIndex in gridPoint.connectedNTGPs)
        {
            GridPoint gp = BoardController.singleton.allGridPoints[neighbourIndex];
            foreach(int secondDegreeIndex in gp.connectedTGPs)
            {
                if (neighbourIndex != gridPoint.index)
                {
                    result.Add(((TileGridPoint)BoardController.singleton.allGridPoints[secondDegreeIndex]).Tile);
                }
            }
        }

        return result;
    }

    public override string ToString()
    {
        return "Tile @ " + gridPoint.position.ToString() + ": " + Resource.ToString() + " - " + Number;
    }
}

