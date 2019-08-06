using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealVNC.VncSdk;

public class VncServer : MonoBehaviour
{
    public Server Server { get; private set; }
    private void Start()
    {
        Server = new Server("D:/Repos/VncTest/Assets/RealVnc");
        
    }
}
