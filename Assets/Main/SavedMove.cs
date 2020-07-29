using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[Serializable()]
public class SavedMove
{
    public SavedMove()
    {

    }

    public SavedMove(double timestamp, float forwardmove, float sidemove, float rotationAngle, float postionX, float postionY, float postionZ, float velocityX, float velocityY, float velocityZ, float speed, bool shooting)
    {
        this.timestamp = timestamp;
        this.forwardmove = forwardmove;
        this.sidemove = sidemove;
        this.rotationAngle = rotationAngle;
        this.postionX = postionX;
        this.postionY = postionY;
        this.postionZ = postionZ;
        this.velocityX = velocityX;
        this.velocityY = velocityY;
        this.velocityZ = velocityZ;
        this.speed = speed;
        this.shooting = shooting;
    }

    public double timestamp { get; set; }

    public float forwardmove { get; set; }

    public float sidemove { get; set; }

    public float rotationAngle { get; set; }

    public float postionX { get; set; }

    public float postionY { get; set; }

    public float postionZ { get; set; }

    public float velocityX { get; set; }

    public float velocityY { get; set; }

    public float velocityZ { get; set; }

    public float speed { get; set; }

    public bool shooting { get; set; }// wasn't here

    public Vector3 getPostion()
    {
        return new Vector3(postionX, postionY, postionZ);
    }

    public Vector3 getDirection()
    {
        return new Vector3(sidemove, 0, forwardmove);
    }

    public Vector3 getVelocity()
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

            return (SavedMove)binaryF.Deserialize(memoryStream);
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
