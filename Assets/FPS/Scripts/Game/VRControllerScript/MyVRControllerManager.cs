using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Threading;
using System.Runtime.InteropServices;


namespace Unity.FPS.Game
{


    enum NetworkState
    {
        Ready = 0,
        Connecting,
        Connected,
        Disconnected,
        Exit
    }


    public class MyVRControllerManager : MonoBehaviour
    {
        // Start is called before the first frame update

        [SerializeField] float syncReceiveTimeout;
        [SerializeField] float pollInterval;
        [SerializeField] int pollTrials;


        UdpClient cli;


        IPEndPoint endPoint;
        string message;
        AsyncCallback callback;

        NetworkState state = NetworkState.Ready;

        byte[] bytes;

        [SerializeField]int PORTNO = 9002;

        Vector3 devVel;
        Vector3 devAngularVel;
        Vector3 devRotation;


        VRDevData prevData, curData;
        public VRDevData devData { get; private set; }
        public bool devAvailable { get; private set; }


        [HideInInspector]public bool Shake = false;
        [SerializeField] float shakeThreshold;
        [SerializeField] float shakeDelay;
        float shakeTimer = 0;


        public static MyVRControllerManager instance
        {
            private set; get;
        }

        private void Awake()
        {
            if (instance == null)
            {
                print("VRControllerManager created!!");
                instance = this;
                devAvailable = false;
            }
            else
                throw new System.Exception("Only 1 of MyVRControllerManager can exists.");

        }

        void Start()
        {
            Init();
            print("init");


        }

        private void Update()
        {
            if(shakeTimer <= 0 && devData.devSpeed >= shakeThreshold)
            {
                Shake = true;
                shakeTimer = shakeDelay;
            }
            else
            {
                Shake = false;
            }

            if(shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;   
            }
        }

        #region
        void Init()
        {
            cli = new UdpClient(PORTNO);
            Thread thread = new Thread(ConnToRaspi);
            thread.Start();

        }

        void ConnToRaspi()
        {
            state = NetworkState.Ready;

            print("finding device...");

            cli.Client.ReceiveTimeout = (int)(1000 * syncReceiveTimeout);

            try
            {
                bytes = cli.Receive(ref endPoint);
            }
            catch (SocketException e)
            {
                state = NetworkState.Disconnected;
                print(e);
                return;
            }

            string str = Encoding.UTF8.GetString(bytes);
            str = str.Substring(0, str.Length - 1);
            if (str.Equals("synctounity"))
            {
                print("connecting...");
                state = NetworkState.Connecting;
                cli.Client.ReceiveTimeout = (int)(1000 * pollInterval);
                bytes = Encoding.UTF8.GetBytes("ackfromunity");

                for (int i = 0; i < pollTrials; i++)
                {
                    try
                    {
                        cli.Send(bytes, bytes.Length, endPoint);
                        bytes = cli.Receive(ref endPoint);
                        state = NetworkState.Connected;
                        break;
                    }
                    catch (SocketException e)
                    {
                        print("polling...");
                        state = NetworkState.Disconnected;
                        continue;
                    }

                    catch (Exception e)
                    {
                        Debug.LogError(e);

                    }
                }

                switch (state)
                {
                    case NetworkState.Connected:
                        StartRecvControllerInfo();
                        break;
                    case NetworkState.Disconnected:

                        break;
                }

                print("yes");
                //이제 TCP를 이용해 연결을 정립하고 connection 연결하는거 지속하도록 코드 작성하자.
            }
            else
            {
                state = NetworkState.Disconnected;
            }

            if (state == NetworkState.Connected)
            {
                StartRecvControllerInfo();
            }
            else
            {
                print("connection failed!");
            }

        }

        void StartRecvControllerInfo()
        {

            Thread thread = new Thread(ReadSensor);
            thread.Start();
        }

        void ReadSensor()
        {

            while (state == NetworkState.Connected)
            {
                try
                {
                    bytes = cli.Receive(ref endPoint);
                }
                catch (SocketException e)
                {
                    print(e);
                    state = NetworkState.Disconnected;
                    return;
                }

                UpdateDevData(bytes);

                print("x: " + devData.angle[0]
                    + ", y: " + devData.angle[1]
                    + ", z: " + devData.angle[2]
                    + ", w: " + devData.angle[3]
                    + ", devSpeed: " + devData.devSpeed
                    + ", btnClick: " + devData.btnClick);


            }
        }
        #endregion

        private void UpdateDevData(byte[] buffer)
        {


            int size = Marshal.SizeOf(typeof(VRDevData));

            if (size > buffer.Length)
            {
                throw new Exception();
            }

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, ptr, size);
            devData = (VRDevData)Marshal.PtrToStructure(ptr, typeof(VRDevData));

            devAvailable = true;

            Marshal.FreeHGlobal(ptr);

        }
    }
}