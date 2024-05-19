using System.Collections;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

//ref: https://qiita.com/ZAP_xgame/items/9487aeddd3b9fa9a8f1d
// namespace Unity.FPS.Gameplay
namespace Unity.FPS.Gameplay
{
    public class SerialHandler : MonoBehaviour
    {
        public delegate void SerialDataReceivedEventHandler(string message);
        public event SerialDataReceivedEventHandler OnDataReceived;

        [Tooltip("input COM to ignore")]
        public string portName = "COM3";
        public int baudRate = 115200;

        private SerialPort serialPort_;
        private Thread thread_;
        private bool isRunning_ = false;

        private string message_;
        private bool isNewMessageReceived_ = false;

        // params for reflecting player status
        public bool _isGhostStepArea = false;
        public bool _disableStepFeedBack = false;
        void Awake()
        {
            Open();
            Write("Open");
        }

        void Update()
        {
            // if (isNewMessageReceived_)
            // {
            //     OnDataReceived(message_);
            // }
            // isNewMessageReceived_ = false;
        }

        void OnDestroy()
        {
            Close();
        }

        /// Serial control functions
        private void Open()
        {
            if (portName != "COM")
            {
                serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
                serialPort_.Open();
                serialPort_.ReadTimeout = 100;
                isRunning_ = true;
                // thread_ = new Thread(Read);
                // thread_.Start();
            }
        }
        private void Close()
        {
            isNewMessageReceived_ = false;
            isRunning_ = false;
            if (thread_ != null && thread_.IsAlive)
            {
                thread_.Join();
            }
            if (serialPort_ != null && serialPort_.IsOpen)
            {
                serialPort_.Close();
                serialPort_.Dispose();
            }
        }
        private void Read()
        {
            while (isRunning_ && serialPort_ != null && serialPort_.IsOpen)
            {
                try
                {
                    message_ = serialPort_.ReadLine();
                    isNewMessageReceived_ = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning(e.Message);
                }
            }
        }
        public void Write(string message)
        {
            try
            {
                serialPort_.WriteLine(message);
            }
            catch (System.Exception e)
            {
                // Debug.LogWarning(e.Message);
            }
        }



        // // common functions
        float Map(float value, float FromMin, float FromMax, float ToMin, float ToMax)
        {
            return ToMin + (ToMax - ToMin) * ((value - FromMin) / (FromMax - FromMin));
        }
        int MapToHapbeat(float value)
        {
            return (int)Map(value, 0, 1, 0, 255);
        }


        /// <summary>
        /// 
        /// 
        /// 
        /// device position numbers [0:neck, 1:chest, 2:abdomen, 
        /// 3:upperArm_L, 4:upperArm_R, 5:wrist_L, 6:wrist_R, 
        /// 7:thigh_L, 8:thigh_R, 9:calf_L, 10:calf_R]
        /// <param name="action"></param>
        /// <param name="devicePosition"></param>
        /// <param name="playType">"oneshot", "loopstart", "loopstop",</param>
        /// <param name="leftPower">0--1</param>
        /// <param name="rightPower">0--1</param>
        /// <param name="category">チャンネル。アプリケーションごとに変えるなど。</param>
        /// </summary>
        public void SendSerial(string action, string devicePos, string playType = "oneshot", float leftPower = -1f, float rightPower = -1f, string category = "0")
        {
            //  [category, devicePos, dataID, isStereo, L_Vol, R_Vol]
            string wearerID = "0"; // 複数人数で別々の信号を出したいとき
            string dataID = "0"; // 振動の種類（同じチャネルで再生ファイルが異なるとき）
            string subid = "0"; // ランダム再生用（銃撃や足音などで微妙に異なる振動を出したいとき）
            // 音量について、動的な値は読み出し元で設定、それ以外はここで設定する。
            string c_leftPower = MapToHapbeat(leftPower).ToString(); //左側の振動強度
            string c_rightPower = "-1";
            // c_leftPower : MapToHapbeat(rightPower).ToString(); //右側の振動強度（引数無しなら左と同じ）

            switch (action)
            {
                case "shotblaster":
                    dataID = "0";
                    subid = Random.Range(0, 6).ToString();
                    if (devicePos == "neck")
                    {
                        c_leftPower = Random.Range(10, 20).ToString();
                    }
                    else if (devicePos == "wrist_L")
                    {
                        c_leftPower = "255";
                    }
                    break;
                case "footstep":
                    // 奥以降は足音を消す
                    if (_disableStepFeedBack == true)
                        return;
                    dataID = "1";
                    subid = Random.Range(0, 2).ToString();
                    c_leftPower = Random.Range(50, 60).ToString();
                    // Debug.Log("_isGhostStepArea: " + _isGhostStepArea);
                    break;
                case "damage":
                    dataID = "2";
                    c_leftPower = "40";
                    break;
                case "landing":
                    dataID = "3";
                    c_leftPower = MapToHapbeat(leftPower * 0.01f).ToString();
                    break;
                case "jetpack":
                    dataID = "4";
                    c_leftPower = "20";
                    break;
                case "chargelauncher":
                    dataID = "5";
                    if (devicePos == "neck")
                    {
                        c_leftPower = "30";
                    }
                    else if (devicePos == "wrist_L")
                    {
                        c_leftPower = "255";
                    }
                    break;
                case "shotlauncher":
                    dataID = "6";
                    if (devicePos == "neck")
                    {
                        c_leftPower = "40";
                    }
                    else if (devicePos == "wrist_L")
                    {
                        c_leftPower = "255";
                    }
                    break;
                case "hitlauncher":
                    dataID = "7";
                    break;
                case "shotshotgun":
                    dataID = "8";
                    if (devicePos == "neck")
                    {
                        c_leftPower = "25";
                    }
                    else if (devicePos == "wrist_L")
                    {
                        c_leftPower = "200";
                    }
                    break;
                // maze actions
                case "mazeloop":
                    dataID = "9";
                    c_leftPower = "60";
                    // Debug.Log("mazeloop evoked");
                    break;
                case "leftnotify":
                    dataID = "10";
                    subid = Random.Range(0, 3).ToString();
                    c_leftPower = Random.Range(155, 255).ToString();
                    c_rightPower = "0";
                    break;
                case "rightnotify":
                    dataID = "10";
                    subid = Random.Range(0, 3).ToString();
                    c_leftPower = "0";
                    c_rightPower = Random.Range(155, 255).ToString();
                    break;
                case "heartbeat":
                    dataID = "11";
                    // no need to set power here
                    break;
                case "ghostinvite":
                    dataID = "12";
                    subid = Random.Range(0, 3).ToString();
                    c_leftPower = "150";
                    break;
                case "passwall":
                    dataID = "13";
                    c_leftPower = "50";
                    break;
                case "ghostleft2right":
                    dataID = "14";
                    c_leftPower = "200";
                    break;
                case "ghostright2left":
                    dataID = "15";
                    c_leftPower = "200";
                    break;
                case "ghostcoming":
                    dataID = "16";
                    // no need to set power here
                    break;
                case "ghosteat":
                    dataID = "17";
                    c_leftPower = "150";
                    break;
            };
            // rightPowerについて操作が無ければleftPowerと同じにする
            if (c_rightPower == "-1")
            {
                c_rightPower = c_leftPower;
            }

            switch (devicePos)
            {
                case "neck":
                    devicePos = "0";
                    break;
                case "chest":
                    devicePos = "1";
                    break;
                case "abdomen":
                    devicePos = "2";
                    break;
                case "upperArm_L":
                    devicePos = "3";
                    break;
                case "upperArm_R":
                    devicePos = "4";
                    break;
                case "wrist_L":
                    devicePos = "5";
                    break;
                case "wrist_R":
                    devicePos = "6";
                    break;
                case "thigh_L":
                    devicePos = "7";
                    break;
                case "thigh_R":
                    devicePos = "8";
                    break;
                case "calf_L":
                    devicePos = "9";
                    break;
                case "calf_R":
                    devicePos = "10";
                    break;
            };

            switch (playType)
            {
                case "oneshot":
                    playType = "0";
                    break;
                case "loopstart":
                    playType = "1";
                    break;
                case "loopstop":
                    playType = "2";
                    break;
            }

            List<string> dataList = new List<string>() {
                category, wearerID, devicePos, dataID, subid, c_leftPower, c_rightPower, playType
                };
            string sendData = string.Join(",", dataList);
            Write(sendData);

            // ghost step
            if (action == "footstep" && _isGhostStepArea)
            {
                // delay randomly
                float rnd = Random.Range(0.2f, 0.5f);
                StartCoroutine(DelayedWrite(rnd, sendData));
            }

            if (playType == "2")
            {
                for (int i = 0; i < 3; i++)
                {
                    StartCoroutine(DelayedWrite(0.01f, sendData));
                }
            }
        }

        private IEnumerator DelayedWrite(float sec, string sendData)
        {
            yield return new WaitForSeconds(sec);
            Write(sendData);
        }
    }
}
