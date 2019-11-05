using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{

    PhotonView photonView;
    public Transform rayOrgin;

    // Start is called before the first frame update
    void Start()
    {
        photonView = GetComponent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }
        if (Input.GetMouseButton(0))
        {
            photonView.RPC("RPC_Shooting", RpcTarget.All);
        }
        
    }


    [PunRPC]
    void RPC_Shooting()
    {
        RaycastHit hit;
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
    }
}
