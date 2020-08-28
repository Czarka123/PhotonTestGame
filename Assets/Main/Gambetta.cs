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
    [SerializeField]
    private GameObject LaserEffect;
    private GameObject LaserObject;
    private BotState BotState;

    double StartTime;
    float MyLocalTime;
    float journeyLength;


    float speed = 0.075f;

    Action currentAction;

    Action LastAction;
    Action NewAction;

    Action LastSavedAction;

    bool interpolationMove = false;

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

    private void LerpMovement(Action startPack, Vector3 endPostion)
    {
        if (startPack.rotationAngle != transform.rotation.eulerAngles.y)
        {
            transform.Rotate(new Vector3(0, (startPack.rotationAngle - transform.rotation.eulerAngles.y), 0));
        }

        if (startPack.shooting == true)
        {
            Shooting();
        }
        if (startPack.animationState == 0 || journeyLength==0)
        {
            Debug.Log(" should stand !!!!!!!!!!!!################");
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
            // Distance moved equals elapsed time times speed..
            float distCovered = (Time.time - MyLocalTime) * 200;

            // Fraction of journey completed equals current distance divided by total distance.
            float fractionOfJourney = distCovered / journeyLength;

            //Debug.Log(" time  " + (Time.time - MyLocalTime) + " journey  " + fractionOfJourney + " postions from " + startPack.getPostion() + " to end on " + endPostion);

            // Set our position as a fraction of the distance between the markers.

            transform.position = Vector3.Lerp(startPack.getPostion(), endPostion, fractionOfJourney);
        }
    }

    private void Recouncil(Action actionpack)
    {
        int history_counter = 0;
        for (int i = 0;i < history.Count; i++)
        {

            if(history[i].sequence_number>= actionpack.sequence_number)
            {
                //if (botColor == BotColor.Target)
                //{
                //    Debug.Log(" history " + history[i].sequence_number + "pos  " + history[i].getPostion() + " re-pack  " + actionpack.sequence_number + " pos " + actionpack.getPostion());
                //}
                if ((history[i].getPostion() == actionpack.getPostion()))
                {
                    //  Debug.Log("correct history " + history[i].sequence_number + " numba  "+ history[i].sequence_number);
                }
                else
                {
                    //if (botColor == BotColor.Target)
                    //{
                    //    Debug.Log("WRONG HISTORY " + history[i].sequence_number + " postion " + history[i].getPostion() + "  server corrected " + actionpack.sequence_number + " postion: " + actionpack.getPostion());
                    //}
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
       
        //history.RemoveRange(0, history_counter);
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
        StartCoroutine(LaserAnimation(0.2f));
        //shooting = true;
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
               
                if (botColor == BotColor.Green && photonView.IsMine)
                {
                    Debug.Log("TARGET hit at " + hit.transform.position + " sq number "+hit.transform.GetComponent<Gambetta>().sequence_number + " !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    byte evCode = 3;
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                    SendOptions sendOptions = new SendOptions { Reliability = true };
                    Action hitInfo = new Action(hit.transform.position.x, hit.transform.position.y, hit.transform.position.z, sequence_number, 0, 0, true);
                    PhotonNetwork.RaiseEvent(evCode, hitInfo, raiseEventOptions, sendOptions);
                }
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

        LaserObject = Instantiate(LaserEffect, rayOrgin);
        LaserObject.SetActive(false);

        to_send = new Queue<Action>();
        history = new List<Action>();

        StartTime = PhotonNetwork.Time;
        MyLocalTime = Time.time;

        currentAction = new Action(transform.position.x, transform.position.y, transform.position.z, 0,0, transform.rotation.eulerAngles.y, false);
        recivedAction = new Action();
         LastAction = new Action(); 
         NewAction = new Action();

        LastSavedAction = new Action();
        sequence_number = 0;

        PhotonNetwork.SendRate = 100;
        PhotonNetwork.SerializationRate = 100;      
    }

    void TargetMovmentScript()
    {
        if (Mathf.CeilToInt(Time.time) % 4 == 0 || Mathf.CeilToInt(Time.time) % 4 == 1)
        {
            MoveFromInput(new Vector3(0, 0, speed));

        }
        else if (Mathf.CeilToInt(Time.time) % 4 == 2 || Mathf.CeilToInt(Time.time) % 4 == 3)
        {
            MoveFromInput(new Vector3(0, 0, -speed));
        }

        //Stand();
    }

    private void MovementSequnce()
    {
        if (MyLocalTime + 3 > Time.time)
        {
            MoveFromInput(new Vector3(0, 0, speed));
            // Debug.Log("postion is " + transform.position);
        }
        else if (MyLocalTime + 3 < Time.time && MyLocalTime + 5 > Time.time)
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
        else if (MyLocalTime + 5 < Time.time && MyLocalTime + 7> Time.time)
        {
            Shooting();
        }
        else if (MyLocalTime + 7 < Time.time && MyLocalTime + 8 > Time.time)
        {
            MoveFromInput(new Vector3(speed, 0, 0));
        }
        else if (MyLocalTime + 8 < Time.time && MyLocalTime + 10 > Time.time)
        {
            if (rotation_counter == 1)
            {
                Rotate(90);
                rotation_counter++;
                // Debug.Log("rotation is " + transform.rotation.eulerAngles.y);
            }
            MoveFromInput(new Vector3(0, 0, -speed));
        }
        else if (MyLocalTime + 10 < Time.time && MyLocalTime + 11 > Time.time)
        {
            MoveFromInput(new Vector3(0, 0, -speed));
        }
        else if (MyLocalTime + 11 < Time.time && MyLocalTime + 13 > Time.time)
        {
            if (rotation_counter == 2)
            {
                Rotate(90);
                rotation_counter++;
                //Debug.Log("rotation is " + transform.rotation.eulerAngles.y);
            }
            MoveFromInput(new Vector3(-speed, 0, 0));
        }
        else
        {
            Stand();
        }
        //else if (MyLocalTime + 3 < Time.time && MyLocalTime + 4 > Time.time)
        //{
        //    MoveFromInput(new Vector3(speed, 0, 0));
        //}
        //else if (MyLocalTime + 4 < Time.time && MyLocalTime + 5 > Time.time)
        //{
        //    if (rotation_counter == 1)
        //    {
        //        Rotate(90);
        //        rotation_counter++;
        //        // Debug.Log("rotation is " + transform.rotation.eulerAngles.y);
        //    }
        //    MoveFromInput(new Vector3(0, 0, -speed));
        //}
        //else if (MyLocalTime + 5 < Time.time && MyLocalTime + 6 > Time.time)
        //{
        //    MoveFromInput(new Vector3(0, 0, -speed));
        //}
        //else if (MyLocalTime + 6 < Time.time && MyLocalTime + 7 > Time.time)
        //{
        //    if (rotation_counter == 2)
        //    {
        //        Rotate(90);
        //        rotation_counter++;
        //        //Debug.Log("rotation is " + transform.rotation.eulerAngles.y);
        //    }
        //    MoveFromInput(new Vector3(-speed, 0, 0));
        //}
        //else if (MyLocalTime + 7 < Time.time && MyLocalTime + 8 > Time.time)
        //{       
        //    Shooting();       
        //}
        //else
        //{
        //    Stand();
        //}
    }



    void FixedUpdate()
    {

        //Debug.Log(botColor + " " + photonView.IsMine + "    " + name);
        if (photonView.IsMine && botColor == BotColor.Green)
        {
            MovementSequnce();
            byte evCode = 1;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendOptions sendOptions = new SendOptions { Reliability = true };
            PhotonNetwork.RaiseEvent(evCode, currentAction, raiseEventOptions, sendOptions);
        }
        else if (photonView.IsMine && botColor == BotColor.Target)
        {
            TargetMovmentScript();

            //Action saveAction = new Action(currentAction.postionX, currentAction.postionY, currentAction.postionZ, currentAction.sequence_number, currentAction.animationState, currentAction.rotationAngle, currentAction.shooting);
            //to_send.Enqueue(saveAction); 
        }
        else if (!photonView.IsMine)
        {
            //Debug.Log("current : " + currentAction.sequence_number + " < " + recivedAction.sequence_number);
            if (currentAction.sequence_number < recivedAction.sequence_number)
            {


                //if(botColor == BotColor.Target)
                //{
                //    MoveFromAction(recivedAction);
                //    //Debug.Log(" Rpack " + recivedAction.sequence_number + " postion " + recivedAction.getPostion());
                //    //Debug.Log(" Me " + sequence_number + " postion " + transform.position);
                //    //Debug.Log("!!!");
                //    history.Add(recivedAction);
                //    byte evCode = 2;
                //    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                //    SendOptions sendOptions = new SendOptions { Reliability = true };
                //    PhotonNetwork.RaiseEvent(evCode, recivedAction, raiseEventOptions, sendOptions);

                //}
                if (botColor == BotColor.Red || botColor == BotColor.Target)
                {
                    if (botColor == BotColor.Target)
                    {
                        history.Add(recivedAction);
                        byte evCode = 2;
                        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                        SendOptions sendOptions = new SendOptions { Reliability = true };
                        PhotonNetwork.RaiseEvent(evCode, recivedAction, raiseEventOptions, sendOptions);
                        //MoveFromAction(recivedAction); //without interpolation
                    }

                    if (to_send.Count > 1 && !interpolationMove)
                    {
                        // Debug.Log(" interpolating change");
                        interpolationMove = true;
                        if (LastAction.sequence_number == -1)
                        {
                            Debug.Log(" startttt ");
                            LastAction = to_send.Dequeue();
                        }
                        else
                        {
                            LastAction = NewAction;
                            Debug.Log("getting action  ");
                        }
                        NewAction = to_send.Dequeue();
                        MyLocalTime = Time.time;
                        journeyLength = Vector3.Distance(LastAction.getPostion(), NewAction.getPostion());

                    }

                    if (interpolationMove)
                    {
                        
                        Debug.Log("interpolating from " + LastAction.getPostion() + " to " + NewAction.getPostion() + " my postion " + transform.position + " que size " + to_send.Count + " shooting " + NewAction.shooting + " anim " + NewAction.animationState);
                        
                        LastAction.shooting = NewAction.shooting;
                        LastAction.animationState = NewAction.animationState;
                        LerpMovement(LastAction, NewAction.getPostion());
                        float distance = Mathf.Abs(Vector3.Distance(transform.position, NewAction.getPostion()));
                        Debug.Log("distance after " + distance);
                        if (distance < 0.01f)
                        {
                            interpolationMove = false;
                        }
                    }
                }
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


          


            if (botColor == BotColor.Red || botColor == BotColor.Target)
            {
                recivedAction = (Action)stream.ReceiveNext();

                if (to_send != null)
                {
                    if (to_send.Count > 2)
                    {
                        float dis = Vector3.Distance(LastSavedAction.getPostion(), recivedAction.getPostion());

                       // Debug.Log(" beetween packet distance " + dis);

                        if (dis > 0.4f || LastSavedAction.shooting != recivedAction.shooting)
                        {
                            to_send.Enqueue(recivedAction);
                            LastSavedAction = recivedAction;
                        }

                    }
                    else
                    {
                        to_send.Enqueue(recivedAction);
                        LastSavedAction = recivedAction;
                    }

                }
            }
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

        if (photonEvent.Code == 2)
        {
            if (botColor == BotColor.Target && photonView.IsMine)
            {
                Action re_action = (Action)photonEvent.CustomData;
                Debug.Log("recouncil_action : targets");
              //  Debug.Log(" Re - pack " + re_action.sequence_number + " postion " + re_action.getPostion());
              //  Debug.Log(" Me " + sequence_number + " postion " + transform.position);
                Recouncil(re_action);
            }
        }

        if (photonEvent.Code == 3)
        {
            if (botColor == BotColor.Target && !photonView.IsMine)
            {
                Debug.Log("GOT hit " + photonView.IsMine);
                Action hitData = (Action)photonEvent.CustomData;

                // int indexOfHit = history.FindIndex(sq => sq.sequence_number == hitData.sequence_number);
                // Debug.Log();
               // Debug.Log("My history on sequence_number  " + hitDatasequence_number + " on postion " + transform.position);
                if (hitData.sequence_number > sequence_number)
                {
                    Debug.Log("Waiting for confirmation");
                    StartCoroutine(WaitForFrameTOCOnfirmShoot(hitData));
                }
                else
                {
                    Debug.Log("No NEED for Waiting");
                }

            }
        }
    }

    IEnumerator WaitForFrameTOCOnfirmShoot(Action hitData)
    {
        float st = Time.time;
        float postionError;
        yield return new WaitUntil(() => sequence_number >= hitData.sequence_number);
         Debug.Log("got hit on seq " + hitData.sequence_number + " on postion " + hitData.getPostion() + " counter " + sequence_number);
        // history[sequence_number - 1].sequence_number + " on postion " + history[sequence_number - 1].getPostion());
        Debug.Log("My history on sequence  " + sequence_number + " history size : " + history.Count + " last postion " + history[hitData.sequence_number - 1].getPostion());
        if (history[hitData.sequence_number - 1].sequence_number == hitData.sequence_number)
        {
            postionError = Mathf.Abs(Vector3.Distance(history[hitData.sequence_number - 1].getPostion(), hitData.getPostion()));
        }
        else
        {
            postionError = Mathf.Abs(Vector3.Distance(history[sequence_number - 1].getPostion(), hitData.getPostion()));
        }

        if (postionError <= 0.3f)
        {
            Debug.Log("shoot CONFIRMED diffrence "+ postionError + " time waited for "+ (Time.time- st));
        }
        else
        {
            Debug.Log("shoot BUSTED diffrence " + postionError + " time waited for " + (Time.time - st));
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
