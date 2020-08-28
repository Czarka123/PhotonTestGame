using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{

    PhotonView photonView;
    public Transform rayOrgin;
    Animator animat;
    [SerializeField]
    private GameObject LaserEffect;

    private GameObject LaserObject;

    [SerializeField]
    private ParticleSystem FireFlash;

    // Start is called before the first frame update
    void Start()
    {
        photonView = GetComponent<PhotonView>();
        animat = GetComponent<Animator>();
        LaserObject = Instantiate(LaserEffect, rayOrgin);
        LaserObject.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }
        if (Input.GetButtonDown("Fire1"))
        {
            animat.SetInteger("condition", 0);
            photonView.RPC("RPC_Shooting", RpcTarget.All);        
        }
        
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

    [PunRPC]
    void RPC_Shooting() //an error ..... -> needes an argument
    {
        RaycastHit hit;
        FireFlash.Play();
        StartCoroutine(LaserAnimation(0.2f));
        if (Physics.Raycast(rayOrgin.position, rayOrgin.TransformDirection(Vector3.forward), out hit, 700))
        {
            Debug.DrawRay(rayOrgin.position, rayOrgin.TransformDirection(Vector3.forward) * hit.distance * 700, Color.red);
            Debug.Log("hit");
            if (hit.transform.tag == "Player")
            {
                Debug.Log("player hit");
            }
            else if(hit.transform.tag == "Target")
            {
                Debug.Log("Target  hit");
            }

        }
        else
        {
            Debug.DrawRay(rayOrgin.position, rayOrgin.TransformDirection(Vector3.forward) * hit.distance * 700, Color.white);
            Debug.Log("no hit");
        }     
    }
}
