using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BoardController : MonoBehaviour
{

    public List<Tile> tiles = new List<Tile>();

    public void Initialize()
    {
        if(tiles.Count < 19)
        {
            Debug.LogWarning("Not all tiles assigned!");
            return;
        }
    }
}

[System.Serializable]
public class Tile
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

    public string name = "";
    public Resource resource = Resource.None;
    public int number = 0;
    public GameObject tileObject = null;
}

[CustomEditor(typeof(BoardController))]
public class BoardControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BoardController bc = (BoardController)target;

        if (GUILayout.Button("Initialize"))
        {
            bc.Initialize();
        }

        base.DrawDefaultInspector();
    }
    
}