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
    [SerializeField] GameObject _LeftDeadend;
    [SerializeField] GameObject _RightDeadend;
    [SerializeField] GameObject _Floor;
    [SerializeField] GameObject _FirstBranch;
    [SerializeField] GameObject _SecondBranchLeft;
    [SerializeField] GameObject _SecondBranchRight;
    private GameObject _SecondBranch;

    // 子要素の MeshRenderer コンポーネントを無効にするため
    [Header("透明にしたいコライダーを参照")]
    [SerializeField] GameObject[] _Colliders;

    private float _lastTriggerTime;
    private float _maxDistanceOfFirstArea;

    public float _repeatInterval = 1f; // 実行間隔

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

    }

    // Update is called once per frame
    void Update()
    {
    }

    public void TriggerEnterFunc(string colName, string oppName)
    {
        Debug.Log("enter: " + colName + ", opponent: " + oppName);
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
                case "LeftBotBotArea":
                    if (_correctDir[0] == "left") InvokeRepeating("NotifyDanger", 0, 1);
                    else GhostFootstep(); break;
                case "RightBotBotArea":
                    if (_correctDir[0] == "right") InvokeRepeating("NotifyDanger", 0, 1);
                    else GhostFootstep(); break;
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
                case "FallTriggerLeft":
                    if (_correctDir[0] != "left") FallPlayer(); break;
                case "FallTriggerRight":
                    if (_correctDir[0] != "right") FallPlayer(); break;
                // // 通過後はFallをDisable
                case "FallTriggerRightTop":
                case "FallTriggerLeftTop":
                    if (_correctDir[1] != "top") FallPlayer(); break;
                case "FallTriggerRightBot":
                case "FallTriggerLeftBot":
                    if (_correctDir[1] != "bot") FallPlayer(); break;
                // Manage Branch events
                case "FirstBranch":
                    InvokeRepeating("NotifyLeftOrRight", 0, 1); break;
                case "SecondBranchLeft":
                    ChangeLayersRecursively(_LeftDeadend.transform, "throughPlayer"); break;
                case "SecondBranchRight":
                    ChangeLayersRecursively(_RightDeadend.transform, "throughPlayer"); break;
                // Manage last trigger events
                case "GhostTrigger1":
                    break;
                case "GhostTrigger2":
                    break;
                case "GhostTrigger3":
                    break;
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
                CancelInvoke("NotifyDanger");
                CancelInvoke("GhostFootstep"); break;
        }
    }

    private void FallPlayer()
    {
        ChangeLayersRecursively(_Floor.transform, "throughPlayer");
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
    private void NotifyDanger()
    {
        Debug.Log("NotifyDanger");
        // スタート地点（分岐）からの距離に応じて大きさを変える
        float distance = Vector3.Distance(_Player.transform.position, _FirstBranch.transform.position);
        // 距離をコンソールに出力する
        Debug.Log("Distance to target: " + distance);
        // volume should be within 0--1
        float volume = distance / _maxDistanceOfFirstArea;
        _SerialHandler.SendSerial("heartbeat", "neck", "oneshot", volume);
    }

    // お化けが後ろからついてくる。最初の通路
    private void GhostFootstep()
    {
        Debug.Log("GhostFootstep");
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