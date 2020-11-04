using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;


[System.Serializable()]
public struct StateMessage
{
    //input data
    public bool up { get; set; }
    public bool down { get; set; }
    public bool right { get; set; }
    public bool left { get; set; }
    public bool jump { get; set; }

    public double delivery_time;
    public uint tick_number;
    //public Vector3 position;
    public float posX;
    public float posY;
    public float posZ;

    public float rotationX;
    public float rotationY;
    public float rotationZ;
    public float rotationW;

    public float velocityX;
    public float velocityY;
    public float velocityZ;

    public float angular_velocityX;
    public float angular_velocityY;
    public float angular_velocityZ;

    public static object Deserialize(byte[] data)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            BinaryFormatter binaryF = new BinaryFormatter();

            memoryStream.Write(data, 0, data.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return (StateMessage)binaryF.Deserialize(memoryStream);
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

    public void setPosition(Vector3 pos)
    {
        posX = pos.x;
        posY = pos.y;
        posZ = pos.z;
    }

    public void setVelocity(Vector3 vel)
    {
        velocityX = vel.x;
        velocityY = vel.y;
        velocityZ = vel.z;
    }

    public void setVelocity_Ang(Vector3 vela)
    {
        angular_velocityX = vela.x;
        angular_velocityY = vela.y;
        angular_velocityZ = vela.z;
    }

    public void setRotation(Quaternion rot)
    {
        rotationX = rot.x;
        rotationY = rot.y;
        rotationZ = rot.z;
        rotationW = rot.w;
    }

    public void setInput(Inputs inp)
    {
        up = inp.up;
        down = inp.down;
        right = inp.right;
        left = inp.left;
        jump = inp.jump;

    }


    public Quaternion getRotation()
    {
       
        return Quaternion.Normalize(new Quaternion (rotationX, rotationY, rotationZ, rotationW));
    }

    public Vector3 getPosition()
    {
        return new Vector3(posX,posY,posZ);
    }

    public Vector3 getVelocity()
    {
        return new Vector3(velocityX,velocityY,velocityZ);
    }

    public Vector3 getVelocity_Angular()
    {
        return new Vector3(angular_velocityX,angular_velocityY,angular_velocityZ);
    }

    public Inputs getInput()
    {
        return new Inputs(up, down, right, left, jump);
    }
}
