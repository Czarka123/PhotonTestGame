﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;



[Serializable()]
public class Action 
{
    public float postionX;
    public float postionY;
    public float postionZ;
    public float inputForward;
    public float inputSide;
    public int sequence_number;
    public double timestamp;
    public int animationState;
    public float rotationAngle;
    public bool shooting;

    public Action()
    {
        sequence_number = -1;
    }

    public Action(float postionX, float postionY, float postionZ, int sequence_number, int animationState, float rotationAngle, bool shooting)
    {
        this.postionX = postionX;
        this.postionY = postionY;
        this.postionZ = postionZ;
        this.sequence_number = sequence_number;
        this.animationState = animationState;
        this.rotationAngle = rotationAngle;
        this.shooting = shooting;
    }

    public Action(float postionX, float postionY, float postionZ, float inputForward, float inputSide, int sequence_number, double timestamp, int animationState, float rotationAngle, bool shooting)
    {
        this.postionX = postionX;
        this.postionY = postionY;
        this.postionZ = postionZ;
        this.inputForward = inputForward;
        this.inputSide = inputSide;
        this.sequence_number = sequence_number;
        this.timestamp = timestamp;
        this.animationState = animationState;
        this.rotationAngle = rotationAngle;
        this.shooting = shooting;
    }

    public void SetAction(Vector3 postion, Vector3 input, int sequence_number, double timestamp, int animationState, float rotationAngle, bool shooting)
    {
        this.postionX = postion.x;
        this.postionY = postion.y;
        this.postionZ = postion.z;
        this.inputForward = input.x;
        this.inputSide = input.z;
        this.sequence_number = sequence_number;
        this.timestamp = timestamp;
        this.animationState = animationState;
        this.rotationAngle = rotationAngle;
        this.shooting = shooting;
    }


    public Vector3 getInput()
    {
        return new Vector3(inputForward, 0, inputSide);
    }

    public void setInput(Vector3 newInput)
    {
        this.inputForward = newInput.x;     
        this.inputSide = newInput.z;
    }

    public Vector3 getPostion()
    {
        return new Vector3(postionX, postionY, postionZ);
    }

    public void setPostion(Vector3 NewPostion)
    {
        this.postionX = NewPostion.x;
        this.postionY = NewPostion.y;
        this.postionZ = NewPostion.z;
    }

    public Vector3 getRotationAngle()
    {
        return new Vector3(0, rotationAngle, 0);
    }


    public static object Deserialize(byte[] data)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            BinaryFormatter binaryF = new BinaryFormatter();

            memoryStream.Write(data, 0, data.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return (Action)binaryF.Deserialize(memoryStream);
        }
    }

    public static byte[] Serialize(object customType)
    {
        if (customType == null)
            return null;
        BinaryFormatter bf = new BinaryFormatter();
        using (MemoryStream ms = new MemoryStream())
        {
            bf.Serialize(ms, customType);
            return ms.ToArray();
        }
    }
}
