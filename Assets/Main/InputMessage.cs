using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

//[Serializable()]
public struct InputMessage
{
    //public float delivery_time;
    public uint start_tick_number;

    //public InputMessage(int start_tick_number, bool up, bool down, bool right, bool left, bool jump) : this()
    //{
    //    this.start_tick_number = start_tick_number;
    //    this.up = up;
    //    this.down = down;
    //    this.right = right;
    //    this.left = left;
    //    this.jump = jump;
    //}

    //public bool up { get; set; }
    //public bool down { get; set; }
    //public bool right { get; set; }
    //public bool left { get; set; }
    //public bool jump { get; set; }

    public Inputs input;

    public InputMessage(uint start_tick_number, Inputs input)
    {
        this.start_tick_number = start_tick_number;
        this.input = input;
    }

    //public static object Deserialize(byte[] data)
    //{
    //    using (MemoryStream memoryStream = new MemoryStream())
    //    {
    //        BinaryFormatter binaryF = new BinaryFormatter();

    //        memoryStream.Write(data, 0, data.Length);
    //        memoryStream.Seek(0, SeekOrigin.Begin);

    //        return (InputMessage)binaryF.Deserialize(memoryStream);
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

    // public List<Inputs> inputs;
}
