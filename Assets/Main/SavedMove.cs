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
        timestamp = 0;
    }

    public SavedMove(double timestamp, float forwardmove, float sidemove, float rotationAngle, float startRotationAngle, float postionX, float postionY, float postionZ, float startPostionX, float startPostionY, float startPostionZ, float speed, bool shooting, bool stand)
    {
        this.timestamp = timestamp;
        this.forwardmove = forwardmove;
        this.sidemove = sidemove;
        this.rotationAngle = rotationAngle;
        this.startRotationAngle = startRotationAngle;
        this.postionX = postionX;
        this.postionY = postionY;
        this.postionZ = postionZ;
        this.startPostionX = startPostionX;
        this.startPostionY = startPostionY;
        this.startPostionZ = startPostionZ;
        this.speed = speed;
        this.shooting = shooting;
        this.stand = stand;
    }

    public SavedMove(double timestamp, float forwardmove, float sidemove, float rotationAngle, float startRotationAngle, Vector3 postion, Vector3 startPostion, float speed, bool shooting, bool stand)
    {
        this.timestamp = timestamp;
        this.forwardmove = forwardmove;
        this.sidemove = sidemove;
        this.rotationAngle = rotationAngle;
        this.startRotationAngle = startRotationAngle;
        postionX= postion.x;
        postionY = postion.y;
        postionZ = postion.z;
        startPostionX = startPostion.x;
        startPostionY = startPostion.y;
        startPostionZ = startPostion.z;
        this.speed = speed;
        this.shooting = shooting;
        this.stand = stand;
    }

    public SavedMove(double timestamp, float forwardmove, float sidemove, float rotationAngle, Vector3 postion, float speed, bool shooting, bool stand)
    {
        this.timestamp = timestamp;
        this.forwardmove = forwardmove;
        this.sidemove = sidemove;
        this.rotationAngle = rotationAngle;
        
        postionX = postion.x;
        postionY = postion.y;
        postionZ = postion.z;
  
        this.speed = speed;
        this.shooting = shooting;
        this.stand = stand;
    }

    public double timestamp { get; set; }

    public float forwardmove { get; set; }

    public float sidemove { get; set; }

    public float rotationAngle { get; set; }

    public float startRotationAngle { get; set; }

    public float postionX { get; set; }

    public float postionY { get; set; }

    public float postionZ { get; set; }

    public float startPostionX { get; set; }

    public float startPostionY { get; set; }

    public float startPostionZ { get; set; }

    //public float velocityX { get; set; }

    //public float velocityY { get; set; }

    //public float velocityZ { get; set; }

    public float speed { get; set; }

    public bool shooting { get; set; }// wasn't here

    public bool stand { get; set; }// can be in animation

    public Vector3 getPostion()
    {
        return new Vector3(postionX, postionY, postionZ);
    }

    public Vector3 getStartPostion()
    {
        return new Vector3(startPostionX, startPostionY, startPostionZ);
    }

    public Vector3 getDirection()
    {
        return new Vector3(sidemove, 0, forwardmove);
    }

    //public Vector3 getVelocity()
    //{
    //    return new Vector3(velocityX, velocityY, velocityZ);
    //}

    public void setPostion(Vector3 NewPostion)
    {
        this.postionX = NewPostion.x;
        this.postionY = NewPostion.y;
        this.postionZ = NewPostion.z;
    }

    public void setStartPostion(Vector3 StPostion)
    {
        this.startPostionX = StPostion.x;
        this.startPostionY = StPostion.y;
        this.startPostionZ = StPostion.z;
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
