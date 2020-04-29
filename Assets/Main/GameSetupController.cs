using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSetupController : MonoBehaviour
{
   // public bool createbot;
    // This script will be added to any multiplayer scene
    void Start()
    {
        CreatePlayer(); //Create a networked player object for each player that loads into the multiplayer scenes.
    }
    private void CreatePlayer()
    {
        Debug.Log("Creating Player");
        PhotonNetwork.Instantiate("PlayerModel", Vector3.zero, Quaternion.identity);

     
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {

            StartCoroutine(DisconnectPlayer());
        }

    }
        //Application.LoadLevel("Menu");
       
    IEnumerator DisconnectPlayer()
    {

        PhotonNetwork.Disconnect();
        yield return new WaitUntil(() => !PhotonNetwork.IsConnected);
        Debug.Log("Leaving game");
        SceneManager.LoadScene("Menu");


    }

}