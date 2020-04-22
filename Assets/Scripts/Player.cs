using UnityEngine;

[System.Serializable]
public class Player : MonoBehaviour
{
    public string name = "";

    public Enums.Action RequestAction()
    {
        return Enums.Action.Pass;
    }
}
