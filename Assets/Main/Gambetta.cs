using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gambetta : MonoBehaviour, IPunObservable, IOnEventCallback
{
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
    private BotState BotState;

    double StartTime;
    float MyLocalTime;

    public float speed = 0.075f;

    Action currentAction;
    Queue<Action> to_send;
    List<Action> history;
    //List<Action> history_confirmed;

    Action recivedAction;
    int sequence_number;

    int rotation_counter = 0;

    private void Stand()
    {
        if (BotState != BotState.Standing)
        {
            BotState = BotState.Standing;
            sequence_number++;
            currentAction.SetAction(transform.position, sequence_number,0,transform.rotation.eulerAngles.y,false);

            Action saveAction = new Action(currentAction.postionX, currentAction.postionY, currentAction.postionZ, sequence_number, 1, transform.rotation.eulerAngles.y, false);
            history.Add(saveAction);
            to_send.Enqueue(saveAction);
            Debug.Log(" standing " + sequence_number);
        }
        animat.SetInteger("condition", 0);
    }


    private void MoveFromAction(Action pack)
    {
        //Debug.Log("REAL " + transform.position + " rot " + transform.rotation.eulerAngles.y);
        //Debug.Log("pack "+pack.getPostion() + " rot " + pack.rotationAngle + " shooting "+ pack.shooting);
        if (pack.rotationAngle != transform.rotation.eulerAngles.y)
        {
            transform.Rotate(new Vector3(0, (pack.rotationAngle- transform.rotation.eulerAngles.y), 0));
        }

        if (pack.shooting == true)
        {
            Shooting();
        }
        else if(pack.animationState==0)
        {
            if (BotState != BotState.Standing)
            {
                BotState = BotState.Standing;
                animat.SetInteger("condition", 0);
                sequence_number++;
                currentAction.SetAction(transform.position, sequence_number, 0, transform.rotation.eulerAngles.y, false);
                Debug.Log("got stand");
            }
        }
        else
        {
            animat.SetInteger("condition", 1);
            transform.position = pack.getPostion();
            BotState = BotState.Moving;
            sequence_number++;
            currentAction.SetAction(transform.position, sequence_number, 1, transform.rotation.eulerAngles.y, false);
        }
    }

    private void Recouncil(Action actionpack)
    {
        int history_counter = 0;
        for (int i = 0;i < history.Count; i++)
        {

            if(history[i].sequence_number>= actionpack.sequence_number)
            {
                if ((history[i].getPostion() == actionpack.getPostion()))
                {
                    Debug.Log("correct history " + history[i].sequence_number + " numba  "+ history[i].sequence_number);
                }
                else
                {
                    Debug.Log("WRONG HISTORY " + history[i].sequence_number + " postion "+ history[i].getPostion() + "  server corrected "+ actionpack.sequence_number + " postion: "+ actionpack.getPostion());

                    Vector3 difference = history[i].getPostion() - actionpack.getPostion();

                    transform.position -= difference;
                    for (int j = i; j < history.Count; j++)
                    {                    
                        history[j].setPostion(history[j].getPostion() - difference);
                    } 
                }
                history_counter = i;
                break;
            }       
        }
        history.RemoveRange(0, history_counter);
    }
    
    

    private void MoveFromInput(Vector3 input)
    {     
        animat.SetInteger("condition", 1);
        transform.position += input;
        BotState = BotState.Moving;
        sequence_number++;
        currentAction.SetAction(transform.position, sequence_number,1, transform.rotation.eulerAngles.y, false);
      
        Action saveAction = new Action( currentAction.postionX,currentAction.postionY,currentAction.postionZ,sequence_number,1, transform.rotation.eulerAngles.y, false);
        history.Add(saveAction);
        to_send.Enqueue(saveAction);
    }

    private void Rotate (float angle)
    {
        transform.Rotate(new Vector3(0, angle, 0));
        sequence_number++;
        currentAction.SetAction(transform.position, sequence_number, 1, transform.rotation.eulerAngles.y, false);
    }

    private void Shooting()
    {
        RaycastHit hit;
        FireFlash.Play();
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
        sequence_number++;
        currentAction.SetAction(transform.position, sequence_number, 2, transform.rotation.eulerAngles.y, true);
    }

    void Start()
    {

        PhotonPeer.RegisterType(typeof(Action), 5, Action.Serialize, Action.Deserialize);

        controller = GetComponent<CharacterController>();
        animat = GetComponent<Animator>();
        photonView = GetComponent<PhotonView>();
        Destroy(playerCamera);
        Destroy(playerAudio);
        controller.detectCollisions = false;
        BotState = BotState.Standing;

        to_send = new Queue<Action>();
        history = new List<Action>();

        StartTime = PhotonNetwork.Time;
        MyLocalTime = Time.time;

        currentAction = new Action(transform.position.x, transform.position.y, transform.position.z, 0,0, transform.rotation.eulerAngles.y, false);
        recivedAction = new Action();
        sequence_number = 0;

        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 60;      
    }

    private void MovementSequnce()
    {
        if (MyLocalTime + 2 > Time.time)
        {
            MoveFromInput(new Vector3(0, 0, speed));
            // Debug.Log("postion is " + transform.position);
        }
        else if (MyLocalTime + 2 < Time.time && MyLocalTime + 3 > Time.time)
        {
            if (rotation_counter == 0)
            {
                // Debug.Log("rotation is " + transform.rotation.eulerAngles.y);
                rotation_counter++;
                Rotate(90);
                // Debug.Log("rotation is " + transform.rotation.eulerAngles.y);
            }
            MoveFromInput(new Vector3(speed, 0, 0));
        }
        else if (MyLocalTime + 3 < Time.time && MyLocalTime + 4 > Time.time)
        {
            MoveFromInput(new Vector3(speed, 0, 0));
        }
        else if (MyLocalTime + 4 < Time.time && MyLocalTime + 5 > Time.time)
        {
            if (rotation_counter == 1)
            {
                Rotate(90);
                rotation_counter++;
                // Debug.Log("rotation is " + transform.rotation.eulerAngles.y);
            }
            MoveFromInput(new Vector3(0, 0, -speed));
        }
        else if (MyLocalTime + 5 < Time.time && MyLocalTime + 6 > Time.time)
        {
            MoveFromInput(new Vector3(0, 0, -speed));
        }
        else if (MyLocalTime + 6 < Time.time && MyLocalTime + 7 > Time.time)
        {
            if (rotation_counter == 2)
            {
                Rotate(90);
                rotation_counter++;
                //Debug.Log("rotation is " + transform.rotation.eulerAngles.y);
            }
            MoveFromInput(new Vector3(-speed, 0, 0));
        }
        else if (MyLocalTime + 7 < Time.time && MyLocalTime + 8 > Time.time)
        {       
            Shooting();       
        }
        else
        {
            Stand();
        }
    }


    void FixedUpdate()
    {
        if (photonView.IsMine && botColor == BotColor.Green)
        {
            MovementSequnce();
            byte evCode = 1;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendOptions sendOptions = new SendOptions { Reliability = true };
            PhotonNetwork.RaiseEvent(evCode, currentAction, raiseEventOptions, sendOptions);
        }
        else if(!photonView.IsMine)
        {
            //Debug.Log("current : " + currentAction.sequence_number + " < " + recivedAction.sequence_number);
            if (currentAction.sequence_number < recivedAction.sequence_number)
            {
                MoveFromAction(recivedAction);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (to_send != null && to_send.Count > 0)
            {
                //Action ac = to_send.Dequeue();
                stream.SendNext(to_send.Dequeue()); //not sure if best
            }

            //Debug.Log("WRITING pack numba: " + currentAction.sequence_number);
            //stream.SendNext(currentAction.sequence_number);
            //stream.SendNext(currentAction.getPostion());
            //stream.SendNext(currentAction.animationState);
            //stream.SendNext(currentAction.rotationAngle);
        }
        else
        {

            //Debug.Log(botColor + " " + photonView.IsMine + "    " + name);
            recivedAction = (Action)stream.ReceiveNext();
            
            
            //int recivedAction_sq = (int)stream.ReceiveNext();
            //Vector3 recivedAction_pos = (Vector3)stream.ReceiveNext();
            //int recivedAction_anim= (int)stream.ReceiveNext();
            //float recivedAction_rot=(float)stream.ReceiveNext();
            //recivedAction.SetAction(recivedAction_pos, recivedAction_sq, recivedAction_anim, recivedAction_rot,false);
            //Debug.Log("recived pack numba: " + recivedAction.sequence_number);
        }
    }


    public void OnEvent(EventData photonEvent)
    {

        if (photonEvent.Code == 1)
        {
            if (botColor == BotColor.Red && photonView.IsMine)
            {
                Action action = (Action)photonEvent.CustomData;
                //Debug.Log("recived action from client numba :" + action.sequence_number);
                MoveFromAction(action);
                Action saveAction = new Action(currentAction.postionX, currentAction.postionY, currentAction.postionZ, currentAction.sequence_number, currentAction.animationState, currentAction.rotationAngle, currentAction.shooting);
                to_send.Enqueue(saveAction); //for now no history for server


                byte evCode = 1;
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                SendOptions sendOptions = new SendOptions { Reliability = true };
                PhotonNetwork.RaiseEvent(evCode, currentAction, raiseEventOptions, sendOptions);
            }
            else if (botColor == BotColor.Green && photonView.IsMine)
            {              
                Action re_action = (Action)photonEvent.CustomData;
                //Debug.Log("recouncil_action :" + re_action.sequence_number);
                Recouncil(re_action);
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
