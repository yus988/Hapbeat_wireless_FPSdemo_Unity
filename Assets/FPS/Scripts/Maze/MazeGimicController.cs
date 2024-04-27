using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Gameplay;
using Unity.FPS;
using UnityEngine.XR;


public class MazeGimicController : MonoBehaviour
{
    private SerialHandler _SerialHandler;

    [SerializeField] GameObject _PC_Player;
    [SerializeField] GameObject _VR_Player;
    private GameObject _Player;
    [Header("Attach reference objects")]
    [SerializeField] GameObject _Floor;
    [SerializeField] GameObject _FirstBranch;
    [SerializeField] GameObject _SecondBranchLeft;
    [SerializeField] GameObject _SecondBranchRight;
    private GameObject _SecondBranch;

    [Header("Attach Gimic Walls")]
    [SerializeField] GameObject _LeftDeadend;
    [SerializeField] GameObject _RightDeadend;
    [Header("プレイヤーの行動を制限する壁")]
    [SerializeField] GameObject[] _ObstacleWalls;

    // 子要素の MeshRenderer コンポーネントを無効にするため
    [Header("透明にしたいコライダーを参照")]
    [SerializeField] GameObject[] _Colliders;

    private float _lastTriggerTime;
    private float _maxDistanceOfFirstArea;

    public SerialHandler _SerialHandlerScript;

    public float _repeatInterval = 1f; // 実行間隔

    // params for notifyingdanger
    private bool _isNotifyingDanger = false;
    private float _timer = 0f;

    // last flab
    private bool[] _finalGhostFlags = { false, false, false };

    string[] _correctDir = { "", "" };
    // Start is called before the first frame update
    void Start()
    {
        _SerialHandler = GameObject.Find("SerialHandler").GetComponent<SerialHandler>();
        // make invisible of all collidars


        if (XRSettings.enabled)
        {
            Debug.Log("VRモードです");
            _Player = _VR_Player;
        }
        else
        {
            Debug.Log("VRモードではありません");
            _Player = _PC_Player;
        }

        for (int i = 0; i < _Colliders.Length; i++)
        {
            // 子要素の MeshRenderer コンポーネントがあれば無効にする
            foreach (Transform child in _Colliders[i].transform)
            {
                MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
                if (meshRenderer != null) { meshRenderer.enabled = false; }
            }
        }

        // 壁を消去
        foreach (GameObject obj in _ObstacleWalls)
        {
            obj.SetActive(false);
        }

    }

    // Update is called once per frame
    void Update()
    {

        if (_isNotifyingDanger)
        {
            _timer += Time.deltaTime;
            // スタート地点（分岐）からの距離に応じて、振動の大きさと鼓動の速さを変える
            float distance = Vector3.Distance(_Player.transform.position, _FirstBranch.transform.position);
            // 危険に近づくと段々早く
            float interval = (1 - distance / _maxDistanceOfFirstArea) + 0.3f;
            if (_timer >= interval)
            {
                NotifyDanger(distance);
                _timer = 0;
            }
        }
    }

    public void TriggerEnterFunc(string colName, string oppName)
    {
        // Debug.Log("enter: " + colName + ", opponent: " + oppName);
        if (oppName == "PC_Player" || oppName == "VR_Player")
        {
            switch (colName)
            {
                // Manage Area events
                case "MazeArea":
                    // set correct direction for branches
                    for (int i = 0; i < 2; i++)
                    {
                        int rnd = Random.Range(0, 2);
                        if (rnd == 0) { _correctDir[i] = (i == 0) ? "left" : "top"; }
                        else { _correctDir[i] = (i == 0) ? "right" : "bot"; }
                    }
                    // set instance related to correct route
                    if (_correctDir[0] == "left")
                    {
                        _maxDistanceOfFirstArea = Vector3.Distance(_SecondBranchLeft.transform.position, _FirstBranch.transform.position);

                    }
                    else if (_correctDir[0] == "right")
                    {
                        _maxDistanceOfFirstArea = Vector3.Distance(_SecondBranchRight.transform.position, _FirstBranch.transform.position);
                    }
                    Debug.Log("correct dir is: " + _correctDir[0] + " / " + _correctDir[1]);
                    // start loop sound serial
                    _SerialHandler.SendSerial("mazeloop", "neck", "loopstart");
                    break;
                // // first area
                // // 正解：お化けが後ろからついてくる。誤り：心臓の鼓動が早くなる
                case "LeftBotBotArea":
                    if (_correctDir[0] == "left") _SerialHandlerScript._isGhostStepArea = true;
                    else _isNotifyingDanger = true; break;
                case "RightBotBotArea":
                    if (_correctDir[0] == "right") _SerialHandlerScript._isGhostStepArea = true;
                    else _isNotifyingDanger = true; break;
                // // second area
                case "RightTopArea":
                case "LeftTopArea":
                    if (_correctDir[1] == "top") GhostInvitation(); break;
                case "TopArea":
                    GhostInvitation(); break;
                case "RightBotArea":
                case "LeftBotArea":
                    if (_correctDir[1] == "bot") GhostInvitation(); break;
                // Manage Fall events
                // // First Area 失敗で落とす
                case "FallTriggerLeft":
                    if (_correctDir[0] != "left") FallPlayer(); break;
                case "FallTriggerRight":
                    if (_correctDir[0] != "right") FallPlayer(); break;
                // // Second Area 失敗で落とす、正解を通過したら壁を出現させる
                case "FallTriggerRightTop":
                case "FallTriggerLeftTop":
                    if (_correctDir[1] != "top") FallPlayer();
                    else SpawnWall(); break;
                case "FallTriggerRightBot":
                case "FallTriggerLeftBot":
                    if (_correctDir[1] != "bot") FallPlayer();
                    else SpawnWall(); break;
                // Manage Branch events
                case "FirstBranch":
                    InvokeRepeating("NotifyLeftOrRight", 0, 1); break;
                case "SecondBranchLeft":
                    ChangeLayersRecursively(_LeftDeadend.transform, "throughPlayer"); break;
                case "SecondBranchRight":
                    ChangeLayersRecursively(_RightDeadend.transform, "throughPlayer"); break;

                // Manage last trigger events
                case "GhostTrigger1":
                    if (_finalGhostFlags[0])
                        _SerialHandler.SendSerial("GhostTrigger1", "neck", "oneshot");
                    _finalGhostFlags[0] = false; break;
                case "GhostTrigger2":
                    if (_finalGhostFlags[1])
                        _SerialHandler.SendSerial("GhostTrigger2", "neck", "oneshot");
                    _finalGhostFlags[1] = false; break;
                case "GhostTrigger3":
                    if (_finalGhostFlags[2])
                        _SerialHandler.SendSerial("GhostTrigger3", "neck", "oneshot");
                    _finalGhostFlags[2] = false; break;
            }
        }
    }

    public void TriggerExitFunc(string colName, string oppName)
    {
        // Debug.Log("exit: " + colName + ", opponent: " + oppName);
        switch (colName)
        {
            case "MazeArea":
                Debug.Log("exit maze area");
                // stop loop
                _SerialHandler.SendSerial("mazeloop", "neck", "loopstop"); break;
            case "FirstBranch":
                CancelInvoke("NotifyLeftOrRight"); break;
            case "LeftBotBotArea":
            case "RightBotBotArea":
                _isNotifyingDanger = false;
                _SerialHandlerScript._isGhostStepArea = false; break;
        }
    }

    private void FallPlayer()
    {
        ChangeLayersRecursively(_Floor.transform, "throughPlayer");
    }

    /// <summary>
    /// 奥のエリアで正解を踏んだら、不正解の道に壁を出現させる
    /// </summary>
    private void SpawnWall()
    {
        foreach (GameObject obj in _ObstacleWalls)
        {
            if (_correctDir[1] == "top" && obj.name.Contains("BotWall") ||
            _correctDir[1] == "bot" && obj.name.Contains("TopWall"))
                obj.SetActive(true);
        }
    }

    // functions for repeat in areas
    private void NotifyLeftOrRight()
    {
        // Debug.Log("in the FirstBranch");
        if (_correctDir[0] == "left")
        {
            Debug.Log("turn left");
            _SerialHandler.SendSerial("leftNotify", "neck");
        }
        else if (_correctDir[1] == "right")
        {
            Debug.Log("turn right");
            _SerialHandler.SendSerial("rightNotify", "neck");
        }
    }

    // 危険を伝える。無視すると落ちる。心臓の鼓動を一定時間ごとに再生
    private void NotifyDanger(float distance)
    {
        Debug.Log("NotifyDanger");
        // 距離をコンソールに出力する
        Debug.Log("Distance to target: " + distance);
        // volume should be within 0--1
        float volume = distance / _maxDistanceOfFirstArea;
        _SerialHandler.SendSerial("heartbeat", "neck", "oneshot", volume);
    }


    // 奥の二股で利用
    private void GhostInvitation()
    {

    }



    /// utilities /
    // 子要素のレイヤー全てを変更する関数
    public void ChangeLayersRecursively(Transform trans, string layerName)
    {
        trans.gameObject.layer = LayerMask.NameToLayer(layerName);
        foreach (Transform child in trans)
        {
            ChangeLayersRecursively(child, layerName);
        }
    }

}