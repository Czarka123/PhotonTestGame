using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;


public class CubeLogic : MonoBehaviour, IPunObservable, IOnEventCallback
{
    [SerializeField]
    private Rigidbody cube_rigidbody;
    public float speed = 0.1f;
    public float player_jump_y_threshold =5.0f;
    public bool isGreen;
    public bool InterpolationOn=true;

    Inputs currentInput;
    StateMessage currentState;
    StateMessage goalState;
    StateMessage LastState;
    StateMessage LastSavedState;

    List<StateMessage> history;
    Queue<StateMessage> newStateMessages;

    Queue<StateMessage> InterpolationBuffer;

    public uint tick_number;

    public GameObject player;
    PhotonView photonView;


    private float timer;
    private float journeyLength;

    private float MyLocalTime;
    private bool interpolationg = false;

    private Vector3 client_pos_error;
    private Quaternion client_rot_error;

    float mycountertest = 0;

    private void Start()
    {
        this.timer = 0.0f;
        MyLocalTime = Time.time;
        photonView = GetComponent<PhotonView>();
        history = new List<StateMessage>();
        newStateMessages = new Queue<StateMessage>();
        InterpolationBuffer = new Queue<StateMessage>();
        LastSavedState = new StateMessage();
        LastSavedState.tick_number = 0; //for checking first send
        LastState.tick_number = 0;
        // PhotonPeer.RegisterType(typeof(Inputs), 2, Inputs.Serialize, Inputs.Deserialize);
        //PhotonPeer.RegisterType(typeof(InputMessage), 2, InputMessage.Serialize, InputMessage.Deserialize);
        PhotonPeer.RegisterType(typeof(StateMessage), 2, StateMessage.Serialize, StateMessage.Deserialize); //droższe rozwiazanie 
        //PhotonNetwork.sendRateOnSerialize      
        PhotonNetwork.SendRate = 50;
        PhotonNetwork.SerializationRate = 50;
        Debug.Log("SR " + PhotonNetwork.SendRate + " SS " + PhotonNetwork.SerializationRate);
    }

    void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            if (history != null && newStateMessages != null)
            {
                if (isGreen)
                {
                    //mycountertest++;
                    //if (mycountertest <= 10)
                    //{
                    //    Debug.Log(" hermite interpolation from" + Vector3.one + " to " + new Vector3(3f, 2f, 2f) +" counter "+ mycountertest + " value : " + (mycountertest / 10));
                    //    Debug.Log(" HERMITE interpolation : " + Hermite(Vector3.one, new Vector3(3f, 2f, 2f), (mycountertest / 10)));
                    //}

                    SimulateFromInput(MovmentOrderv2());
                    byte evCode = 1;
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                    SendOptions sendOptions = new SendOptions { Reliability = true };
                    PhotonNetwork.RaiseEvent(evCode, currentState, raiseEventOptions, sendOptions);
                    RewindState();
                }
            }
           // tick_number++;
        }
        else
        {
            Read();
        }       
    }

    void Read()
    {
        if (InterpolationOn)
        {
            SetInterpolation();

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
            // journeyLength = Vector3.Distance(LastState.getPosition(), currentState.getPosition());

            // Debug.Log("Journey distance from " + LastState.getPosition() + " to " + currentState.getPosition());
        }

        if (interpolationg)
        {
            SimulateFromStateInterpolationHermite(LastState, goalState);
            float distance = Mathf.Abs(Vector3.Distance(cube_rigidbody.position, goalState.getPosition()));  //transform for linear
            Debug.Log("distance interpolation " + distance);
            if (distance < 0.03f) //was  0.01f
            {
                interpolationg = false;
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
             
                position_error = state_msg.getPosition() - history[history.Count - 1].getPosition();
                float rotation_error = 1.0f - Quaternion.Dot(state_msg.getRotation(), history[history.Count - 1].getRotation());


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

                    int rewind_tick_number = (int)state_msg.tick_number;
                    //int currentTickNumber = (int)tick_number;
                    //  Debug.Log("to rewind " + rewind_tick_number +" < " + tick_number);
                    while (rewind_tick_number < tick_number)
                    {


                        //   Debug.Log("rewinding " + history[rewind_tick_number].tick_number);
                        history[rewind_tick_number] = state_msg;
                        AddForcesToPlayer(state_msg.getInput());
                        Physics.Simulate(Time.fixedDeltaTime);
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
        currentInput =input;
        this.timer += Time.deltaTime;
        while (this.timer >= Time.fixedDeltaTime)
        {
            this.timer -= Time.fixedDeltaTime;

          
            this.AddForcesToPlayer(currentInput);
            Physics.Simulate(Time.fixedDeltaTime);
            currentState = GetState(currentInput);
            history.Add(currentState);
            //currentState.tick_number = tick_number;
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
           // currentState = GetState(currentInput);
           // history.Add(currentState);
            //currentState.tick_number = tick_number;
            tick_number++;
        }
    }

    private void SimulateFromStateInterpolationLinear(StateMessage LastStateMsg, StateMessage CurrentStateMsg)
    {



        transform.rotation = Quaternion.Lerp(LastStateMsg.getRotation(), CurrentStateMsg.getRotation(), Time.time * 6);

        //Debug.Log(" packet to interpolate " + journeyLength);

        //Debug.Log(" packet to interpolate " + CurrentStateMsg.tick_number + " and time " + CurrentStateMsg.delivery_time + " postion: " + CurrentStateMsg.getPosition() + " buffor size " + InterpolationBuffer.Count);
        float distCovered = (Time.time - MyLocalTime) * 6;
        float fractionOfJourney = distCovered / journeyLength;
        transform.position = Vector3.Lerp(LastStateMsg.getPosition(), CurrentStateMsg.getPosition(), fractionOfJourney);

    }

    private void SimulateFromStateInterpolationHermite(StateMessage LastStateMsg, StateMessage CurrentStateMsg)
    {
        //Debug.Log("hermite ~!");


        //transform.rotation = Quaternion.Slerp(LastStateMsg.getRotation(), CurrentStateMsg.getRotation(), Time.time * 6);

        float speedof = 6f;
        journeyLength = Vector3.Distance(LastStateMsg.getPosition(), CurrentStateMsg.getPosition());
      //  Debug.Log("frome: "+ LastStateMsg.getPosition()+ "  to : "+ CurrentStateMsg.getPosition()+" distance " + journeyLength);

        //Debug.Log(" packet to interpolate " + CurrentStateMsg.tick_number + " and time " + CurrentStateMsg.delivery_time + " postion: " + CurrentStateMsg.getPosition() + " buffor size " + InterpolationBuffer.Count);
        float distCovered = Vector3.Distance(LastStateMsg.getPosition(), cube_rigidbody.position);
        float fractionOfJourney = (distCovered / journeyLength);
        if((fractionOfJourney + 0.1f)<1)
        {
            //Debug.Log(" fract of journey " + fractionOfJourney);
            fractionOfJourney += 0.1f;
            Vector3 hermite = Hermite(LastStateMsg.getPosition(), CurrentStateMsg.getPosition(), fractionOfJourney);
           // Debug.Log("my postion " + transform.position + " hermite postion " + hermite + " goal " + CurrentStateMsg.getPosition() + " dist coverd " + distCovered + " fract of journey " + fractionOfJourney);
            cube_rigidbody.position = hermite;
        }
        else
        {
            fractionOfJourney = 1f;
          //  Debug.Log("my postion " + transform.position + " hermite postion " + CurrentStateMsg.getPosition() + " goal " + CurrentStateMsg.getPosition() + " dist coverd " + distCovered + " fract of journey " + fractionOfJourney);
            cube_rigidbody.position = CurrentStateMsg.getPosition();
        }
      
        //transform.position = hermite;
     
        cube_rigidbody.rotation = Quaternion.Slerp(LastStateMsg.getRotation(), CurrentStateMsg.getRotation(), Time.time * speedof);
        //cube_rigidbody.velocity = Hermite(LastStateMsg.getVelocity(), CurrentStateMsg.getVelocity(), fractionOfJourney);
        cube_rigidbody.velocity = CurrentStateMsg.getVelocity();
        cube_rigidbody.angularVelocity = Vector3.Slerp(LastStateMsg.getVelocity_Angular(), CurrentStateMsg.getVelocity_Angular(), Time.time * speedof);
    }

    public static float Hermite(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, value * value * (3.0f - 2.0f * value));
    }

    public static Vector3 Hermite(Vector3 start, Vector3 end, float value)
    {
        return new Vector3(Hermite(start.x, end.x, value), Hermite(start.y, end.y, value), Hermite(start.z, end.z, value));
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

    private Inputs MovmentOrderv2()
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
        else if (MyLocalTime + 3 > Time.time && MyLocalTime + 2 < Time.time)
        {
            Debug.Log("right");
            Inputs inputs = new Inputs();
            inputs.right = true;

            return inputs;
        }
        else if (MyLocalTime + 3.5 > Time.time && MyLocalTime + 3 < Time.time)
        {
            Debug.Log("right");
            Inputs inputs = new Inputs();
            inputs.jump = true;

            return inputs;
        }
        else if (MyLocalTime + 4 > Time.time && MyLocalTime + 3.5 < Time.time)
        {
            Debug.Log("ups");
            Inputs inputs = new Inputs();
            inputs.up = true;

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
           
            Inputs inputs = new Inputs();
            inputs.left = true;
            inputs.up = true;

            return inputs;
        }
        else if (MyLocalTime + 6 > Time.time && MyLocalTime + 5 < Time.time)
        {

            Inputs inputs = new Inputs();
            inputs.down = true;
         

            return inputs;
        }
        else if (MyLocalTime + 6.5 > Time.time && MyLocalTime + 6 < Time.time)
        {

            Inputs inputs = new Inputs();
            inputs.left = true;
            inputs.jump = true;

            return inputs;
        }
        else if (MyLocalTime + 7 > Time.time && MyLocalTime + 6.5 < Time.time)
        {

            Inputs inputs = new Inputs();          
            inputs.up = true;

            return inputs;
        }
        else if (MyLocalTime + 8 > Time.time && MyLocalTime + 7.5 < Time.time)
        {

            Inputs inputs = new Inputs();
            inputs.right = true;

            return inputs;
        }
        else if (MyLocalTime + 9 > Time.time && MyLocalTime + 8 < Time.time)
        {

            Inputs inputs = new Inputs();
            inputs.down = true;

            return inputs;
        }
        else if (MyLocalTime + 9.5 > Time.time && MyLocalTime + 9 < Time.time)
        {

            Inputs inputs = new Inputs();
            inputs.left = true;
            inputs.jump = true;

            return inputs;
        }
        else if (MyLocalTime + 11 > Time.time && MyLocalTime + 9.5 < Time.time)
        {

            Inputs inputs = new Inputs();
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
            //currentState.tick_number = tick_number;
            //history.Add(currentState);
            //tick_number++;
           // Debug.Log(name+" writing" + currentState.tick_number);
            stream.SendNext(currentState);
            //stream.SendNext(cube_rigidbody.position);
            //stream.SendNext(cube_rigidbody.rotation);
            //stream.SendNext(cube_rigidbody.velocity);
            //stream.SendNext(cube_rigidbody.angularVelocity);
            //stream.SendNext(new InputMessage(tick_number, currentInput.up, currentInput.down, currentInput.right, currentInput.left, currentInput.jump));


        }
        else
        {
            //Vector3 newPos = (Vector3)stream.ReceiveNext();
            //Quaternion newRot = (Quaternion)stream.ReceiveNext();
            //Vector3 newVel = (Vector3)stream.ReceiveNext();
            //Vector3 newAngVel = (Vector3)stream.ReceiveNext();

            StateMessage recivepack = (StateMessage)stream.ReceiveNext();
           // Debug.Log(name+ "reading " + recivepack.tick_number);
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
                     
                        if (dis>0.7f || angDis>25)  //0.2 10
                        {
                            Debug.Log("distance " + dis + " angle dis " + angDis);
                            LastSavedState = recivepack;
                            InterpolationBuffer.Enqueue(recivepack);
                        }
                       
                    }
                 
                }
            }
            //history.Add(recivepack);

            // Debug.Log("name "+ name+" got tick" + recivepack.start_tick_number + "   up " + recivepack.input.up + "   down " + recivepack.input.down + "   right " + recivepack.input.right + "   left " + recivepack.input.left);
            // tick_number = recivepack.start_tick_number;
            //Debug.Log(" got tick" + recivepack.start_tick_number + "   up " + recivepack.up + "   down " + recivepack.down + "   right " + recivepack.right + "   left " + recivepack.left);

        }
    }



    public void OnEvent(EventData photonEvent)
    {
       if(photonEvent.Code==1)
        {
            if (!isGreen && photonView.IsMine)
            {
                StateMessage state = (StateMessage)photonEvent.CustomData;
            //    Debug.Log("EVENT RECIVED  "+state.tick_number);
                SimulateFromInput(state.getInput());
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
                    //Debug.Log("Got news back RECIVED  " + state.tick_number);
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
