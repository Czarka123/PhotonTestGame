using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unreal : MonoBehaviour, IPunObservable, IOnEventCallback
{
    float MAXspeed = 4;
    public float CurrentSpeed = 4;
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

    public BotState BotState;
    private Vector3 moveDir = Vector3.zero;

    double StartTime;
    double CurrentPhotonTime;
    float MyLocalTime;
    int rotationCounter = 0;

    Vector3 currentPostion;

    Queue<SavedMove> savedMoves;


    // Start is called before the first frame update
    void Start()
    {
        PhotonPeer.RegisterType(typeof(SavedMove), 2, SavedMove.Serialize, SavedMove.Deserialize);

        controller = GetComponent<CharacterController>();
        animat = GetComponent<Animator>();
        photonView = GetComponent<PhotonView>();
        Destroy(playerCamera);
        Destroy(playerAudio);

        controller.detectCollisions = false;

        StartTime = PhotonNetwork.Time;
        MyLocalTime = Time.time;
        savedMoves = new Queue<SavedMove>();

        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 60;
    }

    private void PlaySavedMove(SavedMove savedMove)
    {
        Debug.Log("playimg move " + savedMove.timestamp +" coclor "+ botColor +"  dire " + savedMove.getDirection() + " rotation " + transform.rotation.eulerAngles.y + " recived rot is " + savedMove.rotationAngle);
        Move(savedMove.getDirection());
        moveDir = savedMove.getDirection();
        if (transform.rotation.eulerAngles.y != savedMove.rotationAngle)
        {
            transform.rotation = Quaternion.Euler(new Vector3(0, savedMove.rotationAngle, 0)); //to redo 
        }

    }

    private void Move(Vector3 MoveDirection)
    {
        BotState = BotState.Moving;
        moveDir = MoveDirection;
        animat.SetInteger("condition", 1);
        Vector3 mD = MoveDirection;
        mD *= CurrentSpeed;
        mD = transform.TransformDirection(mD);

        //mD.y -= gravity * detaTime;
        //Debug.Log("MOVEEE " + botColor + " :" + mD);
        controller.Move(mD * Time.deltaTime);
        currentPostion = transform.position;
    }

    IEnumerator Rotate(float Angle)
    {
        float moveSpeed = 2f;
        float correction = 1;

        Debug.Log("Rotate " + transform.rotation.eulerAngles.y + " < " + Angle);
        while (transform.rotation.eulerAngles.y < Angle - correction)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, Angle, 0), moveSpeed * Time.deltaTime);
            //Debug.Log("Lerp " + transform.rotation.eulerAngles.y + " < " + Angle);
            yield return null;
        }
        transform.rotation = Quaternion.Euler(0, Angle, 0);
        Debug.Log("Exit "); ;
        yield return null;
    }

    private void Stand()
    {
        animat.SetInteger("condition", 0);
        BotState = BotState.Standing;
    }

    void MovmentScript()
    {
        if (MyLocalTime + 2 > Time.time)
        {
            Move(new Vector3(0, 0, 1.0f));
        }
        else if (MyLocalTime + 3 > Time.time && MyLocalTime + 2 < Time.time)
        {
            if (rotationCounter == 0)
            {
                rotationCounter++;
                StartCoroutine(Rotate(90));
            }

            Move(new Vector3(0, 0, 1.0f));
        }
        else if (MyLocalTime + 5 > Time.time && MyLocalTime + 3 < Time.time)
        {

            Move(new Vector3(0, 0, 1.0f));
        }
        else if (MyLocalTime + 7 > Time.time && MyLocalTime + 5 < Time.time)
        {
            if (rotationCounter == 1)
            {
                rotationCounter++;
                StartCoroutine(Rotate(190));
            }

            Move(new Vector3(0, 0, 1.0f));
        }
        else if (MyLocalTime + 8 > Time.time && MyLocalTime + 7 < Time.time)
        {

            Move(new Vector3(1.0f, 0, 0));
        }
        else
        {
            Stand();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(photonView.IsMine && botColor==BotColor.Green)
        {
            MovmentScript();

            SavedMove toSaveMove = new SavedMove(PhotonNetwork.Time, moveDir.z, moveDir.x, transform.rotation.eulerAngles.y, transform.position.x, transform.position.y, transform.position.z, controller.velocity.x, controller.velocity.y, controller.velocity.z, CurrentSpeed, false);
            if (savedMoves != null)
            {
                savedMoves.Enqueue(toSaveMove);
            }
        }
        else if (!photonView.IsMine)
        {
            if (savedMoves != null && savedMoves.Count > 0)
            {
                SavedMove RecivedMove = savedMoves.Dequeue();
                PlaySavedMove(RecivedMove);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (savedMoves != null && savedMoves.Count>0)
            {
                SavedMove toSendMove = savedMoves.Dequeue();
                if (botColor==BotColor.Green)
                {  
                    byte evCode = 1;
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                    SendOptions sendOptions = new SendOptions { Reliability = true };
                    PhotonNetwork.RaiseEvent(evCode, toSendMove, raiseEventOptions, sendOptions);
                }
                else if(botColor == BotColor.Red)
                {
                    stream.SendNext(toSendMove);
                }             
            }
        }
        else
        {
            SavedMove recivepack = (SavedMove)stream.ReceiveNext();
            Debug.Log("recived packet " + recivepack.timestamp);
            if (savedMoves != null)
            {
                savedMoves.Enqueue(recivepack);
            }
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == 1)
        {
            if (botColor == BotColor.Red && photonView.IsMine)
            {
                Debug.Log("server recived information packet ");

                SavedMove savedMove = (SavedMove)photonEvent.CustomData;
                //Debug.Log("recived action from client numba :" + action.sequence_number);
                PlaySavedMove(savedMove);
                SavedMove PerformedMove = new SavedMove(PhotonNetwork.Time, moveDir.z, moveDir.x, transform.rotation.eulerAngles.y, transform.position.x, transform.position.y, transform.position.z, controller.velocity.x, controller.velocity.y, controller.velocity.z, CurrentSpeed, false);
                savedMoves.Enqueue(PerformedMove); //for now no history for server
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
