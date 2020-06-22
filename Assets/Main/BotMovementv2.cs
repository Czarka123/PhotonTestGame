using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum BotColor
{
    Green,
    Red,
    Yellow
}

public enum BotState
{
    Moving,
    Standing,
    Rotating,
    Shooting
}

public class BotMovementv2 : MonoBehaviour, IPunObservable
{
    float speed = 4;
    float gravity = 9;
    float rtt = 0;

    public delegate void SendMyTimeDelegate(string time, BotColor color);
    public static event SendMyTimeDelegate SendMyTimeListners;

    CharacterController controller;
    Animator animat;
    PhotonView photonView;
    public Transform rayOrgin;
    [SerializeField]
    private Camera playerCamera;
    [SerializeField]
    private AudioListener playerAudio;
    private double StartTime;
    private int rotationCounter = 0;

    private string otherBotTime;

    System.IO.StreamWriter logfile;
    System.IO.StreamWriter botlogfile;

    private Vector3 moveDir = Vector3.zero;
    [SerializeField]
    private BotColor botColor;
    [SerializeField]
    private ParticleSystem FireFlash;
    [SerializeField]
    Rigidbody rigidbody;

    public BotState BotState;

    private float fixedUpdateCount;

    float MyLocalTime = 0;

    List<Move> history;


    // Start is called before the first frame update
    void Start()
    {
        history = new List<Move>();
        controller = GetComponent<CharacterController>();
        animat = GetComponent<Animator>();
        photonView = GetComponent<PhotonView>();
        rigidbody = GetComponent<Rigidbody>();
        Destroy(playerCamera);
        Destroy(playerAudio);

        rigidbody.detectCollisions = false;
        controller.detectCollisions = false;
        //logfile = new System.IO.StreamWriter(@"C:\Users\Godzinski\gitRepos\PhotonTestGame\Assets\csv"+botColor+"BotLogFile"+Random.Range(1,1000).ToString()+".csv");
        //botlogfile = new System.IO.StreamWriter(@"C:\Users\Godzinski\gitRepos\PhotonTestGame\Assets\csv" + botColor + "BotLogFile" + Random.Range(1, 1000).ToString() + ".csv");
        // logfile.WriteLine("Second test ");
        StartTime = PhotonNetwork.Time;
        MyLocalTime = Time.realtimeSinceStartup;
        fixedUpdateCount = 1;

        BotState = BotState.Standing;
    }

    private void DisplayData()
    {

        Debug.Log("Postion " + transform.position + " DirectionVector " + moveDir + " (fixed) FrameUpdateCount " + fixedUpdateCount + " Local time " + Time.fixedTime + " Photon Time " + PhotonNetwork.Time + " state " + BotState);
    }
    private void WriteData()
    {
        logfile.WriteLine(transform.position + "," + moveDir + "," + fixedUpdateCount + "," + Time.time + "," + PhotonNetwork.Time + "," + BotState);
    }


    private void Move(double actionStartTime, double actionEndTime, Vector3 MoveDirection)
    {
        if (PhotonNetwork.Time > StartTime + actionStartTime && PhotonNetwork.Time < StartTime + actionEndTime)
        {
            Move newMove = new Move(MoveDirection, transform.rotation.eulerAngles, Time.deltaTime, (float)PhotonNetwork.Time, BotState);
            history.Add(newMove);

            animat.SetInteger("condition", 1);
            moveDir = MoveDirection;
            moveDir *= speed;
            moveDir = transform.TransformDirection(moveDir);

            moveDir.y -= gravity * Time.deltaTime;
            controller.Move(moveDir * Time.deltaTime);
            BotState = BotState.Moving;

        }
    }

    private void Move(Vector3 MoveDirection, float detaTime)
    {
        Vector3 mD = MoveDirection;
        mD *= speed;
        mD = transform.TransformDirection(mD);

        mD.y -= gravity * detaTime;
        controller.Move(mD * detaTime);
        BotState = BotState.Moving;

    }

    private void Stand(double actionStartTime, double actionEndTime)
    {
        if (PhotonNetwork.Time > StartTime + actionStartTime && PhotonNetwork.Time < StartTime + actionEndTime)
        {
            animat.SetInteger("condition", 0);
            BotState = BotState.Standing;
            Move newMove = new Move(Vector3.zero, transform.rotation.eulerAngles, Time.deltaTime, (float)PhotonNetwork.Time, BotState);
            history.Add(newMove);

        }
    }

    private void Stand()
    {
        animat.SetInteger("condition", 0);
        BotState = BotState.Standing;

    }

    private bool Rotate(Vector3 RotateVector, double actionTime)
    {
        //Debug.Log(PhotonNetwork.Time + " > " + (StartTime + actionTime) + " < " + (StartTime + actionTime + Time.fixedDeltaTime));
        if (PhotonNetwork.Time > StartTime + actionTime && PhotonNetwork.Time < StartTime + actionTime + (Time.fixedDeltaTime * 2))
        {
            BotState = BotState.Rotating;
            Move newMove = new Move(Vector3.zero, RotateVector, Time.deltaTime, (float)PhotonNetwork.Time, BotState);
            history.Add(newMove);

            animat.SetInteger("condition", 1);
            transform.localRotation = Quaternion.Euler(RotateVector);
            //transform.Rotate(RotateVector);


            return true;
        }
        return false;
    }

    private void Rotate(Vector3 RotateVector)
    {
        //Debug.Log(PhotonNetwork.Time + " > " + (StartTime + actionTime) + " < " + (StartTime + actionTime + Time.fixedDeltaTime));
        BotState = BotState.Rotating;

        animat.SetInteger("condition", 1);
        transform.localRotation = Quaternion.Euler(RotateVector);
        //transform.Rotate(RotateVector);

    }

    private void Shoot(double actionStartTime, double actionEndTime)
    {
        if (PhotonNetwork.Time > StartTime + actionStartTime && PhotonNetwork.Time < StartTime + actionEndTime)
        {
            animat.SetInteger("condition", 0);
            photonView.RPC("RPC_Shooting", RpcTarget.All);
            BotState = BotState.Shooting;
            Move newMove = new Move(Vector3.zero, transform.rotation.eulerAngles, Time.deltaTime, (float)PhotonNetwork.Time, BotState);
            history.Add(newMove);
        }
    }

    private void MovmentOrdersV2()
    {

        Move(0, 3, new Vector3(0, 0, 1));
        if (rotationCounter == 0)
        {
            if (Rotate(new Vector3(0, 100, 0), 2.5))
            {
                rotationCounter++;
            }
        }

        Move(3, 7, new Vector3(0, 0, 1));
        if (rotationCounter == 1)
        {
            if (Rotate(new Vector3(0, 130, 0), 6.5))
            {
                rotationCounter++;
            }
        }
        Move(7, 9, new Vector3(0, 0, 1));
        if (rotationCounter == 2)
        {
            if (Rotate(new Vector3(0, 10, 0), 7.5))
            {
                rotationCounter++;
            }
        }
        if (rotationCounter == 3)
        {
            if (Rotate(new Vector3(0, 220, 0), 8.5))
            {
                rotationCounter++;
            }
        }
        Move(9, 12, new Vector3(0, 0, 1));
        Shoot(12, 14);
        Move(14, 16, new Vector3(0, 0, 1));

        Stand(16, 20);
    }


    private void MovmentOrders()
    {


        if (PhotonNetwork.Time < StartTime + 3)
        {
            animat.SetInteger("condition", 1);
            moveDir = new Vector3(0, 0, 1);
            moveDir *= speed;
            moveDir = transform.TransformDirection(moveDir);

            moveDir.y -= gravity * Time.deltaTime;
            controller.Move(moveDir * Time.deltaTime);
            BotState = BotState.Moving;
            //Debug.Log(Time.time+" bot is moving");
        }
        else if (PhotonNetwork.Time > StartTime + 3 && PhotonNetwork.Time < StartTime + 3.5)
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
        else if (PhotonNetwork.Time > StartTime + 3.5 && PhotonNetwork.Time < StartTime + 7)
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
        else if (PhotonNetwork.Time > StartTime + 7 && PhotonNetwork.Time < StartTime + 9)
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
        else if (PhotonNetwork.Time > StartTime + 9 && PhotonNetwork.Time < StartTime + 10)
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
        else if (PhotonNetwork.Time > StartTime + 10 && PhotonNetwork.Time < StartTime + 11)
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
        else if (PhotonNetwork.Time > StartTime + 11 && PhotonNetwork.Time < StartTime + 12)
        {
            animat.SetInteger("condition", 0);
            photonView.RPC("RPC_Shooting", RpcTarget.All);
            BotState = BotState.Shooting;

        }
        else if (PhotonNetwork.Time > StartTime + 12 && PhotonNetwork.Time < StartTime + 14)
        {
            animat.SetInteger("condition", 1);
            moveDir = new Vector3(0, 0, 1);
            moveDir *= speed;
            moveDir = transform.TransformDirection(moveDir);

            moveDir.y -= gravity * Time.deltaTime;
            controller.Move(moveDir * Time.deltaTime);
        }
        else if (PhotonNetwork.Time > StartTime + 14)
        {
            animat.SetInteger("condition", 0);

            Debug.Log("Bot test ended");
            BotState = BotState.Standing;
        }
        //WriteData();

    }

    [PunRPC]
    void RPC_getTime()
    {
        SendMyTimeListners.Invoke(MyLocalTime.ToString(), botColor);
    }


    void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            //MovmentOrders();

            MovmentOrdersV2();

        }
        else
        {
            ClientPrediction();
        }
        fixedUpdateCount++;
        //  Debug.Log("Bot position is " + transform.position + "Time is " + Time.time);

    }

    public Vector3 getMoveDir()
    {
        if (moveDir != null)
        {
            return moveDir;
        }
        else
        {
            return Vector3.zero;
        }


    }


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (botColor == BotColor.Red)
        {
            Debug.Log("stream");
            if (stream.IsWriting)
            {

                //int moveCount = history.Count;
                //int firstMoveIndex = moveCount - 3;
                //if (firstMoveIndex < 3)
                //{
                //    firstMoveIndex = 0;
                //}

                //for (int i = firstMoveIndex; i < history.Count; i++)
                //{ }
                if (history.Count>0)
                {
                    stream.SendNext(history[history.Count - 1].moveDir);
                    stream.SendNext(history[history.Count - 1].RotateVector);
                    stream.SendNext(history[history.Count - 1].Timestamp);
                    stream.SendNext(history[history.Count - 1].state);
                }
                history.Clear();
                Debug.Log("BotColor " + botColor + " wrting postion: " + transform.position + "  rotation: " + transform.rotation);
            }
            else
            {
                Vector3 movedir = (Vector3)stream.ReceiveNext();
                Vector3 rotation = (Vector3)stream.ReceiveNext();
                float Timestamp = (float)stream.ReceiveNext();
                BotState botstate = (BotState)stream.ReceiveNext();
                //Move recivedData = (Move)stream.ReceiveNext();
                rtt = (float)PhotonNetwork.Time - Timestamp;

                AddToHistoryIfNew(movedir, rotation, Timestamp, botstate);


            }
        }
    }


    private bool AddToHistoryIfNew(Vector3 movedir, Vector3 rotation, float Timestamp, BotState state)
    {

        if (history != null && history.Count > 0 && Timestamp > history[history.Count - 1].Timestamp)
        {
            //Debug.Log("add to history " + Timestamp + " >  " + history[history.Count - 1].Timestamp);
            history[history.Count - 1].isDirty = true;
            float newDeltaTime = history[history.Count - 1].Timestamp >= 0f ?
                Timestamp - history[history.Count - 1].Timestamp : 0f;

            Move recivedData = new Move(movedir, rotation, newDeltaTime, Timestamp, state);
            history.Add(recivedData);
            return true;
        }
        else if (history != null && history.Count == 0)
        {
            // Debug.Log("Add to history");
            Move recivedData = new Move(movedir, rotation, 0, Timestamp, state);
            history.Add(recivedData);
        }

        return false;
    }


    private void ClientPrediction()
    {
        if (history.Count != 0)
        {
            float RTT = (float)rtt;
            float deltaTime = Time.deltaTime;
            SimulateMovement(deltaTime);
            //while (true)
            //{
            //    if (RTT < deltaTime)
            //    {
            //        SimulateMovement(RTT);
            //        break;
            //    }
            //    else
            //    {
            //        SimulateMovement(deltaTime);
            //        RTT -= deltaTime;
            //    }
            //}
        }

    }

    private void SimulateMovement(float deltaTime)
    {
        //history[history.Count - 1].moveDir *=deltaTime;

      
        
        history[0].isDirty = true;
        if (deltaTime<=0)
        {
            Debug.Log("correction");
            deltaTime = Time.deltaTime;
        }

        switch (history[0].state)
        {
            case BotState.Moving:
             
                if(history[0].RotateVector != transform.rotation.eulerAngles)
                {
                    Debug.Log("wrong rotation !!!! should be " + history[0].RotateVector + " is " + transform.rotation.eulerAngles);
                    Rotate(history[0].RotateVector);
                }
                else
                {
                    Debug.Log("simulate " + history[0].moveDir + " " + Time.deltaTime + " " + deltaTime);
                    Move(history[0].moveDir,deltaTime);
                }
                break;

            case BotState.Rotating:
                if (history[0].RotateVector != transform.rotation.eulerAngles)
                {
                    Rotate(history[0].RotateVector);
                }
                break;

            case BotState.Standing:
                Stand();
                break;

            case BotState.Shooting:
                Stand();
                break;

        }

        Move LastMove = history[history.Count - 1];
        history.RemoveAll(x => x.isDirty == true);
        if (history.Count == 0)
        {
            history.Add(LastMove);
        }
    }
}
