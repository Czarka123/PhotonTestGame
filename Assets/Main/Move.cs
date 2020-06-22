using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move 
{
    public Vector3 moveDir;
    public Vector3 RotateVector;
    public float deltaTime;
    public float Timestamp;
    public bool isDirty;
    public BotState state;



    public Move(Vector3 moveDir, Vector3 rotateVector, float deltaTime, float timestamp, BotState state)
    {
        this.moveDir = moveDir;
        RotateVector = rotateVector;
        this.deltaTime = deltaTime;
        this.state = state;
        Timestamp = timestamp;
        isDirty = false;
    }

}
