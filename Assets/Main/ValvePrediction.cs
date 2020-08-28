using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValvePrediction : MonoBehaviour, IPunObservable, IOnEventCallback
{
    float speed = 4;
    float gravity = 9;
    float rtt = 0;

    CharacterController controller;
    Animator animat;
    PhotonView photonView;
    public Transform rayOrgin;
    [SerializeField]
    private Camera playerCamera;
    [SerializeField]
    private AudioListener playerAudio;


    System.IO.StreamWriter logfile;
    System.IO.StreamWriter botlogfile;

    private Vector3 moveDir = Vector3.zero;
    [SerializeField]
    private BotColor botColor;
    [SerializeField]
    private ParticleSystem FireFlash;
    [SerializeField]
    private GameObject LaserEffect;

    private GameObject LaserObject;

    public BotState BotState;

    List<Usercmd> history;
    List<Usercmd> ServerHistory;

    double StartTime;
    float MyLocalTime;
    int rotationCounter = 0;
    int ServerHistoryIterator=0;
    bool shooting = false;

    private void Rotate(Vector3 MoveDirection, float detaTime)
    {
        BotState = BotState.Rotating;
        Usercmd newCmd = new Usercmd(Time.deltaTime,PhotonNetwork.Time, MoveDirection.z, MoveDirection.x, transform.rotation.eulerAngles.y,transform.position.x, transform.position.y);
        history.Add(newCmd);
      
        Quaternion toRotation = Quaternion.FromToRotation(transform.up, MoveDirection);
        transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, speed * Time.time);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(MoveDirection), detaTime*speed);
    }

    IEnumerator Rotate(float Angle)
    {
        float moveSpeed = 2f;
        float correction = 1;

        Debug.Log("Rotate " + transform.rotation.eulerAngles.y + " < " + Angle);
        while (transform.rotation.eulerAngles.y < Angle- correction)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, Angle, 0), moveSpeed * Time.deltaTime);
            //Debug.Log("Lerp " + transform.rotation.eulerAngles.y + " < " + Angle);
            yield return null;
        }
        transform.rotation = Quaternion.Euler(0, Angle, 0);
        Debug.Log("Exit "); ;
        yield return null;
    }

    private void Move(Vector3 MoveDirection, float detaTime)
    {
        BotState = BotState.Moving;

        animat.SetInteger("condition", 1);
        moveDir= MoveDirection;
        Vector3 mD = MoveDirection;
        mD *= speed;
        mD = transform.TransformDirection(mD);

        mD.y -= gravity * detaTime;
     //   Debug.Log("MOVEEE " + botColor + " :" + mD);
        controller.Move(mD * detaTime);    
    }

    private void Shooting()
    {
        RaycastHit hit;
        FireFlash.Play();
        StartCoroutine(LaserAnimation(0.2f));
        shooting = true;
        if (Physics.Raycast(rayOrgin.position, rayOrgin.TransformDirection(Vector3.forward), out hit, 700))
        {
            Debug.DrawRay(rayOrgin.position, rayOrgin.TransformDirection(Vector3.forward) * hit.distance * 700, Color.red);
            Debug.Log("hit");
            if (hit.transform.tag == "Player")
            {
                Debug.Log("player hit");
            }
            else if (hit.transform.tag == "Target")
            {
                Debug.Log("Target hit at postion "+ hit.transform.position + " "+ botColor + " " + photonView.IsMine);
                if (botColor == BotColor.Green && photonView.IsMine)
                {
                    Usercmd hitReport = new Usercmd(Time.time, PhotonNetwork.Time, 0, 0, 0, hit.transform.position.x, hit.transform.position.z);

                    byte evCode = 1;
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                    SendOptions sendOptions = new SendOptions { Reliability = true };                
                    PhotonNetwork.RaiseEvent(evCode, hitReport, raiseEventOptions, sendOptions);
                    Debug.Log("sending to target");
                }
            }


        }
        else
        {
            Debug.DrawRay(rayOrgin.position, rayOrgin.TransformDirection(Vector3.forward) * hit.distance * 700, Color.white);
            Debug.Log("no hit");
        }
    }

    private void EnableLaser()
    {
        LaserObject.SetActive(true);
        LaserObject.GetComponentInChildren<ParticleSystem>().Play();
    }

    IEnumerator LaserAnimation(float time)
    {
        EnableLaser();

        yield return new WaitForSeconds(time);

        LaserObject.SetActive(false);
    }



    void ExecuteCommand(Usercmd command)
    {
        if(command.forwardmove !=0 || command.sidemove != 0)
        {
            Vector3 MoveDirection = new Vector3(command.sidemove, 0, command.forwardmove);
            Move(MoveDirection, Time.deltaTime);
        }
        else if (command.forwardmove == 0 && command.sidemove == 0)
        {
           // Debug.Log("STANNNNDING");
            Stand();
        }

        if(command.rotationAngle != transform.rotation.eulerAngles.y)
        {
            transform.rotation= Quaternion.Euler( new Vector3(0, command.rotationAngle, 0));
        }

        if(command.shooting)
        {
           // Debug.Log("SHOOOOOOTING");
            Shooting();
        }
    }

    private void Stand()
    {
        moveDir = Vector3.zero;
        animat.SetInteger("condition", 0);
        BotState = BotState.Standing;

    }


    void Start()
    {
        history = new List<Usercmd>();
        ServerHistory = new List<Usercmd>();
        controller = GetComponent<CharacterController>();
        animat = GetComponent<Animator>();
        photonView = GetComponent<PhotonView>();
        Destroy(playerCamera);
        Destroy(playerAudio);

        LaserObject = Instantiate(LaserEffect, rayOrgin);
        LaserObject.SetActive(false);

        controller.detectCollisions = false;
   
        BotState = BotState.Standing;

        StartTime = PhotonNetwork.Time;
        MyLocalTime = Time.time;

        PhotonPeer.RegisterType(typeof(Usercmd), 2, Usercmd.Serialize, Usercmd.Deserialize);

        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 60;
    }

    void TargetMovmentScript()
    {
        if (Mathf.CeilToInt(Time.time) % 4 == 0 || Mathf.CeilToInt(Time.time) % 4 == 1)
        {
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);

        }
        else if (Mathf.CeilToInt(Time.time) % 4 == 2 || Mathf.CeilToInt(Time.time) % 4 == 3)
        {
            Move(new Vector3(0, 0, -1.0f), Time.deltaTime);
        }

        //Stand();
    }

    void MovmentScript()
    {
        if (MyLocalTime + 3 > Time.time)
        {
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);

        }
        else if (MyLocalTime + 6 > Time.time && MyLocalTime + 3 < Time.time)
        {
            if (rotationCounter == 0)
            {
                rotationCounter++;
                StartCoroutine(Rotate(90));
            }

            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 8 > Time.time && MyLocalTime + 6 < Time.time)
        {          
            Shooting();
            Stand();
        }
        else
        {
            Stand();
        }
    }

    /*
      if(MyLocalTime+2>Time.time)
        {
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);

            
        }
        else if (MyLocalTime + 3 > Time.time && MyLocalTime + 2 < Time.time)
        {
            if (rotationCounter == 0)
            {
                rotationCounter++;
                StartCoroutine(Rotate(90));
            }

            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if(MyLocalTime + 5 > Time.time && MyLocalTime + 3 < Time.time)
        {
         
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 7 > Time.time && MyLocalTime + 5 < Time.time)
        {
            if (rotationCounter == 1)
            {
                rotationCounter++;
                StartCoroutine(Rotate(190));
            }

            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 8 > Time.time && MyLocalTime + 7< Time.time)
        {

            Move(new Vector3(1.0f, 0, 0), Time.deltaTime);
        }
        else if (MyLocalTime + 10 > Time.time && MyLocalTime + 8 < Time.time)
        {
            Stand();
            Shooting();
        }
        else
        {
            Stand();
        } 
     */




    // Update is called once per frame
    void FixedUpdate()
    {
        if (botColor != BotColor.Target)
        {
            if (photonView.IsMine)
            {
                shooting = false; //maybe I can do that better
                MovmentScript();

                Usercmd newCmd = new Usercmd(Time.deltaTime, PhotonNetwork.Time, moveDir.z, moveDir.x, transform.rotation.eulerAngles.y, transform.position.x, transform.position.y, shooting);
                history.Add(newCmd);

            }
            else
            {
                Read();
            }
        }
        else
        {
            if (photonView.IsMine)
            {
                TargetMovmentScript();
                Usercmd newCmd = new Usercmd(Time.deltaTime, PhotonNetwork.Time, moveDir.z, moveDir.x, transform.rotation.eulerAngles.y, transform.position.x, transform.position.y, shooting);
                history.Add(newCmd);
            }
            else
            {
                Read();
            }
        }
    }

    void Read()
    {
        if(ServerHistory!=null && ServerHistory.Count != 0 && ServerHistoryIterator < ServerHistory.Count)
        {
            ExecuteCommand(ServerHistory[ServerHistoryIterator]);          
            history.Add(ServerHistory[ServerHistoryIterator]);
            ServerHistoryIterator++;
        }
        else
        {
            Debug.Log("predicting");
            if (history != null && history.Count != 0)
            {
                ExecuteCommand(history[history.Count - 1]);
                history.Add(history[history.Count - 1]);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {

            if (history != null && history.Count > 0)
            {
                stream.SendNext(history[history.Count - 1]);
            }
        }
        else
        {
            Usercmd recivepack = (Usercmd)stream.ReceiveNext();
            if (botColor == BotColor.Target)
            {
                Debug.Log(" got " + recivepack.sidemove + "   s " + recivepack.forwardmove + " rotation angle " + recivepack.rotationAngle + " postion " + recivepack.postionX + recivepack.postionY + " shooting " + recivepack.shooting);
            }
            if (ServerHistory != null)
                ServerHistory.Add(recivepack);
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        Debug.Log("msg to " + botColor + " " + photonEvent.Code);
        if (photonEvent.Code == 1)
        {
           
            if (botColor == BotColor.Target)
            {
                Usercmd action = (Usercmd)photonEvent.CustomData;
                Debug.Log("got hit on client " + action.timestamp + " at postion X" + action.postionX + " Z " + action.postionY);
                Debug.Log("Locally on hit I'm " + PhotonNetwork.Time + " at postion " + transform.position);
            }
        }
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}
