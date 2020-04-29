using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotMovement : MonoBehaviour
{
    float speed = 4;
    float gravity = 9;

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
    System.IO.StreamWriter logfile;
    Vector3 moveDir = Vector3.zero;

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

        logfile = new System.IO.StreamWriter(@"C:\Users\Godzinski\gitRepos\PhotonTestGame\Assets\textLogFile2.txt");
        logfile.WriteLine("Second test ");
        StartTime = Time.time;
    }

    private void MovmentOrders()
    {

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
                logfile.WriteLine("Bot rotated " + "Time is " + Time.time);
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
                logfile.WriteLine("Bot rotated " + "Time is " + Time.time);
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
                logfile.WriteLine("Bot rotated " + "Time is " + Time.time);
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
                logfile.WriteLine("Bot rotated " + "Time is " + Time.time);
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
                logfile.WriteLine("Bot rotated " + "Time is " + Time.time);
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
            RPC_Shooting();
            logfile.WriteLine("Bot is shouting " + "Time is " + Time.time);
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
            logfile.WriteLine("Bot test ended");
            Debug.Log("Bot test ended");
        }
    }

    [PunRPC]
    void RPC_Shooting()
    {
        RaycastHit hit;
        FireFlash.Play();
        if (Physics.Raycast(rayOrgin.position, rayOrgin.TransformDirection(Vector3.forward), out hit, 700))
        {
            Debug.DrawRay(rayOrgin.position, rayOrgin.TransformDirection(Vector3.forward) * hit.distance * 700, Color.red);
            Debug.Log("hit");
            if (hit.transform.tag == "Player")
            {
                Debug.Log("player hit");
            }

        }
        else
        {
            Debug.DrawRay(rayOrgin.position, rayOrgin.TransformDirection(Vector3.forward) * hit.distance * 700, Color.white);
            Debug.Log("no hit");
        }
    }


    void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            MovmentOrders();
        }
        
        Debug.Log("Bot position is " + transform.position + "Time is " + Time.time);
        logfile.WriteLine("Bot position is " + transform.position + "Time is " + Time.time);
        
    }
}
