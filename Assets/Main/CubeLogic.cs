using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;



public class CubeLogic : MonoBehaviour, IPunObservable, IOnEventCallback
{

    [SerializeField]
    private Rigidbody cube_rigidbody;
    public float speed = 0.1f;
    public float player_jump_y_threshold =5.0f;
    public bool isGreen;
    public bool InterpolationOn=true;
    public bool SmoothCorrection;

    Inputs currentInput;
    StateMessage currentState;
    StateMessage goalState;
    StateMessage LastState;
    StateMessage LastSavedState;

    //hermite 4
    StateMessage SM1;
    StateMessage SM2;
    StateMessage SM3;
    StateMessage SM4;

    List<StateMessage> history;
    Queue<StateMessage> unConfirmedPredictions;
    Queue<StateMessage> newStateMessages;
    Queue<StateMessage> InterpolationBuffer;

    public uint tick_number;

    public GameObject player;
    PhotonView photonView;


    private float timer;
    private float journeyLength;

    private float MyLocalTime;
    private bool interpolationg = false;
    private bool correcting = false;

    bool bufferpurge = true;

    private Vector3 client_pos_error;
    private Quaternion client_rot_error;

    float mycountertest = 0;
    StringBuilder logfile;
    int SaveCounter = 0;

    void SaveCurrentStateToFile()
    {

        var newLine = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}", SaveCounter, transform.position.x, transform.position.y, transform.position.z, Mathf.Ceil(transform.rotation.eulerAngles.x), Mathf.Ceil(transform.rotation.eulerAngles.y), Mathf.Ceil(transform.rotation.eulerAngles.z), PhotonNetwork.Time);
        logfile.AppendLine(newLine);
        //toSend.Enqueue(newCmd);
    }


    private void Start()
    {
        this.timer = 0.0f;
        MyLocalTime = Time.time;
        photonView = GetComponent<PhotonView>();
        history = new List<StateMessage>();
        newStateMessages = new Queue<StateMessage>();
        InterpolationBuffer = new Queue<StateMessage>();
        unConfirmedPredictions = new Queue<StateMessage>();
        LastSavedState = new StateMessage();
        LastSavedState.tick_number = 0; //for checking first send
        LastState.tick_number = 0;

        PhotonPeer.RegisterType(typeof(StateMessage), 2, StateMessage.Serialize, StateMessage.Deserialize);  

        logfile = new StringBuilder();
        var newLine = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}", "Index", "PostionX", "PostionY", "PostionZ", "RotationAngleX", "RotationAngleY", "RotationAngleZ", "PhotonTime");
        logfile.AppendLine(newLine);

        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 60;
        Debug.Log("SR " + PhotonNetwork.SendRate + " SS " + PhotonNetwork.SerializationRate);
    }

    void FixedUpdate()
    {
        SaveCounter++;
        if (photonView.IsMine)
        {
            if (history != null && newStateMessages != null)
            {
                if (isGreen)
                {
                    SimulateFromInput(MovementTestSequnce());
                    
                    byte evCode = 1;
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                    SendOptions sendOptions = new SendOptions { Reliability = true };
                    PhotonNetwork.RaiseEvent(evCode, currentState, raiseEventOptions, sendOptions);
                    if (SmoothCorrection)
                    {
                        PredictCorrection();
                    }
                    else
                    {
                        RewindState();
                    }

                    SaveCurrentStateToFile(); 
                }
            }
        }
        else
        {
            Read();
            SaveCurrentStateToFile();
        }

        if (SaveCounter == 900)
        {
            string filename = "";
            if (photonView.IsMine && isGreen)
            {
                filename = "CubeGreen";

            }
            else if (photonView.IsMine &&!isGreen)
            {
                filename = "CubeRedServer";
            }
            else if (!photonView.IsMine && !isGreen)
            {
                filename = "CubeRedProxy";
            }

            File.WriteAllText(@"path" + filename + ".csv", logfile.ToString());

            Debug.Log(" Log Saved in file ");
        }
    }

    void Read()
    {
        if (InterpolationOn)
        {
            SetInterpolation();
            //SimulateInterpolationHermite4(0.12f);
        }
        else
        {
            if (history != null && newStateMessages != null && newStateMessages.Count > 0)
            {
                StateMessage state_msg = newStateMessages.Dequeue();
                currentState = state_msg;
                SimulateFromState(state_msg);

            }
            else
            {
                SimulateFromState(currentState);

            }
        }
    }

    void SetInterpolation()
    {
        if (InterpolationBuffer != null && InterpolationBuffer.Count > 1 && !interpolationg)
        {
            Debug.Log("interpolation");
            interpolationg = true;
            if (LastState.tick_number == 0)
            {
                LastState = InterpolationBuffer.Dequeue();
            }
            else
            {
                LastState = goalState;
            }

            goalState = InterpolationBuffer.Dequeue();
            MyLocalTime = Time.time;

        }

        if (interpolationg)
        {
            SimulateFromStateInterpolationHermite(LastState, goalState);
            float distance = Mathf.Abs(Vector3.Distance(cube_rigidbody.position, goalState.getPosition()));  //transform for linear
            //Debug.Log("distance interpolation " + distance);
            if (distance < 0.03f) //was  0.01f
            {
                interpolationg = false;
            }
        }
    }

    private void PredictCorrection()
    {
        if (newStateMessages.Count > 0)
        {

            StateMessage server_state = newStateMessages.Dequeue();
            StateMessage client_state = unConfirmedPredictions.Dequeue();

            if (bufferpurge)
            {
                if (client_state.tick_number != server_state.tick_number || newStateMessages.Count > 5)
                {
                    float skipped_packs = 0;

                    if (client_state.tick_number == server_state.tick_number)
                    {

                        Debug.Log("cool");
                    }


                    if (newStateMessages.Count > 5)  //purging buffer
                    {

                        while (newStateMessages.Count > 5)
                        {
                            server_state = newStateMessages.Dequeue();
                        }

                        while (unConfirmedPredictions.Count > 5)
                        {
                            history.Add(client_state);
                            client_state = unConfirmedPredictions.Dequeue();
                        }

                        for (int i = 0; i < unConfirmedPredictions.Count; i++)
                        {
                            if (client_state.tick_number <= server_state.tick_number)
                            {
                                Debug.Log("found sam number");
                                break;
                            }
                            history.Add(client_state);
                            client_state = unConfirmedPredictions.Dequeue();

                        }
                    }
                }
            }

            float distance = Vector3.Distance(server_state.getPosition(), client_state.getPosition());
            if (distance > 0.25f)
            {

                client_rot_error = Quaternion.Inverse(client_state.getRotation()) * server_state.getRotation();
                client_pos_error = server_state.getPosition() - client_state.getPosition();

                float lerpPercent = 0.1f;

                if (!bufferpurge)
                {
                    lerpPercent = unConfirmedPredictions.Count / 5;
                    if (lerpPercent > 1)
                    {
                        lerpPercent = Mathf.Ceil(lerpPercent);
                        lerpPercent = 0.1f / lerpPercent;
                    }
                    else
                    {
                        lerpPercent = 0.1f;
                    }
                }

                Quaternion rotation_correction = Quaternion.Slerp(Quaternion.identity, client_rot_error, lerpPercent);
                Vector3 correction = Vector3.Lerp(Vector3.zero, client_pos_error, lerpPercent);

                cube_rigidbody.position += correction;
                cube_rigidbody.rotation *= rotation_correction;
                cube_rigidbody.velocity = server_state.getVelocity();
                cube_rigidbody.angularVelocity = server_state.getVelocity_Angular();
            }
            else
            {

                //snap
                cube_rigidbody.position = server_state.getPosition();
                cube_rigidbody.rotation = server_state.getRotation();



                history.Add(client_state); //correcto
            }

        }
    }

    void RewindState()
    {
       
        if (newStateMessages.Count > 0)
        {
            while (newStateMessages.Count != 0)
            {
               
                StateMessage state_msg = newStateMessages.Dequeue();

                //uint buffer_slot = state_msg.tick_number % (uint)history.Count;
                Vector3 position_error = new Vector3();
             
                position_error = state_msg.getPosition() - unConfirmedPredictions.Peek().getPosition();
                float rotation_error = 1.0f - Quaternion.Dot(state_msg.getRotation(), unConfirmedPredictions.Peek().getRotation());


                if (position_error.sqrMagnitude > 0.0000001f || rotation_error > 0.00001f)
                {

                    // capture the current predicted pos for smoothing
                    Vector3 prev_pos = cube_rigidbody.position + this.client_pos_error;
                    Quaternion prev_rot = cube_rigidbody.rotation * this.client_rot_error;

                    // rewind & replay
                    Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! rewind");
                    cube_rigidbody.position = state_msg.getPosition();
                    cube_rigidbody.rotation = state_msg.getRotation();
                    cube_rigidbody.velocity = state_msg.getVelocity();
                    cube_rigidbody.angularVelocity = state_msg.getVelocity_Angular();

                    int rewind_tick_number =(int)state_msg.tick_number;

                    while (rewind_tick_number < tick_number)
                    {


                        StateMessage replay = unConfirmedPredictions.Dequeue();

                        AddForcesToPlayer(replay.getInput());  //for smoothig not applay dirrectly
                        Physics.Simulate(Time.fixedDeltaTime);
                        if (rewind_tick_number != state_msg.tick_number)
                        {
                            replay.setPosition(transform.position);
                            replay.setRotation(transform.rotation);
                            history.Add(replay);
                            unConfirmedPredictions.Enqueue(replay);
                        }

                        ++rewind_tick_number;
                    }

                    // if more than 2ms apart, just snap
                    if ((prev_pos - cube_rigidbody.position).sqrMagnitude >= 4.0f)
                    {
                        this.client_pos_error = Vector3.zero;
                        this.client_rot_error = Quaternion.identity;
                    }
                    else
                    {
                        this.client_pos_error = prev_pos - cube_rigidbody.position;
                        this.client_rot_error = Quaternion.Inverse(cube_rigidbody.rotation) * prev_rot;
                    }

                   
                }
            }
        }
    }


    private void SimulateFromInput(Inputs input) //spróbuj zrobić dla state
    {
        currentInput = input;
        this.timer += Time.deltaTime;
        while (this.timer >= Time.fixedDeltaTime)
        {
            this.timer -= Time.fixedDeltaTime;

            this.AddForcesToPlayer(currentInput);
            Physics.Simulate(Time.fixedDeltaTime);
            currentState = GetState(currentInput);

            unConfirmedPredictions.Enqueue(currentState);
            tick_number++;
        }
    }

    private void SimulateFromState(StateMessage stateMessage) //spróbuj zrobić dla state
    {
        cube_rigidbody.position = stateMessage.getPosition();
        cube_rigidbody.rotation = stateMessage.getRotation();
        cube_rigidbody.velocity = stateMessage.getVelocity();
        cube_rigidbody.angularVelocity = stateMessage.getVelocity_Angular();

        this.timer += Time.deltaTime;
        while (this.timer >= Time.fixedDeltaTime)
        {
            this.timer -= Time.fixedDeltaTime;


            this.AddForcesToPlayer(currentInput);
            Physics.Simulate(Time.fixedDeltaTime);

            tick_number++;
        }
    }

    private void SimulateFromStateInterpolationHermite(StateMessage LastStateMsg, StateMessage CurrentStateMsg)
    {
        //Debug.Log("hermite ~!");


        float speedof = 6f;
        journeyLength = Vector3.Distance(LastStateMsg.getPosition(), CurrentStateMsg.getPosition());
    
        float distCovered = Vector3.Distance(LastStateMsg.getPosition(), cube_rigidbody.position);
        float fractionOfJourney = (distCovered / journeyLength);
        if((fractionOfJourney + 0.1f)<1)
        {
            //Debug.Log(" fract of journey " + fractionOfJourney);
            fractionOfJourney += 0.1f;
            Vector3 hermite = Hermite(LastStateMsg.getPosition(), CurrentStateMsg.getPosition(), fractionOfJourney);
            Debug.Log("my postion " + transform.position + " hermite postion " + hermite + " goal " + CurrentStateMsg.getPosition() + " dist coverd " + distCovered + " fract of journey " + fractionOfJourney);
            cube_rigidbody.position = hermite;
        }
        else
        {
            fractionOfJourney = 1f;
            Debug.Log("my postion " + transform.position + " hermite postion " + CurrentStateMsg.getPosition() + " goal " + CurrentStateMsg.getPosition() + " dist coverd " + distCovered + " fract of journey " + fractionOfJourney);
            cube_rigidbody.position = CurrentStateMsg.getPosition();
        }
      
        //transform.position = hermite;
     
        cube_rigidbody.rotation = Quaternion.Slerp(LastStateMsg.getRotation(), CurrentStateMsg.getRotation(), Time.time * speedof);
        //cube_rigidbody.velocity = Hermite(LastStateMsg.getVelocity(), CurrentStateMsg.getVelocity(), fractionOfJourney);
        cube_rigidbody.velocity = CurrentStateMsg.getVelocity();
        cube_rigidbody.angularVelocity = CurrentStateMsg.getVelocity_Angular();
    }

    private void SimulateInterpolationHermite4(float interpolationFraction)
    {
        //
        if (InterpolationBuffer.Count >= 3 && !interpolationg)
        {
            SM1.setPosition(cube_rigidbody.position);
            SM1.setRotation(cube_rigidbody.rotation);
            SM1.setVelocity_Ang(cube_rigidbody.angularVelocity);
            SM2 = InterpolationBuffer.Dequeue();
            SM3 = InterpolationBuffer.Dequeue();
            SM4 = InterpolationBuffer.Dequeue();

            journeyLength = interpolationFraction;
            Debug.Log("Interpolation starts sm1 " + SM1.getPosition() + "sm2 " + SM2.getPosition() + "sm3 " + SM3.getPosition() + "sm4 " + SM4.getPosition() + "buffer size "+ InterpolationBuffer.Count);
            interpolationg = true;
        }

        if(interpolationg)
        {

           Vector3 IntPos = Hermite4(SM1.getPosition(), SM2.getPosition(), SM3.getPosition(), SM4.getPosition(), journeyLength);
            //Debug.Log("Interpolation in sm1 " + SM1.getPosition() + "sm2 " + SM2.getPosition() + "sm3 " + SM3.getPosition() + "sm4 " + SM4.getPosition());
            Debug.Log("my postion " + cube_rigidbody.position + " hermite postion " + IntPos + " goal " + SM4.getPosition() +" fract of journey " + journeyLength);
            cube_rigidbody.position = IntPos;
           

            if (journeyLength >= 1f)
            {
                interpolationg = false;
            }
            else if (journeyLength >= 0.66f)
            {
                cube_rigidbody.rotation = Quaternion.Slerp(SM3.getRotation(), SM4.getRotation(), ((journeyLength - 0.66f) / 0.33f));
                cube_rigidbody.velocity = SM3.getVelocity();
                cube_rigidbody.angularVelocity = SM3.getVelocity_Angular();
            }
            else if (journeyLength >= 0.33f)
            {
                cube_rigidbody.rotation = Quaternion.Slerp(SM2.getRotation(), SM3.getRotation(), ((journeyLength-0.33f) / 0.33f));
                cube_rigidbody.velocity = SM2.getVelocity();
                cube_rigidbody.angularVelocity = SM2.getVelocity_Angular();
            }
            else if (journeyLength >= 0.0f)
            {
                cube_rigidbody.rotation = Quaternion.Slerp(SM1.getRotation(), SM2.getRotation(), (journeyLength/ 0.33f));
                cube_rigidbody.velocity = SM1.getVelocity();
                cube_rigidbody.angularVelocity = SM1.getVelocity_Angular();
            }
            journeyLength += interpolationFraction;
        }
    }

    public static float Hermite(float start, float end, float value)
    {

        return Mathf.Lerp(start, end, value * value * (3.0f - 2.0f * value));
    }

    public static Vector3 Hermite(Vector3 start, Vector3 end, float value)
    {
        return new Vector3(Hermite(start.x, end.x, value), Hermite(start.y, end.y, value), Hermite(start.z, end.z, value));
    }


    float HermiteInterpolate4( float y0, float y1, float y2, float y3, float mu)
    {
        float m0, m1, mu2, mu3;
        float a0, a1, a2, a3;

        float tension = 0.2f;
        float bias = 0.2f;

        mu2 = mu * mu;
        mu3 = mu2 * mu;
        m0 = (y1 - y0) * (1 + bias) * (1 - tension) / 2;
        m0 += (y2 - y1) * (1 - bias) * (1 - tension) / 2;
        m1 = (y2 - y1) * (1 + bias) * (1 - tension) / 2;
        m1 += (y3 - y2) * (1 - bias) * (1 - tension) / 2;
        a0 = 2 * mu3 - 3 * mu2 + 1;
        a1 = mu3 - 2 * mu2 + mu;
        a2 = mu3 - mu2;
        a3 = -2 * mu3 + 3 * mu2;

        return (a0 * y1 + a1 * m0 + a2 * m1 + a3 * y2);
    }

    public Vector3 Hermite4(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float value)
    {
        return new Vector3(HermiteInterpolate4(p1.x, p2.x, p3.x, p4.x, value), HermiteInterpolate4(p1.y, p2.y, p3.y, p4.y, value), HermiteInterpolate4(p1.z, p2.z, p3.z, p4.z, value));
    }


    private StateMessage GetState(Inputs input)
    {
        StateMessage state_msg= new StateMessage();
        state_msg.setPosition( cube_rigidbody.position);
        state_msg.setRotation ( cube_rigidbody.rotation);
        state_msg.setVelocity( cube_rigidbody.velocity);
        state_msg.setVelocity_Ang( cube_rigidbody.angularVelocity);
        state_msg.setInput(input);
        state_msg.tick_number = tick_number;
       // Debug.Log("vel " + state_msg.velocity);
        return state_msg;
    }

    private Inputs getInputs()
    {
        Inputs inputs = new Inputs();
        inputs.up = Input.GetKey(KeyCode.W);
        inputs.down = Input.GetKey(KeyCode.S);
        inputs.left = Input.GetKey(KeyCode.A);
        inputs.right = Input.GetKey(KeyCode.D);
        inputs.jump = Input.GetKey(KeyCode.Space);
        return inputs;
    }

    private Inputs MovementTestSequnce()
    {

        double startTime = PhotonNetwork.Time;
        if (MyLocalTime + 3 > Time.time)
        {
            Inputs inputs = new Inputs();
            inputs.up = true;
            return inputs;
        }
        else if (MyLocalTime + 4> Time.time && MyLocalTime + 3 < Time.time)
        {

            Inputs inputs = new Inputs();
            inputs.left = true;
            return inputs;

        }
        else if (MyLocalTime + 5 > Time.time && MyLocalTime +4 < Time.time)
        {

            Inputs inputs = new Inputs();
            inputs.right = true;
            return inputs;
        }
        else if (MyLocalTime + 6 > Time.time && MyLocalTime + 5 < Time.time)
        {

            Inputs inputs = new Inputs();
            inputs.up = true;
            return inputs;

        }
        else if (MyLocalTime + 7 > Time.time && MyLocalTime + 6 < Time.time)
        {

            Inputs inputs = new Inputs();
            inputs.up = true;
            inputs.right = true;
            return inputs;
        }
        else if (MyLocalTime + 8 > Time.time && MyLocalTime + 7 < Time.time)
        {
            Inputs inputs = new Inputs();
            inputs.down = true;
            inputs.right = true;
            return inputs;
        }
        else if (MyLocalTime + 9 > Time.time && MyLocalTime + 8 < Time.time)
        {
            Inputs inputs = new Inputs();
            inputs.down = true;
            inputs.left = true;
            return inputs;
        }
        else if (MyLocalTime + 10 > Time.time && MyLocalTime + 9 < Time.time)
        {
            Inputs inputs = new Inputs();
            inputs.up = true;
            inputs.left = true;
            return inputs;
        }
        else if (MyLocalTime + 11 > Time.time && MyLocalTime + 10 < Time.time)
        {
            Inputs inputs = new Inputs();
            inputs.jump = true;
            inputs.left = true;
            return inputs;
        }

        return new Inputs();
    }


    private Inputs MovmentOrder()
    {      
        if (MyLocalTime + 1 > Time.time)
        {
            Debug.Log("ups");
            Inputs inputs = new Inputs();
            inputs.up = true;
            return inputs;
        }
        else if (MyLocalTime + 1.5 > Time.time && MyLocalTime + 1 < Time.time)
        {
            Debug.Log("lefts");
            Inputs inputs = new Inputs();
            inputs.left = true;
            return inputs;
        }
        else if (MyLocalTime + 2 > Time.time && MyLocalTime + 1.5 < Time.time)
        {
            Debug.Log("jumpy");
            Inputs inputs = new Inputs();
            inputs.left = true;
            inputs.down = true;
            return inputs;
        }
        else if (MyLocalTime + 2.2 > Time.time && MyLocalTime + 2 < Time.time)
        {
            Debug.Log("right");
            Inputs inputs = new Inputs();
            inputs.right = true;
            
            return inputs;
        }
        else if (MyLocalTime + 2.5 > Time.time && MyLocalTime + 2.2 < Time.time)
        {
            Debug.Log("right");
            Inputs inputs = new Inputs();
            inputs.jump = true;

            return inputs;
        }
        else if (MyLocalTime + 4 > Time.time && MyLocalTime + 2.5 < Time.time)
        {
            Debug.Log("right");
            Inputs inputs = new Inputs();
            inputs.right = true;

            return inputs;
        }
        else if (MyLocalTime + 4.5 > Time.time && MyLocalTime + 4 < Time.time)
        {
            Debug.Log("break");
            Inputs inputs = new Inputs();
            

            return inputs;
        }
        else if (MyLocalTime + 5 > Time.time && MyLocalTime + 4.5 < Time.time)
        {
            Debug.Log("break");
            Inputs inputs = new Inputs();
            inputs.left = true;
            inputs.up = true;

            return inputs;
        }

        return new Inputs();
    }


    private void AddForcesToPlayer(Inputs inputs)
    {
       // Debug.Log(name + "  "+inputs.up );
        if (inputs.up)
        {
            cube_rigidbody.AddForce(new Vector3(0,0 , speed),ForceMode.Impulse);
        }

        if (inputs.down)
        {
            cube_rigidbody.AddForce(new Vector3(0,0 , -speed), ForceMode.Impulse);
        }
        if (inputs.left)
        {
            cube_rigidbody.AddForce(new Vector3(-speed, 0, 0), ForceMode.Impulse);
        }
        if (inputs.right)
        {
            cube_rigidbody.AddForce(new Vector3(speed, 0, 0), ForceMode.Impulse);
        }
        if (cube_rigidbody.transform.position.y <= player_jump_y_threshold && inputs.jump)
        {
            cube_rigidbody.AddForce(new Vector3(0, speed, 0), ForceMode.Impulse);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {

            stream.SendNext(currentState);

        }
        else
        {

            StateMessage recivepack = (StateMessage)stream.ReceiveNext();
            if (newStateMessages != null)
            {
                newStateMessages.Enqueue(recivepack);
                if (InterpolationOn)
                {
                    if (LastSavedState.tick_number==0)
                    {
                        LastSavedState = recivepack;
                        InterpolationBuffer.Enqueue(recivepack);
                    }
                    else 
                    {
                        float dis = Vector3.Distance(LastSavedState.getPosition(), recivepack.getPosition());
                        float angDis = Quaternion.Angle(LastSavedState.getRotation(), recivepack.getRotation());
                     
                        if (dis>0.2f || angDis > 10)  //0.2 10
                        {
                            //Debug.Log("distance " + dis + " angle dis " + angDis);
                            LastSavedState = recivepack;
                            InterpolationBuffer.Enqueue(recivepack);
                        }
                       
                    }
                 
                }
            }

        }
    }


    public void OnEvent(EventData photonEvent)
    {
       if(photonEvent.Code==1)
        {
            if (!isGreen && photonView.IsMine)
            {
                StateMessage state = (StateMessage)photonEvent.CustomData;
                SimulateFromInput(state.getInput());
                SaveCurrentStateToFile();
                byte evCode = 1;
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                SendOptions sendOptions = new SendOptions { Reliability = true };
                PhotonNetwork.RaiseEvent(evCode, currentState, raiseEventOptions, sendOptions);
            }
            else if(isGreen && photonView.IsMine)
            {
                StateMessage state = (StateMessage)photonEvent.CustomData;
              
                if (newStateMessages != null)
                {
                    newStateMessages.Enqueue(state);
                }

            }
        }
    }

    public void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}
