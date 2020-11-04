using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class ValvePredictionV2 : MonoBehaviour, IPunObservable, IOnEventCallback
{
    float speed = 4;
    float gravity = 9;
    double extrpolationTimeLimit = 0.2;

    CharacterController controller;
    Animator animat;
    PhotonView photonView;
    public Transform rayOrgin;
    [SerializeField]
    private Camera playerCamera;
    [SerializeField]
    private AudioListener playerAudio;

    private Vector3 moveDir = Vector3.zero;
    [SerializeField]
    private BotColor botColor;
    [SerializeField]
    private ParticleSystem FireFlash;
    [SerializeField]
    private GameObject LaserEffect;
    private GameObject LaserObject;
    private BotState BotState;

    double StartTime;
    float MyLocalTime;

    List<Usercmd> history;
    List<Usercmd> PredictionToBeAccepted;
    Queue<Usercmd> toSend;
    Queue<Usercmd> ReciveBuffor;

    int rotationCounter = 0;
    bool shooting = false;
    int sendCounter = 0;

    bool smoothCorrection = false;
    Vector3 CSDiffrence;
    float MaxCorrectionTime = 0.1f;
    float CurrentCorrectionTime = 0;
    Vector3 AppliedCorrection;

    Usercmd pastCommand;
    Usercmd targetInterpolationCommand;
    double InterpolationTimeGap;
    bool interpolating=false;
    double timepassed;
    float interpolationDistance=0;

    int testCounter=0;
    int SaveCounter = 0;

    StringBuilder logfile;

    void Start()
    {
        PhotonPeer.RegisterType(typeof(Usercmd), 2, Usercmd.Serialize, Usercmd.Deserialize);

        controller = GetComponent<CharacterController>();
        animat = GetComponent<Animator>();
        photonView = GetComponent<PhotonView>();
        Destroy(playerCamera);
        Destroy(playerAudio);
        controller.detectCollisions = false;
        BotState = BotState.Standing;

        LaserObject = Instantiate(LaserEffect, rayOrgin);
        LaserObject.SetActive(false);
        history = new List<Usercmd>();
        PredictionToBeAccepted = new List<Usercmd>();
        toSend = new Queue<Usercmd>();
        ReciveBuffor = new Queue<Usercmd>();
        Usercmd currentCommand= new Usercmd();
        Usercmd targetInterpolationCommand = new Usercmd();

        StartTime = PhotonNetwork.Time;
        MyLocalTime = Time.time;

        CSDiffrence = Vector3.zero;
        AppliedCorrection = Vector3.zero;

        logfile = new StringBuilder();   
        var newLine = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", "Index", "PostionX", "PostionY" ,"PostionZ", "RotationAngle", "PhotonTime");
        logfile.AppendLine(newLine);

        PhotonNetwork.SendRate = 80;
        PhotonNetwork.SerializationRate = 80;

    }

    void SaveCurrentStateToFile()
    {

        var newLine = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", SaveCounter, transform.position.x, transform.position.y, transform.position.z, Mathf.Ceil(transform.rotation.eulerAngles.y), PhotonNetwork.Time);
        logfile.AppendLine(newLine);
    }

    void FixedUpdate()
    {
        SaveCounter++;
        if (photonView.IsMine && botColor == BotColor.Green)
        {
            Usercmd newCmd = GenerateTestCommand();

            ExecuteCommand(newCmd);
            SmoothCorrection();
            newCmd.postionX = transform.position.x;
            newCmd.postionY = transform.position.z;

            PredictionToBeAccepted.Add(newCmd);


            SaveCurrentStateToFile();

            byte evCode = 1;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendOptions sendOptions = new SendOptions { Reliability = true };
            PhotonNetwork.RaiseEvent(evCode, newCmd, raiseEventOptions, sendOptions);
            sendCounter = 0;


           
            sendCounter++;
        }
        else if (!photonView.IsMine && botColor != BotColor.Green)
        {
            SimulateEntity();
            SaveCurrentStateToFile();
        }

        if(SaveCounter==750)
        {
            string filename="";
            if (photonView.IsMine && botColor == BotColor.Green)
            {
                 filename = "Valve2Green";

            }
            else if (photonView.IsMine && botColor == BotColor.Red)
            {
                filename = "Valve2RedServer";
            }
            else if (!photonView.IsMine && botColor == BotColor.Red)
            {
                filename = "Valve2RedProxy";
            }

            File.WriteAllText(@"path" + filename + ".csv", logfile.ToString());

            Debug.Log(" Log Saved in file ");
        }
    }

    private Vector3 PredictPostion(Vector3 direction)
    {
        Vector3 FinalPostion = Vector3.zero;
 
        Vector3 mD = direction;

        mD *= speed;
        mD = transform.TransformDirection(mD);

        mD.y = 0;

        mD *= Time.deltaTime;
      
        FinalPostion = transform.position + mD;

        return FinalPostion;
    }

    private void Move(Vector3 MoveDirection, float detaTime)
    {
        BotState = BotState.Moving;

        animat.SetInteger("condition", 1);


        moveDir = MoveDirection;
        Vector3 mD = MoveDirection;

        mD *= speed;
        mD = transform.TransformDirection(mD);

        mD.y =0;

        mD *= Time.deltaTime;

        transform.position = transform.position + mD;
    }

    public void OnEvent(EventData photonEvent)
    {

       
        if (photonEvent.Code == 1)
        {

            if (botColor == BotColor.Red && photonView.IsMine)
            {

                Usercmd ClientCmd = (Usercmd)photonEvent.CustomData;
                //Debug.Log("recived action from client numba :" + action.sequence_number);
                ExecuteCommand(ClientCmd);
                SaveCurrentStateToFile();
                Usercmd ResultCommand = new Usercmd(Time.deltaTime, ClientCmd.timestamp, ClientCmd.forwardmove, ClientCmd.sidemove, transform.rotation.eulerAngles.y, transform.position.x, transform.position.z);

                Usercmd EntityCommand = new Usercmd(Time.deltaTime, PhotonNetwork.Time, ClientCmd.forwardmove, ClientCmd.sidemove, transform.rotation.eulerAngles.y, transform.position.x, transform.position.z);
                toSend.Enqueue(EntityCommand);



                byte evCode = 2;
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                SendOptions sendOptions = new SendOptions { Reliability = true };
                PhotonNetwork.RaiseEvent(evCode, ResultCommand, raiseEventOptions, sendOptions);


            }
           
        }
        if (photonEvent.Code == 2)
        {
            if (botColor == BotColor.Green && photonView.IsMine)
            {
                Usercmd ServerResult = (Usercmd)photonEvent.CustomData;
                ClientSidePrediction(ServerResult);

            }
        }
    }


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            Debug.Log(" send "+ botColor);

            if (botColor == BotColor.Red && photonView.IsMine)
            {
                Debug.Log(" sending " + toSend.Count);

                if (toSend !=null && toSend.Count>0)
                {
                    stream.SendNext(toSend.Dequeue());
                    Debug.Log(" sending to proxy ");
                }

            }
     
        }
        else
        {
            if (botColor == BotColor.Red && !photonView.IsMine)
            {
                
                Usercmd recivepack = (Usercmd)stream.ReceiveNext();
                if (ReciveBuffor != null)
                {
                    Debug.Log(" reading ");
                    ReciveBuffor.Enqueue(recivepack);
                }
            }
        }
    }

    void ExecuteCommand(Usercmd command)
    {
        if (command.forwardmove != 0 || command.sidemove != 0)
        {
            Vector3 MoveDirection = new Vector3(command.sidemove, 0, command.forwardmove);
            Move(MoveDirection, Time.deltaTime);
        }
        else if (command.forwardmove == 0 && command.sidemove == 0)
        {
            Stand();
        }

        if (command.rotationAngle != transform.rotation.eulerAngles.y)
        {
            transform.rotation = Quaternion.Euler(new Vector3(0, command.rotationAngle, 0));
        }

        if (command.shooting)
        {
            Shooting();
        }
    }

    private void Rotate(Vector3 MoveDirection, float detaTime)
    {
        BotState = BotState.Rotating;
        Usercmd newCmd = new Usercmd(Time.deltaTime, PhotonNetwork.Time, MoveDirection.z, MoveDirection.x, transform.rotation.eulerAngles.y, transform.position.x, transform.position.y);
        history.Add(newCmd);

        Quaternion toRotation = Quaternion.FromToRotation(transform.up, MoveDirection);
        transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, speed * Time.time);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(MoveDirection), detaTime * speed);
    }



    private void Stand()
    {
        moveDir = Vector3.zero;
        animat.SetInteger("condition", 0);
        BotState = BotState.Standing;

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
                Debug.Log("Target hit at postion " + hit.transform.position + " " + botColor + " " + photonView.IsMine);
                if (botColor == BotColor.Green && photonView.IsMine)
                {
                    Usercmd hitReport = new Usercmd(Time.time, PhotonNetwork.Time, 0, 0, 0, hit.transform.position.x, hit.transform.position.z);

                    byte evCode = 3;
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                    SendOptions sendOptions = new SendOptions { Reliability = true };
                    PhotonNetwork.RaiseEvent(evCode, hitReport, raiseEventOptions, sendOptions);
                }
            }

        }
        else
        {
            Debug.DrawRay(rayOrgin.position, rayOrgin.TransformDirection(Vector3.forward) * hit.distance * 700, Color.white);
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


    Usercmd GenerateTestCommand()
    {
        Usercmd generatedCommand = new Usercmd();
        double startTime = PhotonNetwork.Time;
        if (MyLocalTime + 3 > Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y, 0, 0, false);
        }
        else if (MyLocalTime +4 > Time.time && MyLocalTime + 3 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y, 0, 0, false);
            if (rotationCounter == 0)
            {
                rotationCounter++;
                generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y -90, 0, 0, false);
            }
           

        }
        else if (MyLocalTime +5 > Time.time && MyLocalTime + 4 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, -1f, 0, transform.rotation.eulerAngles.y, 0, 0, false);        
        }
        else if (MyLocalTime + 6 > Time.time && MyLocalTime + 5 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 0, 1f, transform.rotation.eulerAngles.y, 0, 0, false);
        }
        else if (MyLocalTime + 7 > Time.time && MyLocalTime + 6 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y, 0, 0, false);
            if (rotationCounter == 1)
            {
                rotationCounter++;
                generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y + 135, 0, 0, false);
            }
        }
        else if (MyLocalTime + 8 > Time.time && MyLocalTime + 7 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y, 0, 0, false);
            if (rotationCounter == 2)
            {
                rotationCounter++;
                generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y + 90, 0, 0, false);
            }
        }
        else if (MyLocalTime + 9 > Time.time && MyLocalTime + 8 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y, 0, 0, false);
            if (rotationCounter == 3)
            {
                rotationCounter++;
                generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y + 90, 0, 0, false);
            }
        }
        else if (MyLocalTime + 10 > Time.time && MyLocalTime + 9 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y, 0, 0, false);
            if (rotationCounter == 4)
            {
                rotationCounter++;
                generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y + 90, 0, 0, false);
            }
        }
        else if (MyLocalTime + 11 > Time.time && MyLocalTime + 10 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y, 0, 0, false);
            if (rotationCounter == 5)
            {
                rotationCounter++;
                
                generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y + 135, 0, 0, false);
            }
        }
        else if (MyLocalTime + 12 > Time.time && MyLocalTime + 11 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 0, 0, transform.rotation.eulerAngles.y, 0, 0, true);
          
        }
        else
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 0, 0, transform.rotation.eulerAngles.y, 0, 0, false);        
        }




        return generatedCommand;
    }


    Usercmd GenerateCommand()
    {
        Usercmd generatedCommand = new Usercmd();

        double startTime = PhotonNetwork.Time;

        if (MyLocalTime + 1 > Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 0, 0, transform.rotation.eulerAngles.y, 0, 0, false);
        }
        else if (MyLocalTime + 2 > Time.time && MyLocalTime + 1 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y, 0, 0, false);

        }
        else if (MyLocalTime + 3 > Time.time && MyLocalTime + 2 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y, 0, 0, false);
            if (rotationCounter == 0)
            {
                rotationCounter++;
                //  Move(new Vector3(1f, 0, 0), Time.deltaTime);
                // Move(new Vector3(1f, 0, 0), Time.deltaTime);
                // Move(new Vector3(1f, 0, 0), Time.deltaTime);
                Debug.Log(" noww ");
                generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y + 90, 0, 0, false);
            }
        }
        else if (MyLocalTime + 4 > Time.time && MyLocalTime + 3 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 0, -1f, transform.rotation.eulerAngles.y, 0, 0, false);

        }
        else if (MyLocalTime + 5 > Time.time && MyLocalTime + 4 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 0, 1f, transform.rotation.eulerAngles.y, 0, 0, false);

        }
        else if (MyLocalTime + 6 > Time.time && MyLocalTime + 5 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, -1f, 0, transform.rotation.eulerAngles.y, 0, 0, false);
            if (rotationCounter == 1)
            {
                rotationCounter++;
                generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y + 90, 0, 0, false);

            }


        }
        else if (MyLocalTime + 6.5 > Time.time && MyLocalTime + 6 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 1f, transform.rotation.eulerAngles.y, 0, 0, false);
            if (rotationCounter == 2)
            {
                rotationCounter++;
                generatedCommand = new Usercmd(Time.deltaTime, startTime, 1f, 0, transform.rotation.eulerAngles.y - 90, 0, 0, false);
            }


        }
        else if (MyLocalTime + 9 > Time.time && MyLocalTime + 6.5 < Time.time)
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 0, 0, transform.rotation.eulerAngles.y, 0, 0, true);
        }
        else
        {
            generatedCommand = new Usercmd(Time.deltaTime, startTime, 0, 0, transform.rotation.eulerAngles.y, 0, 0, false);
        }

        return generatedCommand;
    }

    private void ClientSidePrediction(Usercmd ServerResult)
    {

        bool found = false;
        bool accepted = true;
        foreach (Usercmd uc in PredictionToBeAccepted)
        {
            if (uc.timestamp == ServerResult.timestamp)
            {
                float dist = Vector3.Distance(uc.getPostion(), ServerResult.getPostion());
                found = true;
                if (dist > 0.15)
                {
                 
                    accepted = false;

                    if (!smoothCorrection) //for smooth correction
                    {
                        CSDiffrence = ServerResult.getPostion() - uc.getPostion();
                    }
                }
                break;              
            }
        }

        if (!found)
        {
            Debug.Log("NOT FOUND !!!!");
        }
        else
        {
            Debug.Log(" PredictionToBeAccepted " + PredictionToBeAccepted.Count);
            if (accepted)
            {
                history.Add(ServerResult); 
                PredictionToBeAccepted.RemoveAll(x => x.timestamp == ServerResult.timestamp);
               
                if (smoothCorrection)
                {
                    
                    smoothCorrection = false;
                }
            }
            else
            {
              
                //smooth interpolate to it
                if (!smoothCorrection)
                {
                    CurrentCorrectionTime = 0;
                    AppliedCorrection = Vector3.zero;
                    Debug.Log(" SNAP diffrence is "+ CSDiffrence + " wrong predictions "+ PredictionToBeAccepted.Count);
                    smoothCorrection = true;
                }
            }
        }
    }

    void SimulateEntity()
    {


        if (ReciveBuffor != null && ReciveBuffor.Count > 0)
        {
            if (pastCommand == null || pastCommand.timestamp == 0) //first iteration
            {
                pastCommand = ReciveBuffor.Dequeue();
                ExecuteCommand(pastCommand);
                history.Add(pastCommand);
            }

            if (targetInterpolationCommand == null || targetInterpolationCommand.timestamp == 0) //second iteration
            {
                if (!interpolating)
                {
                    targetInterpolationCommand = ReciveBuffor.Dequeue();
                    InterpolationTimeGap = (targetInterpolationCommand.timestamp - pastCommand.timestamp);

                    timepassed = Time.deltaTime;
                    interpolating = true;
                    interpolationDistance = Vector3.Distance(pastCommand.getPostion(), targetInterpolationCommand.getPostion());
                }
            }

            if(targetInterpolationCommand.shooting)
            {
                Shooting();
            }

            if (interpolating)
            {

                Debug.Log(" interpolation between time " + InterpolationTimeGap + " timepassed " + timepassed + " LerpPercent " + (timepassed / InterpolationTimeGap) + " and postion delta " + interpolationDistance + " buffer size is " + ReciveBuffor.Count);

                if(interpolationDistance < 0.01)
                {
                    animat.SetInteger("condition", 0);
                }
                else
                {
                    animat.SetInteger("condition", 1);
                }

                if (interpolationDistance > 0.2f && InterpolationTimeGap != 0)
                {
                    if (timepassed > InterpolationTimeGap)
                    {
                        transform.position = targetInterpolationCommand.getPostion();
                        transform.rotation = Quaternion.Euler(new Vector3(0, targetInterpolationCommand.rotationAngle, 0));
                        interpolating = false;
                        history.Add(new Usercmd(Time.deltaTime, pastCommand.timestamp, targetInterpolationCommand.forwardmove, targetInterpolationCommand.sidemove, transform.rotation.eulerAngles.y, transform.position.x, transform.position.y, targetInterpolationCommand.shooting));
                        pastCommand = targetInterpolationCommand;
                        targetInterpolationCommand = new Usercmd();
                    }
                    else
                    {
                        double LerpPercent = timepassed / InterpolationTimeGap;
                        transform.position = Vector3.Lerp(pastCommand.getPostion(), targetInterpolationCommand.getPostion(), (float)LerpPercent);
                        Vector3 rotation = Vector3.Lerp(new Vector3(0, pastCommand.rotationAngle, 0), new Vector3(0, targetInterpolationCommand.rotationAngle, 0), (float)LerpPercent);
                        history.Add(new Usercmd(Time.deltaTime, (pastCommand.timestamp + timepassed), targetInterpolationCommand.forwardmove, targetInterpolationCommand.sidemove, transform.rotation.eulerAngles.y, transform.position.x, transform.position.y, targetInterpolationCommand.shooting));
                        timepassed += Time.deltaTime;
                    }
                }
                else //no need for interpolating in unity
                {
                    Debug.Log("fast interpolation ");

                    transform.position = targetInterpolationCommand.getPostion();
                    transform.rotation = Quaternion.Euler(new Vector3(0, targetInterpolationCommand.rotationAngle, 0));
                    interpolating = false;

                    history.Add(new Usercmd(Time.deltaTime, pastCommand.timestamp, targetInterpolationCommand.forwardmove, targetInterpolationCommand.sidemove, transform.rotation.eulerAngles.y, transform.position.x, transform.position.y, targetInterpolationCommand.shooting));
                    pastCommand = targetInterpolationCommand;
                    targetInterpolationCommand = new Usercmd();
                }
            }


        }
        else
        {
            //extrapolation
            Debug.Log(" extrapolation ");
            if (history != null && history.Count > 0)
            {
                Usercmd lastConfirmedCommand = history[history.Count - 1];

                //Debug.Log(" extrapolation Time Delta :" + (PhotonNetwork.Time - lastConfirmedCommand.timestamp));
                Vector3 predictedPostion = PredictPostion(new Vector3(lastConfirmedCommand.forwardmove, 0, lastConfirmedCommand.sidemove));

                Usercmd predictCommand = new Usercmd(Time.deltaTime, (lastConfirmedCommand.timestamp + Time.deltaTime), lastConfirmedCommand.forwardmove, lastConfirmedCommand.sidemove, lastConfirmedCommand.rotationAngle, predictedPostion.x, predictedPostion.z);
                //not suer about time
                ReciveBuffor.Enqueue(predictCommand);
            }
        }
        
       
    }

    private void SmoothCorrection()
    {
        if (smoothCorrection)
        {
            CurrentCorrectionTime += Time.deltaTime;

            float lerpPercent = CurrentCorrectionTime / MaxCorrectionTime;
            if (lerpPercent <= 1.1)
            {
                Vector3 FinalCorrection = Vector3.zero;
                Vector3 correctionPos = Vector3.Lerp(Vector3.zero, CSDiffrence, lerpPercent);
                FinalCorrection =  correctionPos- AppliedCorrection;
               

                Debug.Log("Correcting percent " + lerpPercent + " CurrentCorrectionTime " + CurrentCorrectionTime + " CSDiffrence " + CSDiffrence + " correctionPos " + correctionPos + " FinalCorrection "+ (correctionPos - AppliedCorrection) + " AppliedCorrection "+ AppliedCorrection);
                AppliedCorrection = correctionPos;
                transform.position = transform.position + FinalCorrection;
                //need to add correction into it
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
