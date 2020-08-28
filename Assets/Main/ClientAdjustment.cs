using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[Serializable()]
public class ClientAdjustment 
{
    public bool AckGoodMove;
    //float DeltaTime
    public float NewLocX;
    public float NewLocY;
    public float NewLocZ;
    public float NewRot; //new quaternion
    //Vector3 NewVel;
    public double TimeStamp;

    public ClientAdjustment()
    {

    }

    public ClientAdjustment(bool ackGoodMove, Vector3 newLoc, float newRot, double timeStamp)
    {
        AckGoodMove = ackGoodMove;
        NewLocX = newLoc.x;
        NewLocY = newLoc.y;
        NewLocZ = newLoc.z;
        NewRot = newRot;
        TimeStamp = timeStamp;
    }

    public Vector3 getNewLoc()
    {
        return new Vector3(NewLocX, NewLocY, NewLocZ);
    }

    public static object Deserialize(byte[] data)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            BinaryFormatter binaryF = new BinaryFormatter();

            memoryStream.Write(data, 0, data.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return (ClientAdjustment)binaryF.Deserialize(memoryStream);
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
