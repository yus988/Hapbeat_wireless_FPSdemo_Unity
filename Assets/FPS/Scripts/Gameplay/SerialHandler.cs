using System.Collections;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

//ref: https://qiita.com/ZAP_xgame/items/9487aeddd3b9fa9a8f1d
namespace Unity.FPS.Gameplay
{
    public class SerialHandler : MonoBehaviour
    {
        public delegate void SerialDataReceivedEventHandler(string message);
        public event SerialDataReceivedEventHandler OnDataReceived;

        public string portName = "COM3";
        public int baudRate = 115200;

        private SerialPort serialPort_;
        private Thread thread_;
        private bool isRunning_ = false;

        private string message_;
        private bool isNewMessageReceived_ = false;

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

        private void Open()
        {
            serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            serialPort_.Open();
            serialPort_.ReadTimeout = 100;
            isRunning_ = true;
            thread_ = new Thread(Read);
            thread_.Start();
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
                // serialPort_.Write(message);
                serialPort_.WriteLine(message);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e.Message);
            }
        }

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
        /// </summary>
        /// <param name="category"></param>
        /// <param name="action"></param>
        /// <param name="position"></param>
        /// <param name="leftPower"></param>
        /// <param name="rightPower"></param>
        public void SendSerial(string category, string action, string position, float leftPower, float rightPower = -1f)
        {
            //  [cat, pos, id, isStereo, L_Vol, R_Vol]
            string cat = "0"; // チャンネル。PvPで1Pと2Pで分ける、アプリケーションごとに変えるなど。
            string pos = "0"; // 装着位置
            string id = "0"; // 振動の種類（同じチャネルで再生ファイルが異なるとき）
            string isStereo = (rightPower == -1) ? "0" : "1"; //ステレオか否か
            string c_leftPower = MapToHapbeat(leftPower).ToString(); //左側の振動強度
            string c_rightPower = MapToHapbeat(rightPower).ToString(); //右側の振動強度

            switch (action)
            {
                case "gunshot":
                    id = "0";
                    break;
                case "footstep":
                    id = "1";
                    break;
                case "damage":
                    id = "2";
                    break;

            }
            switch (position)
            {
                case "neck":
                    pos = "0";
                    break;
                case "right_arm":
                    pos = "1";
                    break;
            }
            List<string> dataList = new List<string>() { category, pos, id, isStereo, c_leftPower, c_rightPower };
            string sendData = string.Join(",", dataList);
            Write(sendData);
            //  Write("0, 0, 0, 0, 100, 100");
        }
    }
}
