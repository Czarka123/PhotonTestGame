using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    public delegate void RecreatePostion(double timestamp);
    public static event RecreatePostion RecreatePostionListner;

    public delegate void ReturnToOrginalPostion();
    public static event ReturnToOrginalPostion ReturnToOrginalPostionListner;

    float MAXspeed = 4;
    public float CurrentSpeed = 4;

    private double maxTimeDiscrepancy = 0.3;
    private float maxPosDiff = 0.25f;
    private float MaxSmoothNetUpdateDist = 0.1f;

    CharacterController controller;
    Animator animat;
    PhotonView photonView;
    public Transform rayOrgin;
    [SerializeField]
    private Camera playerCamera;
    [SerializeField]
    private AudioListener playerAudio;

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
    private Vector3 offsetPostion = Vector3.zero;
    private Vector3 targetoffsetPostion = Vector3.zero;
    private Vector3 SavedPostion = Vector3.zero;
    private Quaternion orginaloffsetRotation;
    private Quaternion offsetRotation;
    private Quaternion targetoffsetRotation;

    double StartTime;
    double CurrentStartPhotonTime;
    double InterpolationFrameStartTime;
    double CurrentReplayTime;
    double interpoationTargetTime;

    double CorrectionTime=0;
    double CorrectionBound = 0.5;


    float MyLocalTime;
    int rotationCounter = 0;

    int testcounter = 0;

    int sended_counter =0;
    private int repeatCounter = 0;

    bool shooting = false;
    bool standing = false; //for animation
    bool bUpdatePosition = false;
    bool interpolating = false;

    bool bNetworkSmoothingComplete = true;

    public bool InterpolationActive = true;

    //Queue<SavedMove> savedMoves;
    List<SavedMove> PendingMoveList;
    Queue<SavedMove> InterpolationBuffer;


    SavedMove ServerLastMove;
    
    SavedMove ServerLastMoveInterpolation;

    SavedMove ReplicatedMovement;

    ClientAdjustment latestAdjustment;

    StringBuilder logfile;
    int SaveCounter = 0;

    void SaveCurrentStateToFile()
    {

        var newLine = string.Format("{0}:{1}:{2}:{3}:{4}", SaveCounter, transform.position.x, transform.position.z, Mathf.Ceil(transform.rotation.eulerAngles.y), PhotonNetwork.Time);
        logfile.AppendLine(newLine);
        //toSend.Enqueue(newCmd);
    }

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
        CurrentStartPhotonTime = 0; // PhotonNetwork.Time-0.5f;
        MyLocalTime = Time.time;
        //savedMoves = new Queue<SavedMove>();
        PendingMoveList = new List<SavedMove>();
        InterpolationBuffer = new Queue<SavedMove>();
        ServerLastMove = new SavedMove();
        ReplicatedMovement = new SavedMove();
        latestAdjustment = new ClientAdjustment();

        logfile = new StringBuilder();
        var newLine = string.Format("{0}:{1}:{2}:{3}:{4}", "Index", "PostionX", "PostionZ", "RotationAngle", "PhotonTime");
        logfile.AppendLine(newLine);

        PhotonNetwork.SendRate = 80;
        PhotonNetwork.SerializationRate = 80;

        if (botColor == BotColor.Target)
        {
            RecreatePostionListner += recreatePostion;
            ReturnToOrginalPostionListner += returnToOrginalPosition;
        }
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
     
        Vector3 MoveDirection = savedMove.getDirection();

        Move(MoveDirection, deltaTime);

        transform.rotation = Quaternion.Euler(savedMove.getRotationAngle());

        if (savedMove.shooting)
        {
            Shooting();
        }
    }

    void SimulateMovment()
    {

        if(!bNetworkSmoothingComplete)
        {
            SmoothClientPosition_Interpolate(ENetworkSmoothingMode.Linear);
            SmoothClientPosition_UpdateVisuals(ENetworkSmoothingMode.Linear);

        }
        else
        {
            if(ServerLastMove.timestamp !=0 || CurrentStartPhotonTime != ServerLastMove.timestamp)
            {
                CurrentStartPhotonTime = ServerLastMove.timestamp;
                CurrentStartPhotonTime -= Time.deltaTime; // for now additional latency
                InterpolationFrameStartTime = CurrentStartPhotonTime;
                float distance = Vector3.Distance(transform.position, ServerLastMove.getPostion());
                if (distance>0.01 ) // we start interpolating
                {
                    bNetworkSmoothingComplete = false;
                    orginaloffsetPostion = transform.position;
                  //  targetoffsetPostion = ServerLastMove.getPostion();

                    orginaloffsetRotation = transform.rotation;
                    targetoffsetRotation = Quaternion.Euler(ServerLastMove.getRotationAngle());
                    interpoationTargetTime = ServerLastMove.timestamp;
                }
            }
        }

        if (ServerLastMove.shooting)
        {
            Shooting();
        }

        if (ServerLastMove.stand)
        {
            Stand();
        }

    }

   

    private void Move(Vector3 MoveDirection, float deltaTime)
    {
        if(botColor==BotColor.Green || botColor == BotColor.Target)
        {
            animat.SetInteger("condition", 1);
        }
       
        BotState = BotState.Moving;
        moveDir = MoveDirection;
        Vector3 mD = MoveDirection;
        mD *= CurrentSpeed;
        mD = transform.TransformDirection(mD);

        controller.Move(mD * deltaTime);
     
    }


    private void SmoothClientPosition_Interpolate(ENetworkSmoothingMode smoothingMode)
    {
        if (smoothingMode == ENetworkSmoothingMode.Linear)
        {
            float LerpPercent = 0f;
            const float LerpLimit = 1.15f;
            CurrentStartPhotonTime += Time.deltaTime;
            double LastCorrectionDelta = ServerLastMove.timestamp - InterpolationFrameStartTime;

            //if time big enogh
            double RemainingTime = ServerLastMove.timestamp - CurrentStartPhotonTime;
            double CurrentSmoothTime = LastCorrectionDelta - RemainingTime;

            LerpPercent =(float) (CurrentSmoothTime / LastCorrectionDelta);

            //LerpPercent = Mathf.Clamp((float)toClamp, 0.0f, LerpLimit);

            if (LerpPercent >= 0.98f)
            {

                bNetworkSmoothingComplete = true;
                CurrentStartPhotonTime = ServerLastMove.timestamp;
                if(LerpPercent < LerpLimit)
                {
                    offsetPostion = Vector3.LerpUnclamped(orginaloffsetPostion, ServerLastMove.getPostion(), LerpPercent);
                }
                else
                {
                    offsetPostion = ServerLastMove.getPostion();
                }
                offsetRotation = targetoffsetRotation;
            }
            else
            {
                offsetPostion = Vector3.Lerp (orginaloffsetPostion, ServerLastMove.getPostion() , LerpPercent);
                offsetRotation = Quaternion.Slerp(orginaloffsetRotation, targetoffsetRotation, LerpPercent);

            }

            if(Vector3.Distance(transform.position, offsetPostion)>0.3)
            {
                Vector3 correctedOffset = Vector3.ClampMagnitude((offsetPostion- transform.position), 0.15f);
                Debug.Log(" OFFSET TO BIG " + (offsetPostion - transform.position) + " CORRECTING " + correctedOffset);
                offsetPostion = transform.position + correctedOffset;
              
            }
        }
    }

    private void SmoothClientPosition_UpdateVisuals(ENetworkSmoothingMode smoothingMode)
    {
        if (smoothingMode == ENetworkSmoothingMode.Linear)
        {
            BotState = BotState.Moving;
            animat.SetInteger("condition", 1);
            transform.position = offsetPostion;
            transform.rotation = offsetRotation;
        }
    }


    private void SmoothClientPosition_Interpolate(SavedMove ClientData, SavedMove ClientDataNext, ENetworkSmoothingMode smoothingMode)
    {

        //if (ClientDataNext.shooting)
        //{
        //    Shooting();
        //}

        //if (ClientDataNext.stand)
        //{
        //    BotState = BotState.Standing;
        //    animat.SetInteger("condition", 0);
        //    double RemainingTime = ClientDataNext.timestamp - ClientData.timestamp;
        //    double LastCorrectionDelta = ClientDataNext.timestamp - InterpolationFrameStartTime;
        //    double LerpPercent = RemainingTime / LastCorrectionDelta;
        //    transform.position = Vector3.Lerp(ClientData.getPostion(), ClientDataNext.getPostion(), (float)LerpPercent);
        //    //is this good ?
        //    // Debug.Log(botColor + " interpolating to" + ClientDataNext.timestamp + " desired postion " + ClientDataNext.getPostion() + " shooting " + ClientDataNext.shooting + " standing " + ClientDataNext.stand);
        //    interpolating = false;

        //}

        //else

        if (smoothingMode == ENetworkSmoothingMode.Linear)
        {

            //   Debug.Log(botColor + " interpolating to" + ClientDataNext.timestamp + " desired postion " + ClientDataNext.getPostion() + " shooting " + ClientDataNext.shooting + " standing " + ClientDataNext.stand);

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
                if (!ClientDataNext.shooting)
                {
                    animat.SetInteger("condition", 1);
                }

                Vector3 offset = Vector3.Lerp(Vector3.zero, orginaloffsetPostion, LerpPercent);

                Vector3 CurrentDelta = ClientData.getPostion() - ClientDataNext.getPostion();

                Vector3 finalPostion = transform.position + offset;


                //Debug.Log(" orginaloffset " + orginaloffsetPostion + "percent " + LerpPercent + " offset " + offset + " postion from " + ClientData.getPostion() + " to " + ClientDataNext.getPostion() + " delta beetween " + CurrentDelta + " postion after adding offset" + finalPostion);



                Quaternion rotationOffset = Quaternion.Lerp(orginaloffsetRotation, Quaternion.Euler(ClientDataNext.getRotationAngle()), LerpPercent);


                transform.rotation = rotationOffset;
                //transform.position = finalPostion;
                Move(offset, Time.deltaTime);


            }



        }
        else if (smoothingMode == ENetworkSmoothingMode.Exponential)
        {


        }
        else if (smoothingMode == ENetworkSmoothingMode.Replay)
        {
            float StartTime = Time.time;
            if (CurrentReplayTime >= ClientData.timestamp && CurrentReplayTime <= ClientDataNext.timestamp)
            {

                const float EPSILON = 0.01f;
                double Delta = ClientDataNext.timestamp - ClientData.timestamp;
                float LerpPercent;

                if (Delta > EPSILON)
                {
                    double toClamp = (CurrentReplayTime - ClientData.timestamp) / Delta;
                    LerpPercent = Mathf.Clamp((float)toClamp, 0.0f, 1.0f);
                }
                else
                {
                    LerpPercent = 1.0f;
                }

                Vector3 Location = Vector3.Lerp(ClientData.getPostion(), ClientDataNext.getPostion(), LerpPercent);
                Quaternion rotation = Quaternion.Lerp(Quaternion.Euler(ClientData.getRotationAngle()), Quaternion.Euler(ClientDataNext.getRotationAngle()), LerpPercent); //normailzed ?

                //Debug.Log("REPLAY Time on  " + botColor + CurrentReplayTime + " location from  " + ClientData.getPostion() + "loc to" + ClientDataNext.getPostion() + " lerp " + Location + " my postion " + transform.position + " rotation " + rotation);

                transform.position = Location;
                transform.rotation = rotation;
                //PerformeMovment(ClientData, Time.deltaTime);
            }
            else
            {
                Debug.Log("REPLAY TIME NOT CAUGHT Q!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            }
            float DeltaTime = Time.time - StartTime;
            CurrentReplayTime += DeltaTime;
        }
        else
        {

        }


    }

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
        yield return null;
    }

    void RotateAngle (float Angle)
    {
        transform.Rotate(0, Angle, 0);
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
       
        FireFlash.Play();
        StartCoroutine(LaserAnimation(0.2f));
        BotState = BotState.Shooting;
        //shooting = true;
       
        animat.SetInteger("condition", 0);
        shooting = true;
    }

    private void recreatePostion(double timestamp)
    {
        if (botColor == BotColor.Target)
        {
            Debug.Log("Recreating postion !!!!"); //maybe possible to even recreate orginal postion on player taking lag and interpolation into consideration

            foreach (SavedMove history in InterpolationBuffer)
            {
                double deltaTime = (history.timestamp - timestamp);
                if (deltaTime > -0.015 && deltaTime < 0.015)
                {
                    Debug.Log("Fround time " + history.timestamp + " orginal " + timestamp + " dt " + deltaTime + " postion " + history.getPostion());
                    SavedPostion = transform.position;
                    transform.position = history.getPostion();
                    break;
                }
            }
        }
    }

    private void returnToOrginalPosition()
    {
        if (botColor == BotColor.Target)
        {
            if (SavedPostion != Vector3.zero)
            {
                Debug.Log("Returning to orginal postion !!!!");
                transform.position = SavedPostion;
            }
        }
    }

    private void RecreateShoot(SavedMove savedMove)
    {
        if (savedMove.shooting)
        {
            RecreatePostionListner.Invoke(savedMove.timestamp);

            RaycastHit hit;
            Debug.Log("shooting");
            if (Physics.Raycast(rayOrgin.position, rayOrgin.TransformDirection(Vector3.forward), out hit, 700))
            {
                Debug.DrawRay(rayOrgin.position, rayOrgin.TransformDirection(Vector3.forward) * hit.distance * 700, Color.red);
                Debug.Log("hit");
                if (hit.transform.tag == "Target")
                {
                    Debug.Log("target hit at " + hit.transform.position);
                }
            }
            else
            {
                Debug.DrawRay(rayOrgin.position, rayOrgin.TransformDirection(Vector3.forward) * hit.distance * 700, Color.white);
                Debug.Log("no hit");
            }
        }

        ReturnToOrginalPostionListner.Invoke();
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


    private void MovementTestSequnce()
    {
        shooting = false; 
        standing = false;

        if (MyLocalTime + 3 > Time.time)
        {
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 4 > Time.time && MyLocalTime + 3 < Time.time)
        {

            if (rotationCounter == 0)
            {
                rotationCounter++;
                RotateAngle(-90);
            }
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);

        }
        else if (MyLocalTime + 5 > Time.time && MyLocalTime + 4 < Time.time)
        {

            Move(new Vector3(0, 0, -1.0f), Time.deltaTime);

        }
        else if (MyLocalTime + 6 > Time.time && MyLocalTime + 5 < Time.time)
        {

            Move(new Vector3(1f, 0, 0), Time.deltaTime);

        }
        else if (MyLocalTime + 7 > Time.time && MyLocalTime + 6 < Time.time)
        {

            if (rotationCounter == 1)
            {
                rotationCounter++;
                RotateAngle(135);
            }
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 8 > Time.time && MyLocalTime + 7 < Time.time)
        {
            if (rotationCounter == 2)
            {
                rotationCounter++;
                RotateAngle(90);
            }
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 9 > Time.time && MyLocalTime + 8 < Time.time)
        {
            if (rotationCounter == 3)
            {
                rotationCounter++;
                RotateAngle(90);
            }
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 10 > Time.time && MyLocalTime + 9 < Time.time)
        {
            if (rotationCounter == 4)
            {
                rotationCounter++;
                RotateAngle(90);
            }
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 11 > Time.time && MyLocalTime + 10 < Time.time)
        {
            if (rotationCounter == 5)
            {
                rotationCounter++;
                RotateAngle(135);
            }
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 12 > Time.time && MyLocalTime + 11 < Time.time)
        {
            Stand();
            Shooting();

        }
        else
        {
            Stand();
        }
    }

    void MovmentScript()
    {
        shooting = false; //hmm
        standing = false;

        if (MyLocalTime + 0.5f > Time.time)
        {
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }

        else if (MyLocalTime + 4 > Time.time && MyLocalTime + 0.5f < Time.time)
        {
            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 5 > Time.time && MyLocalTime + 4 < Time.time)
        {
            if (rotationCounter == 0)
            {
                rotationCounter++;
                RotateAngle(90);
                Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
                Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
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
                StartCoroutine(Rotate(180, Time.deltaTime));
            }

            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 8 > Time.time && MyLocalTime + 7 < Time.time)
        {

            Move(new Vector3(1.0f, 0, 0), Time.deltaTime);
        }
        else if (MyLocalTime + 12 > Time.time && MyLocalTime + 8 < Time.time)
        {
            if (rotationCounter == 2)
            {
                rotationCounter++;
                StartCoroutine(Rotate(90, Time.deltaTime));
            }

            Move(new Vector3(0, 0, 1.0f), Time.deltaTime);
        }
        else if (MyLocalTime + 13 > Time.time && MyLocalTime + 12 < Time.time)
        {
            if (rotationCounter == 3)
            {
                rotationCounter++;
                StartCoroutine(Rotate(90, Time.deltaTime));
            }
            Stand();
        }
        else if (MyLocalTime + 16 > Time.time && MyLocalTime + 13 < Time.time)
        {
            Stand();
            Shooting();
        }
        else if (MyLocalTime + 16.5 > Time.time && MyLocalTime + 16 < Time.time)
        {
            Move(new Vector3(1.0f, 0, 0), Time.deltaTime);
        }
        else
        {
            Stand();
        }
    }
    // Update is called once per frame

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

    void FixedUpdate()
    {
        SaveCounter++;
        //Debug.Log("me is " + botColor + " mine " + photonView.IsMine);
        if (photonView.IsMine && botColor == BotColor.Green)
        {
            if(bUpdatePosition)
            {
                ClientUpdatePosition();
            }

            Vector3 OldPos = transform.position;
            float OldRot = transform.rotation.eulerAngles.y;

            MovementTestSequnce();//performe movement 
            SaveCurrentStateToFile();
            ReplicateMoveToServer(OldPos, OldRot);
            //serverMove
            byte evCode = 1;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendOptions sendOptions = new SendOptions { Reliability = true };
            PhotonNetwork.RaiseEvent(evCode, PendingMoveList[PendingMoveList.Count - 1], raiseEventOptions, sendOptions);
        }
        else if (photonView.IsMine && botColor == BotColor.Red)
        {

        }
        else if (photonView.IsMine && botColor == BotColor.Target)
        {
            Vector3 OldPos = transform.position;
            float OldRot = transform.rotation.eulerAngles.y;
            TargetMovmentScript();
            ReplicateMoveToServer(OldPos, OldRot);
            ServerLastMove = PendingMoveList[0];
            InterpolationBuffer.Enqueue(ServerLastMove); //works as history here
            PendingMoveList.RemoveAt(0);
            if(InterpolationBuffer.Count>100)//cleaning
            {
                InterpolationBuffer.Dequeue();
            }
        }
        else if (!photonView.IsMine && botColor == BotColor.Red)
        {

            if (InterpolationActive)
            {
                SimulateMovment();
            }
            else
            {
                PerformeMovment(ServerLastMove, Time.deltaTime);
            }
            SaveCurrentStateToFile();
        }

        else if (!photonView.IsMine && botColor == BotColor.Target)
        {

            if (InterpolationActive)
            {
                SimulateMovment();
            }
            else
            {
                PerformeMovment(ServerLastMove, Time.deltaTime);
            }
        }

        if (SaveCounter == 750)
        {
            string filename = "";
            if (photonView.IsMine && botColor == BotColor.Green)
            {
                filename = "UnrealGreen";
            }
            else if (photonView.IsMine && botColor == BotColor.Red)
            {
                filename = "UnrealRedServer";
            }
            else if (!photonView.IsMine && botColor == BotColor.Red)
            {
                filename = "UnrealRedProxy";
            }

            File.WriteAllText(@"/path" + filename + ".csv", logfile.ToString());

            Debug.Log(" Log Saved in file ");
        }
    }

    void ServerMove(SavedMove savedMove)
    {
        if (botColor == BotColor.Red && photonView.IsMine)
        {

            Vector3 serverStartPos = transform.position;
            float startRotation = transform.rotation.eulerAngles.y;
            if (serverStartPos != savedMove.getStartPostion())
            {
                if(serverStartPos == Vector3.zero)
                {
                    serverStartPos = savedMove.getStartPostion();
                    transform.position = savedMove.getStartPostion(); 
                }
            }

            PerformeMovment(savedMove, Time.deltaTime);
           

            float distance = Mathf.Abs(Vector3.Distance(transform.position, savedMove.getPostion()));
            SaveCurrentStateToFile();
            ServerLastMove = new SavedMove(savedMove.timestamp, savedMove.forwardmove, savedMove.sidemove, transform.rotation.eulerAngles.y, startRotation , transform.position, serverStartPos, CurrentSpeed, savedMove.shooting, savedMove.stand);

            if (distance > maxPosDiff)
            {
               
                if ((CorrectionTime + CorrectionBound) < PhotonNetwork.Time)
                {

                    ClientAdjustment adjustment = new ClientAdjustment(false, serverStartPos, transform.position, transform.rotation.y, savedMove.timestamp);
                  
                    photonView.RPC("ClientAdjustPosition", RpcTarget.All, adjustment);
                    byte evCode = 2;
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                    SendOptions sendOptions = new SendOptions { Reliability = true };
                    PhotonNetwork.RaiseEvent(evCode, adjustment, raiseEventOptions, sendOptions);
                    CorrectionTime = PhotonNetwork.Time;
                }

            }
            else
            {

                Debug.Log(" MOVE CORRECT " );
                RecreateShoot(savedMove);
                ClientAdjustment adjustment = new ClientAdjustment();
                adjustment.AckGoodMove = true;
                adjustment.TimeStamp = savedMove.timestamp;
                byte evCode = 2;
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                SendOptions sendOptions = new SendOptions { Reliability = true };
                PhotonNetwork.RaiseEvent(evCode, adjustment, raiseEventOptions, sendOptions);
            }

        }
    }


    void ClientAdjustPosition(ClientAdjustment adjustment)
    {
        if (botColor == BotColor.Green && photonView.IsMine)
        {
            if (adjustment.AckGoodMove)
            {              
                if (PendingMoveList != null)
                {
                    PendingMoveList.RemoveAll(a => a.timestamp <= adjustment.TimeStamp);
                }
            }
            else
            {
               bUpdatePosition = true;

                latestAdjustment = adjustment;
                Debug.Log(" WRONMG move " + adjustment.getStartLoc() + " resulted in postion " + adjustment.getNewLoc() + " actual postion   "  + transform.position + "  on " + adjustment.TimeStamp);

            }
        }
    }

    private void ReplicateMoveToServer() //just adding move to list
    {
        SavedMove toSaveMove = new SavedMove(PhotonNetwork.Time, moveDir.z, moveDir.x, 0, transform.rotation.eulerAngles.y, Vector3.zero, transform.position,  CurrentSpeed, shooting, standing);

        if (PendingMoveList != null)
        {
            PendingMoveList.Add(toSaveMove);
        }

    }

    private void ReplicateMoveToServer(Vector3 startPostion, float startRotation) //just adding move to list
    {
        SavedMove toSaveMove = new SavedMove(PhotonNetwork.Time, moveDir.z, moveDir.x, transform.rotation.eulerAngles.y, startRotation, transform.position, startPostion, CurrentSpeed, shooting, standing);

        if (PendingMoveList != null)
        {
            PendingMoveList.Add(toSaveMove);
        }

    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (botColor == BotColor.Red || botColor == BotColor.Target)
            {
                if (testcounter % 2 == 0)
                {
                    Debug.Log("sending shoots" + ServerLastMove.shooting);
                    stream.SendNext(ServerLastMove);
                    testcounter = 0;
                }
                testcounter++;
            }
        }
        else
        {
            SavedMove server_lastMove = (SavedMove)stream.ReceiveNext();

            ServerLastMove = server_lastMove;
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == 1)
        {
            if (botColor == BotColor.Red   && photonView.IsMine)
            {
                SavedMove recivedMove = (SavedMove)photonEvent.CustomData;
                ServerMove(recivedMove);
            }
        }

        if (photonEvent.Code == 2)
        {
            if (botColor == BotColor.Green  && photonView.IsMine)
            {
                ClientAdjustment clientAdjustment  = (ClientAdjustment)photonEvent.CustomData;
                ClientAdjustPosition(clientAdjustment);
            }
        }

    }



    private void ClientUpdatePosition()
    {
        if (PendingMoveList.Count > 0)
        {
           // CurrentReplayTime = PendingMoveList[0].timestamp;

            for (int i = 0; i < PendingMoveList.Count; i++)
            {
               // Debug.Log("Coreecting " + (i+1) + "/" + PendingMoveList.Count + " adustement timestamp " + latestAdjustment.TimeStamp + " | " + PendingMoveList[i].timestamp);
                if(latestAdjustment.TimeStamp > PendingMoveList[i].timestamp)
                {

                }
                else if (latestAdjustment.TimeStamp == PendingMoveList[i].timestamp)
                {

                    controller.enabled = false;
                    controller.transform.position = latestAdjustment.getNewLoc();
                    controller.enabled = true;

                    transform.position = latestAdjustment.getNewLoc();
                    transform.rotation = Quaternion.Euler(new Vector3(0, latestAdjustment.NewRot, 0));

                    Debug.Log("This is Start " + transform.position);
                }
                else if (latestAdjustment.TimeStamp < PendingMoveList[i].timestamp)
                {
                    PendingMoveList[i].setStartPostion(transform.position);
                    PendingMoveList[i].startRotationAngle = transform.rotation.eulerAngles.y;

                    PerformeMovment(PendingMoveList[i], Time.deltaTime);
                    
                    PendingMoveList[i].setPostion(transform.position);
                    PendingMoveList[i].rotationAngle = transform.rotation.eulerAngles.y;

                    Debug.Log("Moves from " + PendingMoveList[i].getStartPostion() + " to " +PendingMoveList[i].getPostion() + " direction " + PendingMoveList[i].getDirection());

                    //smoothing
                }            
            }
        }
        bUpdatePosition = false;
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

                if (Vector3.Distance(toSendMove.getPostion(), sm.getPostion()) < 0.05 && toSendMove.getDirection() == sm.getDirection() && !sm.shooting)
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
