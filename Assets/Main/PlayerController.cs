using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    float speed = 4;
    float rotationSpeed = 80;
    float rotation = 0;
    float gravity = 9;



    Vector3 moveDir = Vector3.zero;

    CharacterController controller;
    Animator animat;
    PhotonView photonView;

    [SerializeField]
    private Camera playerCamera;
    [SerializeField]
    private AudioListener playerAudio;


    void Start()
    {
        controller = GetComponent<CharacterController>();
        animat = GetComponent<Animator>();
        photonView = GetComponent<PhotonView>();

        if (photonView.IsMine)
        {

        }
        else
        {
            // playerCamera.SetActive(false);
            Destroy(playerCamera);
            Destroy(playerAudio);
        }

    }


   
    void Update()
    {
        if (photonView.IsMine)
        {
            Movment();
        }
     


    }

    //private void MovesOutsidePlayerView()
    //{
    //    if (Input.GetKeyUp(KeyCode.C))
    //    {
    //        Debug.Log("Creating REEEEEED BOT");
    //        StartCoroutine(ExampleCoroutine());
    //        //PhotonNetwork.Instantiate("RedServerClientTime", Vector3.one, Quaternion.identity);
    //    }
    //}

    [PunRPC]
    void CreateBot(string botString)
    {
        PhotonNetwork.Instantiate(botString, Vector3.zero, Quaternion.identity);
    }

    [PunRPC]
    void CreateBot(string botString, Vector3 postion)
    {
        PhotonNetwork.Instantiate(botString, postion, Quaternion.identity);
    }




    //IEnumerator ExampleCoroutine()
    //{

    //    //yield on a new YieldInstruction that waits for 5 seconds.
    //    yield return new WaitForSeconds(1);
    //    PhotonNetwork.Instantiate("RedServerClientTime", Vector3.one, Quaternion.identity);


    //}

    private void Movment()
    {
        if (controller.isGrounded)
        {
            if (Input.GetKey(KeyCode.W))
            {
                animat.SetInteger("condition", 1);
                moveDir = new Vector3(0, 0, 1);
                moveDir *= speed;
                moveDir = transform.TransformDirection(moveDir);

            }
            if (Input.GetKeyUp(KeyCode.W))
            {
                animat.SetInteger("condition", 0);
                moveDir = new Vector3(0, 0, 0);
            }
            if (Input.GetKey(KeyCode.S))
            {
                animat.SetInteger("condition", 2);
                moveDir = new Vector3(0, 0, -1);
                moveDir *= speed;
                moveDir = transform.TransformDirection(moveDir);

            }
            if (Input.GetKeyUp(KeyCode.S))
            {
                animat.SetInteger("condition", 0);
                moveDir = new Vector3(0, 0, 0);
            }
            if (Input.GetKeyUp(KeyCode.B))
            {
                Debug.Log("Creating Bot");
                PhotonNetwork.Instantiate("BotModel", Vector3.one, Quaternion.identity);
            }
            if (Input.GetKeyUp(KeyCode.C))
            {
                // Debug.Log("Creating Red bot");
                PhotonNetwork.Instantiate("GreenLocalClientTime", Vector3.zero, Quaternion.identity);
                //PhotonNetwork.Instantiate("TargetBot", new Vector3(20, 0.2f, 14.65f), Quaternion.identity);
                photonView.RPC("CreateBot", RpcTarget.Others, "RedServerClientTime");
                photonView.RPC("CreateBot", RpcTarget.Others, "TargetBot",new Vector3(20, 0.2f, 14.65f));

            }
            if (Input.GetKeyUp(KeyCode.U))
            {
                // Debug.Log("Creating Red bot");
                PhotonNetwork.Instantiate("GreenCube", Vector3.one, Quaternion.identity);

                photonView.RPC("CreateBot", RpcTarget.Others, "RedCube");

            }



        }
        rotation += Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;
        transform.eulerAngles = new Vector3(0, rotation, 0);

        moveDir.y -= gravity * Time.deltaTime;
        controller.Move(moveDir * Time.deltaTime);
    }

}
