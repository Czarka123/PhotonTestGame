using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class SaveInfoToFile : MonoBehaviour
{
    // Start is called before the first frame update
    StringBuilder logfile;
    public string fileName;
    int counter = 0;

    void Start()
    {
        logfile = new StringBuilder(); 

        var newLine = string.Format("{0},{1}", transform.position, PhotonNetwork.Time);
        logfile.AppendLine(newLine);

    }

    // Update is called once per frame
    void Update()
    {
        if (counter < 1000)
        {
            var newLine = string.Format("{0},{1}", transform.position, PhotonNetwork.Time);
            logfile.AppendLine(newLine);
        }
        else if (counter == 1000)
        {

            File.WriteAllText(@"path" + fileName + ".csv", logfile.ToString());

            Debug.Log(" Log Saved in file ");

        }
        else
        {

        }
        counter++;
        //Suggestion made by KyleMit


    }
}
