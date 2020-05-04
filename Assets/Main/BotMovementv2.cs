using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotMovementv2: MonoBehaviour
{
    float speed = 4;
    float gravity = 9;

    public delegate void SendMyTimeDelegate(string time,bool Isgreen);
    public static event SendMyTimeDelegate SendMyTimeListners;

    CharacterController controller;
    Animator animat;
    PhotonView photonView;
    public Transform rayOrgin;
    [SerializeField]
    private Camera playerCamera;
    [SerializeField]
    private AudioListener playerAudio;
    private float StartTime;
    private int rotationCounter = 0;

    private string otherBotTime;

    //System.IO.StreamWriter logfile;
    Vector3 moveDir = Vector3.zero;
    [SerializeField]
    private bool isBotGreen;
    [SerializeField]
    private ParticleSystem FireFlash;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        animat = GetComponent<Animator>();
        photonView = GetComponent<PhotonView>();

        Destroy(playerCamera);
        Destroy(playerAudio);

       // logfile = new System.IO.StreamWriter(@"C:\Users\Godzinski\gitRepos\PhotonTestGame\Assets\textLogFile2.txt");
       // logfile.WriteLine("Second test ");
        StartTime = Time.time;
    }

    private void MovmentOrders()
    {

        SendMyTimeListners.Invoke(Time.time.ToString(), isBotGreen);
        photonView.RPC("RPC_getTime", RpcTarget.Others);
       

        if (Time.time < StartTime + 3)
        {
            animat.SetInteger("condition", 1);
            moveDir = new Vector3(0, 0, 1);
            moveDir *= speed;
            moveDir = transform.TransformDirection(moveDir);

            moveDir.y -= gravity * Time.deltaTime;
            controller.Move(moveDir * Time.deltaTime);
            //Debug.Log(Time.time+" bot is moving");
        }
        else if (Time.time > StartTime + 3 && Time.time < StartTime + 3.5)
        {
            if (rotationCounter == 0)
            {
                rotationCounter++;
                transform.Rotate(new Vector3(0, 90, 0));
               
            }
            animat.SetInteger("condition", 1);
            moveDir = new Vector3(0, 0, -1);
            moveDir *= speed;
            moveDir = transform.TransformDirection(moveDir);

            moveDir.y -= gravity * Time.deltaTime;
            controller.Move(moveDir * Time.deltaTime);

        }
        else if (Time.time > StartTime + 3.5 && Time.time < StartTime + 7)
        {
            if (rotationCounter == 1)
            {
                rotationCounter++;
                transform.Rotate(new Vector3(0, 90, 0));
          
            }
            animat.SetInteger("condition", 1);
            moveDir = new Vector3(0, 0, 1);
            moveDir *= speed;
            moveDir = transform.TransformDirection(moveDir);

            moveDir.y -= gravity * Time.deltaTime;
            controller.Move(moveDir * Time.deltaTime);
        }
        else if (Time.time > StartTime + 7 && Time.time < StartTime + 9)
        {
            if (rotationCounter == 2)
            {
                rotationCounter++;
                transform.Rotate(new Vector3(0, 30, 0));
         
            }
            animat.SetInteger("condition", 1);
            moveDir = new Vector3(0, 0, 1);
            moveDir *= speed;
            moveDir = transform.TransformDirection(moveDir);

            moveDir.y -= gravity * Time.deltaTime;
            controller.Move(moveDir * Time.deltaTime);
        }
        else if (Time.time > StartTime + 9 && Time.time < StartTime + 10)
        {
            if (rotationCounter == 3)
            {
                rotationCounter++;
                transform.Rotate(new Vector3(0, -45, 0));
 
            }
            animat.SetInteger("condition", 1);
            moveDir = new Vector3(0, 0, 1);
            moveDir *= speed;
            moveDir = transform.TransformDirection(moveDir);

            moveDir.y -= gravity * Time.deltaTime;
            controller.Move(moveDir * Time.deltaTime);
        }
        else if (Time.time > StartTime + 10 && Time.time < StartTime + 11)
        {
            if (rotationCounter == 4)
            {
                rotationCounter++;
                transform.Rotate(new Vector3(0, 100, 0));
            }
            animat.SetInteger("condition", 1);
            moveDir = new Vector3(0, 0, 1);
            moveDir *= speed;
            moveDir = transform.TransformDirection(moveDir);

            moveDir.y -= gravity * Time.deltaTime;
            controller.Move(moveDir * Time.deltaTime);
        }
        else if (Time.time > StartTime + 11 && Time.time < StartTime + 12)
        {
            animat.SetInteger("condition", 0);
            photonView.RPC("RPC_Shooting", RpcTarget.All);

        }
        else if (Time.time > StartTime + 12 && Time.time < StartTime + 14)
        {
            animat.SetInteger("condition", 1);
            moveDir = new Vector3(0, 0, 1);
            moveDir *= speed;
            moveDir = transform.TransformDirection(moveDir);

            moveDir.y -= gravity * Time.deltaTime;
            controller.Move(moveDir * Time.deltaTime);
        }
        else if (Time.time > StartTime + 14)
        {
            animat.SetInteger("condition", 0);

            Debug.Log("Bot test ended");
        }
    }

    [PunRPC]
    void RPC_getTime()
    {
        SendMyTimeListners.Invoke(Time.time.ToString(), isBotGreen);
    }




    void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            MovmentOrders();
        }
      //  Debug.Log("Bot position is " + transform.position + "Time is " + Time.time);
        
    }
}
