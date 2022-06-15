using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Unity.FPS.Game
{


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VRDevData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] angle;
        public float devSpeed;
        public int btnClick;
    }


}