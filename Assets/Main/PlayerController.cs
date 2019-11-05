using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    float speed = 4;
    float rotationSpeed = 80;
    float rotation = 0;
    float gravity = 9;

    Vector3 moveDir = Vector3.zero;

    CharacterController controller;
    Animator animat;
    PhotonView photonView;

    [SerializeField]
    private Camera playerCamera;
    [SerializeField]
    private AudioListener playerAudio;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animat = GetComponent<Animator>();
        photonView = GetComponent<PhotonView>();

        if (photonView.IsMine)
        {

        }
        else
        {
            // playerCamera.SetActive(false);
            Destroy(playerCamera);
            Destroy(playerAudio);
        }

    }

   
    void Update()
    {
        if (photonView.IsMine)
        {
            Movment();
        }
    }

    private void Movment()
    {
        if (controller.isGrounded)
        {
            if (Input.GetKey(KeyCode.W))
            {
                animat.SetInteger("condition", 1);
                moveDir = new Vector3(0, 0, 1);
                moveDir *= speed;
                moveDir = transform.TransformDirection(moveDir);

            }
            if (Input.GetKeyUp(KeyCode.W))
            {
                animat.SetInteger("condition", 0);
                moveDir = new Vector3(0, 0, 0);
            }
            if (Input.GetKey(KeyCode.S))
            {
                animat.SetInteger("condition", 2);
                moveDir = new Vector3(0, 0, -1);
                moveDir *= speed;
                moveDir = transform.TransformDirection(moveDir);

            }
            if (Input.GetKeyUp(KeyCode.S))
            {
                animat.SetInteger("condition", 0);
                moveDir = new Vector3(0, 0, 0);
            }
        }
        rotation += Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;
        transform.eulerAngles = new Vector3(0, rotation, 0);

        moveDir.y -= gravity * Time.deltaTime;
        controller.Move(moveDir * Time.deltaTime);
    }
}
