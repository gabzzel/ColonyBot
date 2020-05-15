using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using static Enums;

[System.Serializable]
public class Tile : MonoBehaviour
{
    [SerializeField] private Text text = null;
    private Resource resource = Resource.None;
    private int number = 0;
    private float value = 0f;
    private TileGridPoint gridPoint = null;

    public TileGridPoint GridPoint
    {
        get { return gridPoint; }
        set
        {
            if(gridPoint == null) { gridPoint = value; }
        }
    }
    public Resource Resource { get { return resource; } }
    public int Number { get { return number; } }
    public float Value { get { return value; } }

    public void SetResource(Resource res, Sprite sprite)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        /*
        switch (res)
        {
            case Enums.Resource.None:
                sr.color = Color.black;
                break;
            case Enums.Resource.Wood:
                sr.color = Color.green;
                break;
            case Enums.Resource.Stone:
                sr.color = Color.red;
                break;
            case Enums.Resource.Wool:
                sr.color = Color.white;
                break;
            case Enums.Resource.Grain:
                sr.color = Color.yellow;
                break;
            case Enums.Resource.Ore:
                sr.color = Color.gray;
                break;
            default:
                break;
        }
        */ 
        resource = res;
    }

    public void SetNumber(int number)
    {
        this.number = number;

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

            this.value = (6f - Mathf.Abs(7f - number)) / 36f;
        }
        else
        {
            text.text = "";
        }       
    }

    public List<Tile> GetNeighbouringTiles()
    {
        List<Tile> result = new List<Tile>();

        // Get all connected NonTileGridPoints
        foreach(NonTileGridPoint ntgp in gridPoint.ConnectedNTGPs)
        {
            // Get all second level neighbours
            foreach(TileGridPoint tgp in ntgp.ConnectedTGPs)
            {
                // If we are not looking at ourselves and it has a tile...
                if(tgp != gridPoint) { result.Add(tgp.Tile); }
                
            }
        }
        return result;
    }

    public override string ToString()
    {
        return "Tile @ " + gridPoint.colRow.ToString() + ": " + resource.ToString() + " - " + number;
    }
}

