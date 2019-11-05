using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviourPunCallbacks
{

    [SerializeField]
    private GameObject StartButton;
    [SerializeField]
    private GameObject CancelButton; 
    [SerializeField]
    private int RoomSize;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        StartButton.SetActive(true);
    }

    public void QuickStart() //For the start button
    {
        StartButton.SetActive(false);
        CancelButton.SetActive(true);
        PhotonNetwork.JoinRandomRoom(); 
        Debug.Log("Starting");
    }


    public override void OnJoinRandomFailed(short returnCode, string message) 
    {
        Debug.Log("Room not found...creating new one");
        CreateRoom();
    }
    void CreateRoom() //trying to create our own room
    {
        Debug.Log("Creating room ");
        int randomRoomNumber = Random.Range(0, 10000); //creating a random name for the room
        RoomOptions roomOps = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = (byte)RoomSize };
        PhotonNetwork.CreateRoom("Room" + randomRoomNumber, roomOps); //attempting to create a new room
        Debug.Log(randomRoomNumber);
    }

    public override void OnCreateRoomFailed(short returnCode, string message) 
    {
        Debug.Log("Failed to create room... trying again");
        CreateRoom(); //once again
    }

    public void Cancel()
    {
        StartButton.SetActive(false);
        StartButton.SetActive(true);
        PhotonNetwork.LeaveRoom();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
