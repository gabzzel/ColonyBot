using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class Tile : MonoBehaviour
{
    [SerializeField] private Text text = null;
    public Enums.Resource resource = Enums.Resource.None;
    public int number = 0;
    public float value = 0;
    public GridPoint gridPoint = null;

    public void SetGridPoint(GridPoint gp)
    {
        this.gridPoint = gp;
    }

    public void InitializeTile(Enums.Resource resource, int number)
    {
        this.SetResource(resource);
        this.SetNumber(number);
        text = transform.GetChild(0).GetComponent<Text>();
    }

    public void SetResource(Enums.Resource res)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

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
            this.gridPoint.UpdateNeighbourValues();
        }
        else
        {
            text.text = "";
        }       
    }

    public List<Tile> GetNeighbouringTiles()
    {
        List<Tile> result = new List<Tile>();

        // Get all first level neighbours
        foreach(GridPoint gp in gridPoint.GetNeighbouringGridPoints())
        {
            // Get all second level neighbours
            foreach(GridPoint gp2 in gp.GetNeighbouringGridPoints())
            {
                // If we are not looking at ourselves and it has a tile...
                if(gp2 != gridPoint && gp2.isMiddle)
                {
                    result.Add(gp2.GetTile());
                }
                
            }
        }
        return result;
    }

    public override string ToString()
    {
        return "Tile @ " + gridPoint.colRow.ToString() + ": " + resource.ToString() + " - " + number;
    }
}

