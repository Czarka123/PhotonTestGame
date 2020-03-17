using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
public class GameSetupController : MonoBehaviour
{
    public bool createbot;
    // This script will be added to any multiplayer scene
    void Start()
    {
        CreatePlayer(); //Create a networked player object for each player that loads into the multiplayer scenes.
    }
    private void CreatePlayer()
    {
        Debug.Log("Creating Player");
        PhotonNetwork.Instantiate("PlayerModel", Vector3.zero, Quaternion.identity);

        if (createbot)
        {
            Debug.Log("Creating Bot");
            PhotonNetwork.Instantiate("BotModel", Vector3.one, Quaternion.identity);
        }
    }
}