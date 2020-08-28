using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ENetworkSmoothingMode
{
    Disabled,
    Linear,
    Exponential,
    Replay
}

public class Unreal : MonoBehaviour, IPunObservable, IOnEventCallback
{
    float MAXspeed = 4;
    public float CurrentSpeed = 4;

    private double maxTimeDiscrepancy = 0.3;
    private float maxPosDiff = 0.25f;

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

    [SerializeField]
    private BotColor botColor;
    [SerializeField]
    private ParticleSystem FireFlash;
    [SerializeField]
    private GameObject LaserEffect;

    private GameObject LaserObject;


    public BotState BotState;
    private Vector3 moveDir = Vector3.zero; //treating as input for moving 
    private Vector3 orginaloffsetPostion = Vector3.zero;
    private Quaternion orginaloffsetRotation;

    double StartTime;
    double CurrentStartPhotonTime;
    double InterpolationFrameStartTime;
    float MyLocalTime;
    int rotationCounter = 0;

    int sended_counter =0;

    bool shooting = false;
    bool standing = false; //for animation
    bool bUpdatePosition = false;
    bool interpolating = false;

    //Queue<SavedMove> savedMoves;
    List<SavedMove> PendingMoveList;
    Queue<SavedMove> InterpolationBuffer;
    SavedMove ServerLastMove;
    SavedMove ServerLastMoveInterpolation;

    SavedMove ReplicatedMovement;


    // Start is called before the first frame update
    void Start()
    {
        PhotonPeer.RegisterType(typeof(SavedMove), 2, SavedMove.Serialize, SavedMove.Deserialize);
        PhotonPeer.RegisterType(typeof(ClientAdjustment), 3, ClientAdjustment.Serialize, ClientAdjustment.Deserialize);

        controller = GetComponent<CharacterController>();
        animat = GetComponent<Animator>();
        photonView = GetComponent<PhotonView>();
        Destroy(playerCamera);
        Destroy(playerAudio);

        LaserObject = Instantiate(LaserEffect, rayOrgin);
        LaserObject.SetActive(false);

        controller.detectCollisions = false;

        StartTime = PhotonNetwork.Time;
        CurrentStartPhotonTime = PhotonNetwork.Time-0.5f;
        MyLocalTime = Time.time;
        //savedMoves = new Queue<SavedMove>();
        PendingMoveList = new List<SavedMove>();
        InterpolationBuffer = new Queue<SavedMove>();
        ServerLastMove = new SavedMove();
        ReplicatedMovement = new SavedMove();

        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 60;
    }

    private void PerformeMovment(SavedMove savedMove, float deltaTime)
    {
        if (savedMove.stand)
        {
            BotState = BotState.Standing;
            animat.SetInteger("condition", 0);
        }
        else
        {
            BotState = BotState.Moving;
            animat.SetInteger("condition", 1);
        }
        //Debug.Log("playimg move " + savedMove.timestamp +" coclor "+ botColor +"  dire " + savedMove.getDirection() + " rotation " + transform.rotation.eulerAngles.y + " recived rot is " + savedMove.rotationAngle);
        Vector3 MoveDirection = new Vector3(savedMove.sidemove, 0, savedMove.forwardmove);


        Move(MoveDirection, Time.deltaTime);


        //moveDir = savedMove.getDirection();

        if (transform.rotation.eulerAngles.y != savedMove.rotationAngle)
        {
            //transform.rotation = Quaternion.Euler(new Vector3(0, savedMove.rotationAngle, 0)); //to redo 
            //StartCoroutine(Rotate(savedMove.rotationAngle, deltaTime));
            transform.rotation = Quaternion.Euler(savedMove.getRotationAngle());
            // RotateAngle(savedMove.rotationAngle);
        }

        if (savedMove.shooting)
        {
            Shooting();
        }

    }

    void SimulateMovment()
    {
        if (botColor == BotColor.Red && !photonView.IsMine)
        {
            SmoothClientPosition();
        }
    }

    private void Move(Vector3 MoveDirection, float deltaTime)
    {
        if(botColor==BotColor.Green)
        {
            animat.SetInteger("condition", 1);
        }
       
        BotState = BotState.Moving;
        moveDir = MoveDirection;
        Vector3 mD = MoveDirection;
        mD *= CurrentSpeed;
        mD = transform.TransformDirection(mD);

        //mD.y -= gravity * detaTime;
        //Debug.Log("MOVEEE " + botColor + " :" + mD);
        controller.Move(mD * deltaTime);

        // currentPostion = transform.position;       
    }

    void SmoothClientPosition()
    {
        if (InterpolationBuffer != null && InterpolationBuffer.Count > 0 && !interpolating)
        {
            Debug.Log("interpolation");
            interpolating = true;

            ReplicatedMovement = InterpolationBuffer.Dequeue();
            orginaloffsetPostion = ReplicatedMovement.getPostion()- transform.position;
            orginaloffsetRotation = transform.rotation;
            InterpolationFrameStartTime = CurrentStartPhotonTime;
        }

        if(InterpolationBuffer.Count==0)
        {
            Debug.Log("EMPTYYYYYYYYYYYY");

        }

        if (interpolating)
        {
            SavedMove currentState = new SavedMove();
            currentState.timestamp = CurrentStartPhotonTime;
            currentState.setPostion(transform.position);
            SmoothClientPosition_Interpolate(currentState, ReplicatedMovement, ENetworkSmoothingMode.Linear);
        }
    }

    private void SmoothClientPosition_Interpolate(SavedMove ClientData, SavedMove ClientDataNext, ENetworkSmoothingMode smoothingMode)
    {
        if (ClientDataNext.stand)
        {
            BotState = BotState.Standing;
            animat.SetInteger("condition", 0);
        }
        else
        {

            if (smoothingMode == ENetworkSmoothingMode.Linear)
            {
                float LerpPercent = 0f;
                const float LerpLimit = 1.15f;
                CurrentStartPhotonTime += Time.deltaTime;
                // if time is right ReplicatedMovement
                double RemainingTime = ClientDataNext.timestamp - ClientData.timestamp;
                double LastCorrectionDelta = ClientDataNext.timestamp - InterpolationFrameStartTime;
                double CurrentSmoothTime = LastCorrectionDelta - RemainingTime;

                //  Debug.Log("TIMES server " + ServerData.timestamp + " client data " + ClientData.timestamp + " CurrentStartPhotonTime " + CurrentStartPhotonTime + " started this shiet on " + InterpolationFrameStartTime);

                double toClamp = CurrentSmoothTime / LastCorrectionDelta;

                LerpPercent = Mathf.Clamp((float)toClamp, 0.0f, LerpLimit);

                // Debug.Log("TIMES serverpack " + ServerData.timestamp + " client data " + ClientData.timestamp + " LastCorrectionDelta "+ LastCorrectionDelta+ " RemainingTime " + RemainingTime + " Percent " + LerpPercent);

                if (LerpPercent >= 1.0f - 0.001f)
                {
                    interpolating = false;
                    CurrentStartPhotonTime = ClientDataNext.timestamp;
                    transform.rotation = Quaternion.Euler(ClientDataNext.getRotationAngle());
                }
                else
                {

                    BotState = BotState.Moving;
                    animat.SetInteger("condition", 1);

                    Vector3 offset = Vector3.Lerp(orginaloffsetPostion, Vector3.zero, LerpPercent);

                    Debug.Log(" orginaloffset " + orginaloffsetPostion + " offset " + offset + " postion from " + ClientData.getPostion() + " to " + ClientDataNext.getPostion());

                    Quaternion rotationOffset = Quaternion.Lerp(orginaloffsetRotation, Quaternion.Euler(ClientDataNext.getRotationAngle()), LerpPercent);


                    transform.rotation = rotationOffset;

                    Move(offset, Time.deltaTime);

                }


            }
            else if (smoothingMode == ENetworkSmoothingMode.Exponential)
            {


            }
            else if (smoothingMode == ENetworkSmoothingMode.Replay)
            {
                //if (CurrentTime >= ClientData.timestamp && CurrentTime <= ClientDataNext.timestamp)
                //{
                //    const float EPSILON = 0.01f;
                //    double Delta = ClientDataNext.timestamp - ClientData.timestamp;
                //    float LerpPercent;

                //    if (Delta> EPSILON)
                //    {
                //        LerpPercent = Mathf.Clamp((CurrentTime - ClientData.timestamp) / Delta, 0.0f, 1.0f);
                //    }
                //    else
                //    {
                //        LerpPercent = 1.0f;
                //    }

                //    Vector3 Location = Vector3.Lerp(ClientData.getPostion(), ClientDataNext.getPostion(), LerpPercent);
                //    Quaternion rotation = Quaternion.Lerp(Quaternion.Euler(ClientData.getRotationAngle()), Quaternion.Euler(ClientDataNext.getRotationAngle()), LerpPercent).normalized; //normailzed ?
                //}

            }
            else
            {

            }

            if (ClientDataNext.shooting)
            {
                Shooting();
            }
        }
    }

    //private void Move(Vector3 MoveDirection, float deltaTime)
    //{
    //    animat.SetInteger("condition", 1);
    //    BotState = BotState.Moving;
    //    moveDir = MoveDirection;

    //    transform.position += (MoveDirection * deltaTime * CurrentSpeed);
    //}


    IEnumerator Rotate(float Angle, float deltaTime)
    {
        float moveSpeed = 2f;
        float correction = 1;
        animat.SetInteger("condition", 1);
        //Debug.Log("Rotate " + transform.rotation.eulerAngles.y + " < " + Angle);
        while (transform.rotation.eulerAngles.y < Angle - correction)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, Angle, 0), moveSpeed * deltaTime);
            //Debug.Log("Lerp " + transform.rotation.eulerAngles.y + " < " + Angle);
            yield return null;
        }
        transform.rotation = Quaternion.Euler(0, Angle, 0);
       // Debug.Log("Exit "); ;
        yield return null;


    }

    void RotateAngle (float Angle)
    {
        transform.Rotate(0, Angle, 0);
        //animat.SetInteger("condition", 1);
        //transform.rotation = Quaternion.RotateTowards(transform.rotation, new Quaternion(0,Angle,0,0), Time.deltaTime*0.1f);

    }

    private void Stand()
    {
        animat.SetInteger("condition", 0);
        BotState = BotState.Standing;
        moveDir = Vector3.zero;
        standing = true;
    }


    private void Shooting()
    {
        RaycastHit hit;
        FireFlash.Play();
        StartCoroutine(LaserAnimation(0.2f));
        BotState = BotState.Shooting;
        //shooting = true;
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
        animat.SetInteger("condition", 0);
        shooting = true;
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


    void MovmentScript()
    {
        shooting = false; //hmm
        standing = false;

        if (MyLocalTime + 3 > Time.time)
        {
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 5 > Time.time && MyLocalTime + 3 < Time.time)
        {
            if (rotationCounter == 0)
            {
                rotationCounter++;
                RotateAngle(90);
                Debug.Log("ROTATING!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                //StartCoroutine(Rotate(90, Time.deltaTime));
            }
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 7 > Time.time && MyLocalTime + 5 < Time.time)
        {
            if (rotationCounter == 1)
            {
                rotationCounter++;
                StartCoroutine(Rotate(90, Time.deltaTime));
            }

            Move(new Vector3(0, 0, -1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 5 > Time.time && MyLocalTime + 3 < Time.time)
        {

            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 7 > Time.time && MyLocalTime + 5 < Time.time)
        {
            if (rotationCounter == 1)
            {
                rotationCounter++;
                StartCoroutine(Rotate(190, Time.deltaTime));
            }

            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 8 > Time.time && MyLocalTime + 7 < Time.time)
        {

            Move(new Vector3(1.0f, 0, 0), Time.deltaTime);
        }
        else if (MyLocalTime + 11 > Time.time && MyLocalTime + 8 < Time.time)
        {
            if (rotationCounter == 2)
            {
                rotationCounter++;
                StartCoroutine(Rotate(0, Time.deltaTime));
            }

            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 12 > Time.time && MyLocalTime + 11 < Time.time)
        {
            if (rotationCounter == 3)
            {
                rotationCounter++;
                StartCoroutine(Rotate(50, Time.deltaTime));
            }
            Stand();
        }
        else if (MyLocalTime + 12.5 > Time.time && MyLocalTime + 12 < Time.time)
        {
            Move(new Vector3(1.0f, 0, 0), Time.deltaTime);
            Shooting();
        }
        else if (MyLocalTime + 15 > Time.time && MyLocalTime + 12.5 < Time.time)
        {
            Stand();
            Shooting();
        }
        else
        {
            Stand();
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine && botColor == BotColor.Green)
        {
            if (bUpdatePosition)
            {
                ClientUpdatePosition();
            }
            else
            {
                ReplicateMoveToServer();
                MovmentScript();//performe movement 
                PendingMoveList[PendingMoveList.Count - 1].setPostion(transform.position);
                PendingMoveList[PendingMoveList.Count - 1].rotationAngle= transform.rotation.eulerAngles.y;
                //photonView.RPC("ServerMove", RpcTarget.All, GetSavedMoveForServer());

                // 
                RemoveSimilarPackets();
               // Debug.Log(" sending standing  " + PendingMoveList[PendingMoveList.Count - 1].stand);
                //serverMove
                byte evCode = 1;
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                SendOptions sendOptions = new SendOptions { Reliability = true };
                PhotonNetwork.RaiseEvent(evCode, PendingMoveList[PendingMoveList.Count - 1], raiseEventOptions, sendOptions);
                //Debug.Log("sended " + PendingMoveList[PendingMoveList.Count - 1].timestamp + " PendingMoveList " + PendingMoveList.Count);
              

            }
        }
        else if (!photonView.IsMine && botColor == BotColor.Red)
        {
            // PerformeMovment(ServerLastMove, Time.deltaTime);
            SimulateMovment();

        }
    }

    //[PunRPC]
    void ServerMove(SavedMove savedMove)
    {
        //Debug.Log(" me Server is " + botColor + " " + photonView.IsMine);
        if (botColor == BotColor.Red && photonView.IsMine)
        {
            Debug.Log(" V0 move on " + savedMove.timestamp + " resulted in rotation " + savedMove.getRotationAngle() + " my rotation " + transform.rotation.eulerAngles + "start postion " + savedMove.getStartPostion());
           // transform.position = savedMove.getStartPostion();
            Debug.Log(" V1 move on " + savedMove.timestamp + " resulted in rotation " + savedMove.getRotationAngle() + " my rotation " + transform.rotation.eulerAngles + "start postion " + savedMove.getStartPostion());
            //
            PerformeMovment(savedMove, Time.deltaTime);
           
            float distance = Mathf.Abs(Vector3.Distance(transform.position, savedMove.getPostion()));

                 //Mathf.Abs(Vector3.Distance(transform.position, savedMove.getPostion()));
            Debug.Log("V2 move on " + savedMove.timestamp + " resulted in rotation " + savedMove.getRotationAngle() + " my rotation " + transform.rotation.eulerAngles + "start postion " + savedMove.getStartPostion() +" distance " + distance);

            ServerLastMove = new SavedMove(savedMove.timestamp, savedMove.forwardmove, savedMove.sidemove, transform.rotation.eulerAngles.y, transform.position, CurrentSpeed, savedMove.shooting, savedMove.stand);

            if (distance > maxPosDiff)
            {
                ClientAdjustment adjustment = new ClientAdjustment(false, transform.position, transform.rotation.y, savedMove.timestamp);
                //photonView.RPC("ClientAdjustPosition", RpcTarget.All, adjustment);
                //ClientAdjustPosition
                byte evCode = 2;
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                SendOptions sendOptions = new SendOptions { Reliability = true };
                PhotonNetwork.RaiseEvent(evCode, adjustment, raiseEventOptions, sendOptions);
            }
            else
            {
                ClientAdjustment adjustment = new ClientAdjustment();
                adjustment.AckGoodMove = true;
                adjustment.TimeStamp = savedMove.timestamp;
                //photonView.RPC("ClientAdjustPosition", RpcTarget.All, adjustment);
                //ClientAdjustPosition
                byte evCode = 2;
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                SendOptions sendOptions = new SendOptions { Reliability = true };
                PhotonNetwork.RaiseEvent(evCode, adjustment, raiseEventOptions, sendOptions);
            }

        }
    }

    //[PunRPC]
    void ClientAdjustPosition(ClientAdjustment adjustment)
    {
      //  Debug.Log(" me client adj is " + botColor + " " + photonView.IsMine);
        if (botColor == BotColor.Green && photonView.IsMine)
        {
            if (adjustment.AckGoodMove)
            {
               
                 //Debug.Log("GOOD move " + PendingMoveList.Count + " "+ PendingMoveList[0].timestamp +  " <=  " + adjustment.TimeStamp);
                if (PendingMoveList != null)
                {
                    PendingMoveList.RemoveAll(a => a.timestamp <= adjustment.TimeStamp);
                }

               
            }
            else
            {
               bUpdatePosition = true;

               // Debug.Log(" WRONMG move " + transform.position + " resulted in postion " + adjustment.getNewLoc() + " on  " + adjustment.TimeStamp);
                transform.position = adjustment.getNewLoc();
                transform.Rotate(new Vector3(0, adjustment.NewRot, 0));

                //Debug.Log(" WRONMG move " + PendingMoveList.Count + " " + PendingMoveList[0].timestamp + " " + adjustment.TimeStamp);

                if (PendingMoveList != null)
                {
                    PendingMoveList.RemoveAll(a => a.timestamp <= adjustment.TimeStamp);
                }
              
            }
        }
    }

    private void ReplicateMoveToServer()
    {
        SavedMove toSaveMove = new SavedMove(PhotonNetwork.Time, moveDir.z, moveDir.x, 0, transform.rotation.eulerAngles.y, Vector3.zero, transform.position,  CurrentSpeed, shooting, standing);

        if (PendingMoveList != null)
        {
            PendingMoveList.Add(toSaveMove);
        }

    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (botColor == BotColor.Red)
            {
                stream.SendNext(ServerLastMove);
                //stream.SendNext(transform.position);
            }
            //if (savedMoves != null && savedMoves.Count>0)
            //{
            //    SavedMove toSendMove = GetSavedMoveForServer();
            //    if (botColor==BotColor.Green)
            //    {  
            //        byte evCode = 1;
            //        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            //        SendOptions sendOptions = new SendOptions { Reliability = true };
            //        PhotonNetwork.RaiseEvent(evCode, toSendMove, raiseEventOptions, sendOptions);
            //    }
            //    else if(botColor == BotColor.Red)
            //    {
            //        stream.SendNext(toSendMove);
            //    }             
            //}
        }
        else
        {
            if (botColor == BotColor.Red && !photonView.IsMine)
            {
                //Vector3 postion = (Vector3)stream.ReceiveNext();
                //transform.position = postion;
                SavedMove server_lastMove = (SavedMove)stream.ReceiveNext();

                if (InterpolationBuffer != null) {
                    if (sended_counter > 0)
                    {
                        if (Vector3.Distance(ServerLastMoveInterpolation.getPostion(), server_lastMove.getPostion()) > 0.5f || Vector3.Distance(ServerLastMoveInterpolation.getRotationAngle(), server_lastMove.getRotationAngle())>10 || server_lastMove.shooting)
                        {
                            Debug.Log("addded " + server_lastMove.timestamp);
                            InterpolationBuffer.Enqueue(server_lastMove);
                            ServerLastMoveInterpolation = server_lastMove;
                        }
                    }
                    else
                    {
                        sended_counter++;
                        InterpolationBuffer.Enqueue(server_lastMove);
                        ServerLastMoveInterpolation = server_lastMove;
                    }
                }

                //ServerLastMove = server_lastMove;            
                //transform.position = server_lastMove.getPostion();
                //transform.Rotate(server_lastMove.getRotationAngle());


              
                //PerformeMovment(server_lastMove,Time.deltaTime);
            }

            //if (botColor == BotColor.Red)
            //{

            //    SavedMove recivepack = (SavedMove)stream.ReceiveNext();

            //    if (savedMoves != null)
            //    {
            //        savedMoves.Enqueue(recivepack);
            //    }
            //}
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == 1)
        {
            if (botColor == BotColor.Red && photonView.IsMine)
            {
                SavedMove recivedMove = (SavedMove)photonEvent.CustomData;
                ServerMove(recivedMove);
            }
        }

        if (photonEvent.Code == 2)
        {
            if (botColor == BotColor.Green && photonView.IsMine)
            {
                ClientAdjustment clientAdjustment  = (ClientAdjustment)photonEvent.CustomData;
                ClientAdjustPosition(clientAdjustment);
            }
        }

        //if (photonEvent.Code == 1)
        //{
        //    if (botColor == BotColor.Red && photonView.IsMine)
        //    {
        //        //Debug.Log("server recived information packet ");

        //        SavedMove recivedMove = (SavedMove)photonEvent.CustomData;

        //        if (recivedMove.shooting)
        //        {
        //            Debug.Log("ShOUUUULD SHOOTTT !!!!!!!!!!!!!");
        //        }
        //        //Debug.Log("recived action from client numba :" + action.sequence_number);
        //        if (ServerLastMove == null || ServerLastMove.timestamp == 0)
        //        {
        //            PerformeMovment(recivedMove, Time.deltaTime);
        //        }
        //        else if ((PhotonNetwork.Time - recivedMove.timestamp) > maxTimeDiscrepancy)
        //        {
        //            // Debug.Log(" current time "+ PhotonNetwork.Time + " last move time "+ ServerLastMove.timestamp+ " recivedMove " + recivedMove.timestamp + " delta beetewin recived packs "+ (recivedMove.timestamp - ServerLastMove.timestamp) + " delta beetwen server time "+ (PhotonNetwork.Time - recivedMove.timestamp)+ " normal delta "+Time.deltaTime);
        //            Debug.Log("SKIPING !!!");
        //        }
        //        else
        //        {
        //            //(float)((recivedMove.timestamp - ServerLastMove.timestamp)+0.002)
        //            //if(recivedMove.getRotationAngle() != transform.rotation.eulerAngles)
        //            //{
        //            //    Debug.Log("Rotation " + recivedMove.getRotationAngle() + "and is " + transform.rotation.eulerAngles);
        //            //}
        //            PerformeMovment(recivedMove, Time.deltaTime);
        //        }

        //        SavedMove PerformedMove = new SavedMove(recivedMove.timestamp, moveDir.z, moveDir.x, transform.rotation.eulerAngles.y, transform.position.x, transform.position.y, transform.position.z, controller.velocity.x, controller.velocity.y, controller.velocity.z, CurrentSpeed, recivedMove.shooting, recivedMove.stand);

        //        if (recivedMove.getPostion() != transform.position && Vector3.Distance(recivedMove.getPostion(), transform.position) > maxPosDiff)
        //        {
        //            Debug.Log(" Diffrence " + Vector3.Distance(recivedMove.getPostion(), transform.position));
        //            byte evCode = 2;
        //            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        //            SendOptions sendOptions = new SendOptions { Reliability = true };
        //            PhotonNetwork.RaiseEvent(evCode, PerformedMove, raiseEventOptions, sendOptions);
        //        }
        //        else
        //        {
        //            byte evCode = 3;
        //            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        //            SendOptions sendOptions = new SendOptions { Reliability = true };
        //            PhotonNetwork.RaiseEvent(evCode, recivedMove.timestamp, raiseEventOptions, sendOptions);
        //        }

        //        savedMoves.Enqueue(PerformedMove); //for now no history for server

        //        ServerLastMove = recivedMove;
        //    }

        //}

        //if (photonEvent.Code == 2) //correction 
        //{
        //    Debug.Log("correction Move");
        //    if (botColor == BotColor.Green && photonView.IsMine)
        //    {
        //        SavedMove recivedMove = (SavedMove)photonEvent.CustomData;
        //        ClientAdjustPosition(recivedMove);

        //    }
        //}


        //if (photonEvent.Code == 3) //ACK
        //{
        //    Debug.Log("ACK");
        //    if (botColor == BotColor.Green && photonView.IsMine)
        //    {
        //        double ackedTimestamp = (double)photonEvent.CustomData;
        //        int confirmed_moves=0;
        //        for (int i=0;i< UnAckedMoves.Count;i++)
        //        {
        //            if(UnAckedMoves[i].timestamp>=ackedTimestamp)
        //            {
        //                confirmed_moves = i;
        //                Debug.Log("remove till " + UnAckedMoves[i].timestamp);
        //                break;
        //            }
        //        }
        //        UnAckedMoves.RemoveRange(0, confirmed_moves);
        //    }
        //}
    }



    private void ClientUpdatePosition()
    {
        List<SavedMove> newPendingMoves = new List<SavedMove>();

        foreach (SavedMove sm in PendingMoveList)
        {
            PerformeMovment(sm, Time.deltaTime);
            SavedMove CorrectedMove = new SavedMove(sm.timestamp, sm.forwardmove, sm.sidemove, transform.rotation.y, transform.position, CurrentSpeed, shooting, standing);
            newPendingMoves.Add(CorrectedMove);
        }
        PendingMoveList.Clear();
        PendingMoveList = newPendingMoves;

        bUpdatePosition = false;
            //for (int i = 0; i < UnAckedMoves.Count; i++)
            //{
            //    PerformeMovment(UnAckedMoves[i], Time.deltaTime);
            //    //SavedMove correctedMove = new SavedMove(UnAckedMoves[i].timestamp, moveDir.z, moveDir.x, transform.rotation.eulerAngles.y, transform.position.x, transform.position.y, transform.position.z, controller.velocity.x, controller.velocity.y, controller.velocity.z, CurrentSpeed, false);
            //    //UnAckedMoves[i] = correctedMove;
            //    //savedMoves.Enqueue(correctedMove);
            //}
            //UnAckedMoves.Clear();
            //bUpdatePosition = false;
    }

    void RemoveSimilarPackets() //we merge same inputs into one
    {
        SavedMove toSendMove = new SavedMove();
        int counter = 0;

        foreach (SavedMove sm in PendingMoveList)
        {
            if (counter == 0)
            {
                toSendMove = sm;
                counter++; //first just scouts for first msg
            }
            else
            {
                //Debug.Log("pos diff " + Vector3.Distance(toSendMove.getPostion() , sm.getPostion()) + " now rot " + sm.getRotationAngle() + "prev rot " + toSendMove.getRotationAngle() + "dirs "+ sm.getDirection() + toSendMove.getDirection() );
                //Debug.Log("now "+ sm.getPostion() + " prev "+ toSendMove.getPostion() + " now rot " + sm.getRotationAngle() + "prev rot " + toSendMove.getRotationAngle() + "dirs "+ sm.getDirection() + toSendMove.getDirection() );
                // Debug.Log("last one " + toSendMove.postionX +" "+ toSendMove.postionY +" "+ toSendMove.postionZ + " now " + sm.postionX +" "+ sm.postionY +" "+ sm.postionZ);
                //if (toSendMove.postionX == sm.postionX && toSendMove.postionY == sm.postionY && toSendMove.postionZ == sm.postionZ)
                //0.05 for now
                //&& !shooting
                // && toSendMove.getRotationAngle() == sm.getRotationAngle()
                if (Vector3.Distance(toSendMove.getPostion(), sm.getPostion()) < 0.01 && toSendMove.getDirection() == sm.getDirection() )
                {
                    Debug.Log("SAME PACKET TO REMOVE");
                    counter++;        //now counts amount of mssg to remove          
                    toSendMove = sm;
                }
                else
                {
                    break; //next is diffrent so we will send first one anywat
                }
            }
        }

        Debug.Log(" PACKET TO REMOVE : "+ (counter-1));
        if (counter > 1)
        {
            PendingMoveList.RemoveRange(0, counter-1);
        }
        //foreach (SavedMove sm in PendingMoveList)
        //{
        //    Debug.Log("move " + sm.timestamp);
        //}


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
