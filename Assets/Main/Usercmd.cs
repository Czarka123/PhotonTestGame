﻿using ExitGames.Client.Photon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[Serializable()]
public class Usercmd 
{
    public Usercmd(float msec, float forwardmove, float sidemove, float rotationAngle, float postionX, float postionY)
    {
        this.msec = msec;
        this.forwardmove = forwardmove;
        this.sidemove = sidemove;
        this.rotationAngle = rotationAngle;
        this.postionX = postionX;
        this.postionY = postionY;
    }

    //short lerp_msec;

    //byte msec;
    //// Command view angles.
    //Vector3 viewangles;
    //// intended velocities
    //// Forward velocity.
    //float forwardmove;
    //// Sideways velocity.
    //float sidemove;
    //// Upward velocity.
    //float upmove;
    //// Attack buttons
    //short buttons;

    public float msec { get; set; }

    public float forwardmove { get; set; }

    public float sidemove { get; set; }

    public float rotationAngle { get; set; }

    public float postionX { get; set; }

    public float postionY { get; set; }

  

    public static object Deserialize(byte[] data)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            BinaryFormatter binaryF = new BinaryFormatter();

            memoryStream.Write(data, 0, data.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return (Usercmd)binaryF.Deserialize(memoryStream);
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
