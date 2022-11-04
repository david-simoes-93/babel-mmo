using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSelectionFunctions : MonoBehaviour
{
    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() { }

    /// <summary>
    /// Client picked Fighter
    /// </summary>
    public void ButtonFighter_Connect()
    {
        ClientGameLoop.CGL.ConnectToWorldServer(Globals.UnitEntityCode.kFighter);
    }

    /// <summary>
    /// Client picked Mage
    /// </summary>
    public void ButtonMage_Connect()
    {
        ClientGameLoop.CGL.ConnectToWorldServer(Globals.UnitEntityCode.kMage);
    }

    /// <summary>
    /// Client picked Sniper
    /// </summary>
    public void ButtonSniper_Connect()
    {
        ClientGameLoop.CGL.ConnectToWorldServer(Globals.UnitEntityCode.kSniper);
    }

    /// <summary>
    /// Client exits char selection
    /// </summary>
    public void ButtonExit()
    {
        ClientGameLoop.Close(true);
    }
}
