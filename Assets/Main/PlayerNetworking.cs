using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNetworking : MonoBehaviour
{
    [SerializeField]
    private GameObject camera;
    [SerializeField]
    private MonoBehaviour[] scriptsToIgnore;
    PhotonView photonView;


    // Start is called before the first frame update
    void Start()
    {

        photonView = GetComponent<PhotonView>();
        Initialize();
    }

    private void Initialize()
    {
        if (photonView.IsMine)
        {

        }
        else
        {
            camera.SetActive(false);
            foreach(MonoBehaviour item in scriptsToIgnore)
            {
               // item.enabled=false;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
