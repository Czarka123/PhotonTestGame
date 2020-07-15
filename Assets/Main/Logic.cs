using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Logic : MonoBehaviour
{
    //public struct Inputs
    //{
    //    public bool up;
    //    public bool down;
    //    public bool right;
    //    public bool left;
    //    public bool jump;
    //}


    CharacterController controller;
    Animator animat;
    PhotonView photonView;
    public Transform rayOrgin;
    [SerializeField]
    private Camera playerCamera;
    [SerializeField]
    private AudioListener playerAudio;


    [SerializeField]
    private Rigidbody cube_rigidbody;
    public float speed = 0.1f;
    public float player_jump_y_threshold = 5.0f;

    public GameObject player;
    private float timer;
    private float MyLocalTime;

    private void Start()
    {
        this.timer = 0.0f;
        MyLocalTime = Time.time;
      
        controller = GetComponent<CharacterController>();
        animat = GetComponent<Animator>();
        photonView = GetComponent<PhotonView>();
        Destroy(playerCamera);
        Destroy(playerAudio);

        controller.detectCollisions = false;

       // BotState = BotState.Standing;

       // StartTime = PhotonNetwork.Time;
        MyLocalTime = Time.time;
    }

    private Inputs MovmentOrder()
    {
        Debug.Log(MyLocalTime+" simulate " + Time.time);
        if (MyLocalTime + 2 > Time.time)
        {
            Debug.Log("ups");
            Inputs inputs= new Inputs();
            inputs.up = true;
            return inputs;
        }
        else if (MyLocalTime + 3 > Time.time && MyLocalTime + 2 < Time.time)
        {
            Debug.Log("lefts");
            Inputs inputs = new Inputs();
            inputs.left = true;
            return inputs;
        }
 
        return new Inputs();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        while (timer >= Time.fixedDeltaTime)
        {
            timer -= Time.fixedDeltaTime;

            AddForcesToPlayer(MovmentOrder());

            Debug.Log("simulate");
            Physics.Simulate(Time.fixedDeltaTime);
        }

    }

    private void AddForcesToPlayer(Inputs inputs)
    {

        if (inputs.up)
        {
            cube_rigidbody.AddForce(new Vector3(0, 0, speed), ForceMode.Impulse);
        }

        if (inputs.down)
        {
            cube_rigidbody.AddForce(new Vector3(0, 0, -speed), ForceMode.Impulse);
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
}
