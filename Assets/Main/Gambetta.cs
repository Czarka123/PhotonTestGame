using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    float distCovered;


    float speed = 0.085f; //was 0.075f

    Action currentAction;

    Action LastAction;
    Action NewAction;

    Action LastSavedAction;

    bool interpolationMove = false;
    public bool queueActive = false;
    private bool historychangeLock=false;

    Queue<Action> to_send;
    List<Action> history;
    //List<Action> history_confirmed;

    Action recivedAction;
    int sequence_number;

    int rotation_counter = 0;

    StringBuilder logfile;
    int SaveCounter = 0;

    void SaveCurrentStateToFile()
    {

        var newLine = string.Format("{0}:{1}:{2}:{3}:{4}", SaveCounter, transform.position.x, transform.position.z, Mathf.Ceil(transform.rotation.eulerAngles.y), PhotonNetwork.Time);
        logfile.AppendLine(newLine); 
    }

    private void Stand()
    {
        if (BotState != BotState.Standing)
        {
            BotState = BotState.Standing;
            sequence_number++;
            currentAction.SetAction(transform.position,Vector3.zero, sequence_number,PhotonNetwork.Time, 0,transform.rotation.eulerAngles.y,false);

            Action saveAction = new Action(currentAction.postionX, currentAction.postionY, currentAction.postionZ, sequence_number, 1, transform.rotation.eulerAngles.y, false);
            history.Add(saveAction);
            to_send.Enqueue(saveAction);
        }
        animat.SetInteger("condition", 0);
    }


    private void MoveFromAction(Action pack)
    {

        if (pack.rotationAngle != transform.rotation.eulerAngles.y)
        {
            transform.Rotate(new Vector3(0, (pack.rotationAngle - transform.rotation.eulerAngles.y), 0));
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
                currentAction.SetAction(transform.position,Vector3.zero, sequence_number, pack.timestamp, 0, transform.rotation.eulerAngles.y, false);
                Debug.Log("got stand");
            }
        }
        else
        {
            animat.SetInteger("condition", 1);
            MoveFromInput(pack.getInput());
            BotState = BotState.Moving;
        } 
    }


    private void MoveFromInput(Vector3 input)
    {
        animat.SetInteger("condition", 1);
        transform.position += input;
        BotState = BotState.Moving;
        sequence_number++;
        currentAction.SetAction(transform.position, input, sequence_number,PhotonNetwork.Time, 1, transform.rotation.eulerAngles.y, false);

        Action saveAction = new Action(currentAction.postionX, currentAction.postionY, currentAction.postionZ, input.x,input.z, sequence_number, PhotonNetwork.Time, 1, transform.rotation.eulerAngles.y, false);
        history.Add(saveAction);

        //to_send.Enqueue(saveAction);
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
                currentAction.SetAction(transform.position,startPack.getInput(), sequence_number,PhotonNetwork.Time, 0, transform.rotation.eulerAngles.y, false);
                Debug.Log("got stand");
            }
        }
        else
        {
            animat.SetInteger("condition", 1);
            // Distance moved equals elapsed time times speed..
            

            // Fraction of journey completed equals current distance divided by total distance.
            float fractionOfJourney = distCovered / journeyLength;
            Vector3 finalPos = Vector3.Lerp(startPack.getPostion(), endPostion, fractionOfJourney);
            Vector3 currentPos = transform.position;

            // Set our position as a fraction of the distance between the markers.

            transform.position = finalPos;
            distCovered = distCovered + Vector3.Distance(currentPos, finalPos);
        }
    }



    private void Recouncil(Action actionpack)
    {
        if (!historychangeLock)
        {
            int history_counter = 0;
            for (int i = 0; i < history.Count; i++)
            {

                if (history[i].sequence_number == actionpack.sequence_number)
                {

                    if ((history[i].getPostion() == actionpack.getPostion())) //can make small error buffor here
                    {
                        Debug.Log("correct history ");
                    }
                    else
                    {
                        // Debug.Log("Wrong history ");
                        historychangeLock = true;
                        Vector3 difference = history[i].getPostion() - actionpack.getPostion();              

                        transform.position -= difference;
                        for (int j = i; j < history.Count; j++)
                        {
                            history[j].setPostion(history[j].getPostion() - difference);
                        }
                        historychangeLock = false;
                    }
                    history_counter = i;
                    break;
                }
            }

        }
    }
    


    private void Rotate (float angle)
    {
        transform.Rotate(new Vector3(0, angle, 0));
        sequence_number++;
        currentAction.SetAction(transform.position, Vector3.zero, sequence_number, PhotonNetwork.Time, 1, transform.rotation.eulerAngles.y, false);
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
                    Action hitInfo = new Action(hit.transform.position.x, hit.transform.position.y, hit.transform.position.z,0,0, sequence_number,PhotonNetwork.Time ,0, 0, true);
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
        currentAction.SetAction(transform.position,Vector3.zero, sequence_number,PhotonNetwork.Time, 2, transform.rotation.eulerAngles.y, true);
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

        logfile = new StringBuilder();
        var newLine = string.Format("{0}:{1}:{2}:{3}:{4}", "Index", "PostionX", "PostionZ", "RotationAngle", "PhotonTime");
        logfile.AppendLine(newLine);

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
        if (history != null && history.Count > 0)
        {
            to_send.Enqueue(history[history.Count-1]);
        }
            //Stand();
    }

    private void MovementTestSequnce()
    {
       
        double startTime = PhotonNetwork.Time;
        if (MyLocalTime + 3 > Time.time)
        {
            MoveFromInput(new Vector3(0, 0, speed));
        }
        else if (MyLocalTime + 4 > Time.time && MyLocalTime + 3 < Time.time)
        {
           
            if (rotation_counter == 0)
            {
                rotation_counter++;
                Rotate(-90);
            }
            MoveFromInput(new Vector3(-speed, 0, 0));

        }
        else if (MyLocalTime + 5 > Time.time && MyLocalTime + 4 < Time.time)
        {

            MoveFromInput(new Vector3(speed, 0, 0));

        }
        else if (MyLocalTime + 6 > Time.time && MyLocalTime + 5 < Time.time)
        {

            MoveFromInput(new Vector3(0, 0, speed));

        }
        else if (MyLocalTime + 7 > Time.time && MyLocalTime + 6 < Time.time)
        {
          
            if (rotation_counter == 1)
            {
                rotation_counter++;
                Rotate(135);
            }
            MoveFromInput(new Vector3(speed / 2, 0, speed/2));
        }
        else if (MyLocalTime + 8 > Time.time && MyLocalTime + 7 < Time.time)
        {
            if (rotation_counter == 2)
            {
                rotation_counter++;
                Rotate(90);
            }
            MoveFromInput(new Vector3(speed / 2, 0, -speed / 2));
        }
        else if (MyLocalTime + 9 > Time.time && MyLocalTime + 8 < Time.time)
        {
            if (rotation_counter == 3)
            {
                rotation_counter++;
                Rotate(90);
            }
            MoveFromInput(new Vector3(-speed / 2, 0, -speed / 2));
        }
        else if (MyLocalTime + 10 > Time.time && MyLocalTime + 9 < Time.time)
        {
            if (rotation_counter == 4)
            {
                rotation_counter++;
                Rotate(90);
            }
            MoveFromInput(new Vector3(-speed / 2, 0, speed / 2));
        }
        else if (MyLocalTime + 11 > Time.time && MyLocalTime + 10 < Time.time)
        {
            if (rotation_counter == 5)
            {
                rotation_counter++;
                Rotate(135);
            }
            MoveFromInput(new Vector3(speed, 0, 0));
        }
        else if (MyLocalTime + 12 > Time.time && MyLocalTime + 11 < Time.time)
        {
            Shooting();

        }
        else
        {
            Stand();
        }



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
        else if (MyLocalTime + 5 < Time.time && MyLocalTime + 7 > Time.time)
        {
            //Stand();
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
                transform.position += new Vector3(0.2f, 0, 0);
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
    }



    void FixedUpdate()
    {
        SaveCounter++;
        //Debug.Log(botColor + " " + photonView.IsMine + "    " + name);
        if (photonView.IsMine && botColor == BotColor.Green)
        {
            MovementTestSequnce();
            SaveCurrentStateToFile();
            byte evCode = 1;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendOptions sendOptions = new SendOptions { Reliability = true };
            PhotonNetwork.RaiseEvent(evCode, currentAction, raiseEventOptions, sendOptions);
        }
        else if (photonView.IsMine && botColor == BotColor.Target)
        {
            TargetMovmentScript();
        }
        else if (!photonView.IsMine)
        {
            if (currentAction.sequence_number < recivedAction.sequence_number)
            {
                if (queueActive)
                {
                    InterpolateFromQueue();
                    SaveCurrentStateToFile();
                }
                else
                {
                    InterpolateFromLatestAction();
                    SaveCurrentStateToFile();
                }
               //
                
            }
        }

        if (SaveCounter == 750)
        {
            string filename = "";
            if (photonView.IsMine && botColor == BotColor.Green)
            {
                filename = "GambettaGreen";

            }
            else if (photonView.IsMine && botColor == BotColor.Red)
            {
                filename = "GambettaRedServer";
            }
            else if (!photonView.IsMine && botColor == BotColor.Red)
            {
                filename = "GambettaRedProxy";
            }

            File.WriteAllText(@"path" + filename + ".csv", logfile.ToString());

            Debug.Log(" Log Saved in file ########################################################");
        }

    }
    private void InterpolateFromQueue()
    {

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

            if (to_send.Count == 0)
            {
                Debug.Log(" interpolation buffer " + to_send.Count);
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
                distCovered = journeyLength * 0.25f;
            }

            if (interpolationMove)
            {
              
                LastAction.shooting = NewAction.shooting;
                LastAction.animationState = NewAction.animationState;
                LerpMovement(LastAction, NewAction.getPostion());
                float distance = Mathf.Abs(Vector3.Distance(transform.position, NewAction.getPostion()));
                if (distance < 0.01f)
                {
                    interpolationMove = false;
                }
            }
        }
    }


    private void InterpolateFromLatestAction()
    {

        if (!interpolationMove)
        {
            if (LastSavedAction.sequence_number != -1)
            {
                if (LastAction.sequence_number == -1)
                {
                    LastAction = LastSavedAction;
                }
                else
                {

                    if (LastSavedAction.shooting)
                    {
                        Shooting();
                    }

                    if (LastSavedAction.animationState == 0 && Vector3.Distance(LastAction.getPostion(), LastSavedAction.getPostion()) < 0.3f)
                    {
                        Debug.Log("stand after that #########################################################");
                        transform.position = LastSavedAction.getPostion();
                        Stand();
                    }
                    else
                    {

                        if (LastAction.sequence_number != LastSavedAction.sequence_number && Vector3.Distance(LastAction.getPostion(), LastSavedAction.getPostion()) > 0.3f)
                        {
                            LastAction.setPostion(transform.position);
                            NewAction = LastSavedAction;
                            Debug.Log("getting action  ");
                            interpolationMove = true;
                            MyLocalTime = Time.time;
                            journeyLength = Vector3.Distance(LastAction.getPostion(), NewAction.getPostion());
                            distCovered = journeyLength * 0.25f;
                        }
                        else if (!LastSavedAction.shooting) //we don't predict shooting here
                        {
                            //extrapolate
                            if (LastAction.animationState != 0) //we stand
                            {
                                if (botColor == BotColor.Red)
                                {
                                    Debug.Log(" extrapolate according to " + LastAction.getInput());

                                }
                                    MoveFromInput(LastAction.getInput());
                            }
                            else
                            {
                                Stand();
                            }
                        }
                    }
                }
            }
        }

        if (interpolationMove)
        {
            LastAction.animationState = NewAction.animationState;
            LerpMovement(LastAction, NewAction.getPostion());
            float distance = Mathf.Abs(Vector3.Distance(transform.position, NewAction.getPostion()));
            if (distance < 0.01f)
            {
                interpolationMove = false;
                LastAction = NewAction;
                if(LastAction.animationState==0)
                {
                    Debug.Log("stand after that !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    Stand();
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
        }
        else
        {

            if (botColor == BotColor.Red || botColor == BotColor.Target)
            {
                recivedAction = (Action)stream.ReceiveNext();

                if (queueActive)
                {

            
                    if (to_send != null)
                    {
                        if (to_send.Count > 2)
                        {
                            float dis = Vector3.Distance(LastSavedAction.getPostion(), recivedAction.getPostion());

                            // Debug.Log(" beetween packet distance " + dis);

                            if (dis > 0.3f || LastSavedAction.shooting != recivedAction.shooting)
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
                else
                {

                    if (LastSavedAction.sequence_number == -1)
                    {
                        LastSavedAction = recivedAction;
                    }
                    else if (Vector3.Distance(LastSavedAction.getPostion(), recivedAction.getPostion()) > 0.3f)
                    {
                        LastSavedAction = recivedAction;
                    }
                    else if (recivedAction.shooting || recivedAction.animationState == 0)
                    {
                        LastSavedAction = recivedAction;
                    }
                }
            }
        }
    }


    public void OnEvent(EventData photonEvent)
    {

        if (photonEvent.Code == 1)
        {
            if (botColor == BotColor.Red && photonView.IsMine)
            {
                Action action = (Action)photonEvent.CustomData;
                MoveFromAction(action);
                SaveCurrentStateToFile();
                Action saveAction = new Action(currentAction.postionX, currentAction.postionY, currentAction.postionZ, currentAction.inputForward,currentAction.inputSide, action.sequence_number, action.timestamp, currentAction.animationState, currentAction.rotationAngle, currentAction.shooting);
                to_send.Enqueue(saveAction); 


                byte evCode = 1;
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                SendOptions sendOptions = new SendOptions { Reliability = true };
                PhotonNetwork.RaiseEvent(evCode, currentAction, raiseEventOptions, sendOptions);
            }
            else if (botColor == BotColor.Green && photonView.IsMine)
            {
                Action re_action = (Action)photonEvent.CustomData;
                Recouncil(re_action);
            }

        }

        if (photonEvent.Code == 2)
        {
            if (botColor == BotColor.Target && photonView.IsMine)
            {
                Action re_action = (Action)photonEvent.CustomData;
                Recouncil(re_action);
            }
        }

        if (photonEvent.Code == 3)
        {
            if (botColor == BotColor.Target && photonView.IsMine)
            {
                Debug.Log("GOT hit " + photonView.IsMine);
                Action hitData = (Action)photonEvent.CustomData;

                checkShot(hitData);
            }
            else if (botColor == BotColor.Red && photonView.IsMine)
            {
                Action hitData = (Action)photonEvent.CustomData;
                checkShot(hitData);
            }
        }
    }

    private void checkShot(Action hitData)
    {
        for (int i=history.Count-1; i>=0;i--)
        {

            if( (history[i].timestamp- hitData.timestamp)<0.04 )
            {
                Debug.Log(botColor+" shot found " + i + " t1 " + history[i].timestamp + " t2 " + hitData.timestamp + " pos " + "hit postion" + hitData.getPostion() + " real postion " + history[i].getPostion());
                break;
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
