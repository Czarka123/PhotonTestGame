using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

//[Serializable()]
public struct Inputs
{

    public bool up { get; set; }
    public bool down { get; set; }
    public bool right { get; set; }
    public bool left { get; set; }
    public bool jump { get; set; }


    public Inputs(bool up, bool down, bool right, bool left, bool jump) : this()
    {
        this.up = up;
        this.down = down;
        this.right = right;
        this.left = left;
        this.jump = jump;
    }



    //public static object Deserialize(byte[] data)
    //{
    //    using (MemoryStream memoryStream = new MemoryStream())
    //    {
    //        BinaryFormatter binaryF = new BinaryFormatter();

    //        memoryStream.Write(data, 0, data.Length);
    //        memoryStream.Seek(0, SeekOrigin.Begin);

    //        return (Inputs)binaryF.Deserialize(memoryStream);
    //    }
    //}

    //public static byte[] Serialize(object customType)
    //{
    //    if (customType == null)
    //        return null;
    //    BinaryFormatter bf = new BinaryFormatter();
    //    using (MemoryStream ms = new MemoryStream())
    //    {
    //        bf.Serialize(ms, customType);
    //        return ms.ToArray();
    //    }
    //}
}