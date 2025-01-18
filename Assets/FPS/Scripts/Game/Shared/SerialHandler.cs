using System.Collections;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

namespace Unity.FPS.Gameplay
{
    public class SerialHandler : MonoBehaviour
    {

        // デバイス位置ごとのパワー設定を定義する構造体
        [System.Serializable]

        // シリアライズ可能なディクショナリ
        public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
        {
            [SerializeField]
            private List<TKey> keys = new List<TKey>();

            [SerializeField]
            private List<TValue> values = new List<TValue>();

            public void OnBeforeSerialize()
            {
                keys.Clear();
                values.Clear();
                foreach (KeyValuePair<TKey, TValue> pair in this)
                {
                    keys.Add(pair.Key);
                    values.Add(pair.Value);
                }
            }

            public void OnAfterDeserialize()
            {
                this.Clear();

                if (keys.Count != values.Count)
                    throw new System.Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable.", keys.Count, values.Count));

                for (int i = 0; i < keys.Count; i++)
                    this.Add(keys[i], values[i]);
            }
        }

        // アクション設定を定義する構造体
        [System.Serializable]
        public struct ActionSettings
        {
            public string dataID;
            [SerializeField]
            public SerializableDictionary<string, DevicePowerSettings> devicePowers;
            public bool useRandomSubID;
            public Vector2Int subIDRange;
            public bool useRandomPower;
            public Vector2Int randomPowerRange;

            public ActionSettings(
                string dataID,
                SerializableDictionary<string, DevicePowerSettings> devicePowers = null,
                bool useRandomSubID = false,
                Vector2Int subIDRange = new Vector2Int(),
                bool useRandomPower = false,
                Vector2Int randomPowerRange = new Vector2Int())
            {
                this.dataID = dataID;
                this.devicePowers = devicePowers ?? new SerializableDictionary<string, DevicePowerSettings>();
                this.useRandomSubID = useRandomSubID;
                this.subIDRange = subIDRange;
                this.useRandomPower = useRandomPower;
                this.randomPowerRange = randomPowerRange;
            }
        }

        //===============================================
        // アプリに応じて変更 カテゴリ分け
        //===============================================

        [Header("Action Settings")]
        [SerializeField]
        private SerializableDictionary<string, ActionSettings> actionSettings = new SerializableDictionary<string, ActionSettings>();


        // まず構造体を修正
        [System.Serializable]
        public struct DevicePowerSettings
        {
            public Vector2Int leftPowerRange;  // MinとMaxを指定。Max=Minの場合は固定値として扱う
            public Vector2Int rightPowerRange; // MinとMaxを指定。Max=Minの場合は固定値として扱う

            public DevicePowerSettings(int leftPower)
                : this(new Vector2Int(leftPower, leftPower), new Vector2Int(leftPower, leftPower))
            {
            }

            public DevicePowerSettings(Vector2Int leftPowerRange)
                : this(leftPowerRange, leftPowerRange)
            {
            }

            public DevicePowerSettings(Vector2Int leftPowerRange, Vector2Int rightPowerRange)
            {
                this.leftPowerRange = leftPowerRange;
                this.rightPowerRange = rightPowerRange;
            }

            public bool IsLeftPowerRandom => leftPowerRange.x != leftPowerRange.y;
            public bool IsRightPowerRandom => rightPowerRange.x != rightPowerRange.y;
        }

        [Header("Communication Parameters")]
        public string _Category = "99";      // チャンネル。アプリケーションごとに変えるなど
        public string _WearerID = "99";      // 複数人数で別々の信号を出したいとき

        //     // アクション設定の初期化
        //     private void InitializeActionSettings()
        //     {
        //         actionSettings = new SerializableDictionary<string, ActionSettings>()
        // {
        //     // 武器関連
        //     {
        //         "shotblaster",
        //         new ActionSettings(
        //             dataID: "0",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(new Vector2Int(20, 35))},
        //                 {"wrist_L", new DevicePowerSettings(new Vector2Int(100, 155))}
        //             },
        //             useRandomSubID: true,
        //             subIDRange: new Vector2Int(0, 6)
        //         )
        //     },
        //     {
        //         "footstep",
        //         new ActionSettings(
        //             dataID: "1",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(new Vector2Int(40, 60))},
        //                 {"wrist_L", new DevicePowerSettings(new Vector2Int(40, 60))}
        //             },
        //             useRandomSubID: true,
        //             subIDRange: new Vector2Int(0, 2)
        //         )
        //     },
        //     {
        //         "damage",
        //         new ActionSettings(
        //             dataID: "2",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(30)},
        //                 {"wrist_L", new DevicePowerSettings(30)}
        //             }
        //         )
        //     },
        //     {
        //         "landing",
        //         new ActionSettings(
        //             dataID: "3",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(0)},
        //                 {"wrist_L", new DevicePowerSettings(0)}
        //             }
        //         )
        //     },
        //     {
        //         "jetpack",
        //         new ActionSettings(
        //             dataID: "4",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(20)},
        //                 {"wrist_L", new DevicePowerSettings(20)}
        //             }
        //         )
        //     },
        //     {
        //         "chargelauncher",
        //         new ActionSettings(
        //             dataID: "5",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(20)},
        //                 {"wrist_L", new DevicePowerSettings(100)}
        //             }
        //         )
        //     },
        //     {
        //         "shotlauncher",
        //         new ActionSettings(
        //             dataID: "6",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(35)},
        //                 {"wrist_L", new DevicePowerSettings(new Vector2Int(200, 255))}
        //             },
        //             useRandomSubID: false
        //         )
        //     },
        //     {
        //         "hitlauncher",
        //         new ActionSettings(
        //             dataID: "7",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(0)},
        //                 {"wrist_L", new DevicePowerSettings(0)}
        //             }
        //         )
        //     },
        //     {
        //         "shotshotgun",
        //         new ActionSettings(
        //             dataID: "8",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(20)},
        //                 {"wrist_L", new DevicePowerSettings(new Vector2Int(100, 155))}
        //             }
        //         )
        //     },
        //     // 迷路関連
        //     {
        //         "mazeloop",
        //         new ActionSettings(
        //             dataID: "9",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(60)},
        //                 {"wrist_L", new DevicePowerSettings(60)}
        //             }
        //         )
        //     },
        //     {
        //         "leftnotify",
        //         new ActionSettings(
        //             dataID: "10",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(
        //                     new Vector2Int(155, 255),
        //                     new Vector2Int(0, 0)
        //                 )},
        //                 {"wrist_L", new DevicePowerSettings(
        //                     new Vector2Int(155, 255),
        //                     new Vector2Int(0, 0)
        //                 )}
        //             },
        //             useRandomSubID: true,
        //             subIDRange: new Vector2Int(0, 3)
        //         )
        //     },
        //     {
        //         "rightnotify",
        //         new ActionSettings(
        //             dataID: "10",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(
        //                     new Vector2Int(0, 0),
        //                     new Vector2Int(155, 255)
        //                 )},
        //                 {"wrist_L", new DevicePowerSettings(
        //                     new Vector2Int(0, 0),
        //                     new Vector2Int(155, 255)
        //                 )}
        //             },
        //             useRandomSubID: true,
        //             subIDRange: new Vector2Int(0, 3)
        //         )
        //     },
        //     {
        //         "heartbeat",
        //         new ActionSettings(
        //             dataID: "11",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(0)},
        //                 {"wrist_L", new DevicePowerSettings(0)}
        //             },
        //             useRandomSubID: true,
        //             subIDRange: new Vector2Int(0, 2)
        //         )
        //     },
        //     // ゴースト関連
        //     {
        //         "ghostinvite",
        //         new ActionSettings(
        //             dataID: "12",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(150)},
        //                 {"wrist_L", new DevicePowerSettings(150)}
        //             },
        //             useRandomSubID: true,
        //             subIDRange: new Vector2Int(0, 3)
        //         )
        //     },
        //     {
        //         "passwall",
        //         new ActionSettings(
        //             dataID: "13",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(50)},
        //                 {"wrist_L", new DevicePowerSettings(50)}
        //             }
        //         )
        //     },
        //     {
        //         "ghostleft2right",
        //         new ActionSettings(
        //             dataID: "14",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(170)},
        //                 {"wrist_L", new DevicePowerSettings(170)}
        //             }
        //         )
        //     },
        //     {
        //         "ghostright2left",
        //         new ActionSettings(
        //             dataID: "15",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(170)},
        //                 {"wrist_L", new DevicePowerSettings(170)}
        //             }
        //         )
        //     },
        //     {
        //         "ghostcoming",
        //         new ActionSettings(
        //             dataID: "16",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(0)},
        //                 {"wrist_L", new DevicePowerSettings(0)}
        //             },
        //             useRandomSubID: true,
        //             subIDRange: new Vector2Int(0, 2)
        //         )
        //     },
        //     {
        //         "ghosteat",
        //         new ActionSettings(
        //             dataID: "17",
        //             devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
        //                 {"neck", new DevicePowerSettings(100)},
        //                 {"wrist_L", new DevicePowerSettings(100)}
        //             }
        //         )
        //     }
        // };
        //     }

        //===============================================
        // アプリに応じて変更 イベント用
        //===============================================

        // アクション設定の初期化
        private void InitializeActionSettings()
        {
            actionSettings = new SerializableDictionary<string, ActionSettings>()
    {
        // 武器関連
        {
            "shotblaster",
            new ActionSettings(
                dataID: "0",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(new Vector2Int(50, 75))},
                },
                useRandomSubID: true,
                subIDRange: new Vector2Int(0, 6)
            )
        },
        {
            "footstep",
            new ActionSettings(
                dataID: "1",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(new Vector2Int(30, 50))},
                },
                useRandomSubID: true,
                subIDRange: new Vector2Int(0, 2)
            )
        },
        {
            "damage",
            new ActionSettings(
                dataID: "2",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(40)},
                }
            )
        },
        {
            "landing",
            new ActionSettings(
                dataID: "3",5
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(0)},
                }
            )
        },
        {
            "jetpack",
            new ActionSettings(
                dataID: "4",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(30)},
                }
            )
        },
        {
            "chargelauncher",
            new ActionSettings(
                dataID: "5",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(50)},
                }
            )
        },
        {
            "shotlauncher",
            new ActionSettings(
                dataID: "6",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(50)},
                    {"all", new DevicePowerSettings(0)},
                },
                useRandomSubID: false
            )
        },
        {
            "hitlauncher",
            new ActionSettings(
                dataID: "7",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(0)},
                }
            )
        },
        {
            "shotshotgun",
            new ActionSettings(
                dataID: "8",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(50)},
                }
            )
        },
        // 迷路関連
        {
            "mazeloop",
            new ActionSettings(
                dataID: "9",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(60)},
                }
            )
        },
        {
            "leftnotify",
            new ActionSettings(
                dataID: "10",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(
                        new Vector2Int(155, 255),
                        new Vector2Int(0, 0)
                    )},
                },
                useRandomSubID: true,
                subIDRange: new Vector2Int(0, 3)
            )
        },
        {
            "rightnotify",
            new ActionSettings(
                dataID: "10",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(
                        new Vector2Int(0, 0),
                        new Vector2Int(155, 255)
                    )},
                },
                useRandomSubID: true,
                subIDRange: new Vector2Int(0, 3)
            )
        },
        {
            "heartbeat",
            new ActionSettings(
                dataID: "11",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(0)},
                },
                useRandomSubID: true,
                subIDRange: new Vector2Int(0, 2)
            )
        },
        // ゴースト関連
        {
            "ghostinvite",
            new ActionSettings(
                dataID: "12",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(150)},
                },
                useRandomSubID: true,
                subIDRange: new Vector2Int(0, 3)
            )
        },
        {
            "passwall",
            new ActionSettings(
                dataID: "13",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(50)},
                }
            )
        },
        {
            "ghostleft2right",
            new ActionSettings(
                dataID: "14",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(170)},
                }
            )
        },
        {
            "ghostright2left",
            new ActionSettings(
                dataID: "15",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(170)},
                }
            )
        },
        {
            "ghostcoming",
            new ActionSettings(
                dataID: "16",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(0)},
                },
                useRandomSubID: true,
                subIDRange: new Vector2Int(0, 2)
            )
        },
        {
            "ghosteat",
            new ActionSettings(
                dataID: "17",
                devicePowers: new SerializableDictionary<string, DevicePowerSettings> {
                    {"neck", new DevicePowerSettings(100)},
                }
            )
        }
    };
        }

        //===============================================
        // アプリに応じて変更 end
        //===============================================

        [Header("Device Settings")]
        [SerializeField]
        public Dictionary<string, string> DevicePositionMap = new Dictionary<string, string>()
        {
            {"neck", "0"},
            {"chest", "1"},
            {"abdomen", "2"},
            {"upperArm_L", "3"},
            {"upperArm_R", "4"},
            {"wrist_L", "5"},
            {"wrist_R", "6"},
            {"thigh_L", "7"},
            {"thigh_R", "8"},
            {"calf_L", "9"},
            {"calf_R", "10"},
            {"all", "99"}
        };

        [Header("Play Type Settings")]
        [SerializeField]
        public Dictionary<string, string> PlayTypeMap = new Dictionary<string, string>()
        {
            {"oneshot", "0"},
            {"loopstart", "1"},
            {"loopstop", "2"},
            {"oneshot_bg", "3"}
        };

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

        public bool _isGhostStepArea = false;
        public bool _disableStepFeedBack = false;


        void Awake()
        {
            InitializeActionSettings();
            Open();
            Write("Open");
        }
        void Update()
        {
            if (isNewMessageReceived_)
            {
                OnDataReceived?.Invoke(message_);
                isNewMessageReceived_ = false;
            }
        }

        void OnDestroy()
        {
            Close();
        }

        private void Open()
        {
            if (portName != "COM")
            {
                serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
                serialPort_.Open();
                serialPort_.ReadTimeout = 100;
                isRunning_ = true;
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

        private float Map(float value, float FromMin, float FromMax, float ToMin, float ToMax)
        {
            return ToMin + (ToMax - ToMin) * ((value - FromMin) / (FromMax - FromMin));
        }

        private int MapToHapbeat(float value)
        {
            return (int)Map(value, 0, 1, 0, 255);
        }

        public void SendSerial(string action, string devicePos, string playType = "oneshot", float leftPower = -1f, float rightPower = -1f)
        {
            if (!actionSettings.ContainsKey(action))
            {
                Debug.LogWarning($"Unknown action: {action}");
                return;
            }

            // アクション設定を取得
            var settings = actionSettings[action];

            // デバイス位置が設定されていない場合はスキップ
            if (settings.devicePowers == null || !settings.devicePowers.ContainsKey(devicePos))
            {
                return;
            }

            // footstepの特別処理
            if (action == "footstep" && _disableStepFeedBack)
            {
                return;
            }

            // データIDとサブID
            string dataID = settings.dataID;
            string subid = settings.useRandomSubID ?
                Random.Range(settings.subIDRange.x, settings.subIDRange.y).ToString() : "0";

            // デバイス位置のパワー設定を取得
            string c_leftPower, c_rightPower;

            if (settings.devicePowers != null && settings.devicePowers.ContainsKey(devicePos))
            {
                var devicePower = settings.devicePowers[devicePos];

                // 左パワーの決定
                if (leftPower >= 0)
                {
                    c_leftPower = MapToHapbeat(leftPower).ToString();
                }
                else if (devicePower.IsLeftPowerRandom)
                {
                    c_leftPower = Random.Range(devicePower.leftPowerRange.x, devicePower.leftPowerRange.y).ToString();
                }
                else
                {
                    c_leftPower = devicePower.leftPowerRange.x.ToString();
                }

                // 右パワーの決定
                if (rightPower >= 0)
                {
                    c_rightPower = MapToHapbeat(rightPower).ToString();
                }
                else if (devicePower.IsRightPowerRandom)
                {
                    c_rightPower = Random.Range(devicePower.rightPowerRange.x, devicePower.rightPowerRange.y).ToString();
                }
                else
                {
                    c_rightPower = devicePower.rightPowerRange.x.ToString();
                }
            }
            else
            {
                // デバイス位置の設定がない場合は0を設定
                c_leftPower = "0";
                c_rightPower = "0";
                Debug.LogWarning($"No device power settings for {action} at {devicePos}");
            }

            // footstepの場合はplayTypeを強制的に変更
            if (action == "footstep")
            {
                playType = "oneshot_bg";
            }

            // マッピング変換
            string mappedDevicePos = DevicePositionMap.ContainsKey(devicePos) ? DevicePositionMap[devicePos] : devicePos;
            string mappedPlayType = PlayTypeMap.ContainsKey(playType) ? PlayTypeMap[playType] : playType;

            // データリストの作成と送信
            List<string> dataList = new List<string>() {
        _Category, _WearerID, mappedDevicePos, dataID, subid, c_leftPower, c_rightPower, mappedPlayType
            };

            string sendData = string.Join(",", dataList);
            Write(sendData);

            // 特殊ケースの処理
            if (action == "footstep" && _isGhostStepArea)
            {
                float rnd = Random.Range(0.1f, 0.3f);
                StartCoroutine(DelayedWrite(rnd, sendData));
            }

            if (mappedPlayType == "2")
            {
                for (int i = 0; i < 2; i++)
                {
                    StartCoroutine(DelayedWrite(0.1f, sendData));
                }
            }
            IEnumerator DelayedWrite(float sec, string sendData)
            {
                yield return new WaitForSeconds(sec);
                Write(sendData);
            }
        }
    }
}