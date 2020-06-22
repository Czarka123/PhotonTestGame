using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalTimeDisplayer : MonoBehaviour
{

    [SerializeField] private Text TimeText;
    [SerializeField] private bool displaylocaltime;
    [SerializeField] private bool isGreen;
    // Start is called before the first frame update
    void Start()
    {
       
        
    }

    void SetText(string time, bool IsGreen)
    {
        Debug.Log("event dispaly  " + time + "   " + IsGreen);

        if (isGreen == IsGreen && !displaylocaltime)
        {
            TimeText.text = time;
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (displaylocaltime)
        {
            TimeText.text = Time.timeSinceLevelLoad.ToString();
        }
    }
}
