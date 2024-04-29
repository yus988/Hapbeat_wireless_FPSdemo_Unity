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
    [SerializeField] GameObject _FirstFallTrigger;
    [SerializeField] GameObject _TopArea;

    [SerializeField] GameObject _GhostObject;

    private GameObject _SecondBranch;

    [Header("Attach Parent of Gimic Walls")]
    [SerializeField] GameObject _GimicWalls;

    // 子要素の MeshRenderer コンポーネントを無効にするため
    [Header("透明にしたいコライダーを参照")]
    [SerializeField] GameObject[] _Colliders;

    private float _lastTriggerTime;
    // NotifyDanger で distance を0~1までにするために利用する、
    // それぞれの区間の最大距離
    private float _maxDistanceOfFirstArea;
    private float _maxDistanceOfSecondArea;

    public SerialHandler _SerialHandlerScript;
    public float _repeatInterval = 1f; // 実行間隔

    [Header("Ghost params")]
    public Material GhostMaterial;
    public float rotationSpeed = 1.0f;
    public float movementSpeed = 1.0f;
    public float scaleSpeed = 0.1f;
    public float finalScale = 100f;
    // 色変化
    public Color startColor = Color.red; // 開始色
    public Color endColor = Color.blue; // 終了色
    public float durationOfColorVariant = 2.0f; // 変化にかかる時間（秒）
    private float startTimeColor;


    // params for notifyingdanger
    private bool _isNotifyingDanger = false;
    private float _timer = 0f;

    // last flag
    private bool[] _finalGhostFlags = { true, true, true, true };

    string[] _correctDir = { "", "" };
    // Start is called before the first frame update
    void Start()
    {
        _SerialHandler = GameObject.Find("SerialHandler").GetComponent<SerialHandler>();
        // make invisible of all collidars

        if (XRSettings.enabled) { Debug.Log("VRモードです"); _Player = _VR_Player; }
        else { Debug.Log("VRモードではありません"); _Player = _PC_Player; }

        for (int i = 0; i < _Colliders.Length; i++)
        {
            // 子要素の MeshRenderer コンポーネントがあれば無効にする
            foreach (Transform child in _Colliders[i].transform)
            {
                MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
                if (meshRenderer != null) { meshRenderer.enabled = false; }
            }
        }

        // 壁を消去（リセット）
        foreach (Transform child in _GimicWalls.transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_isNotifyingDanger)
        {
            _timer += Time.deltaTime;
            // スタート地点（分岐）からの距離に応じて、振動の大きさと鼓動の速さを変える
            float distance; float interval;
            if (_Player.transform.position.z < _FirstFallTrigger.transform.position.z)
            {
                Debug.Log("Player is in First Area");
                distance = Vector3.Distance(_Player.transform.position, _FirstBranch.transform.position);
                interval = (1 - distance / _maxDistanceOfFirstArea) + 0.3f;
            }
            else
            {
                Debug.Log("Player is in Second Area");
                distance = Vector3.Distance(_Player.transform.position, _SecondBranch.transform.position);
                interval = (1 - distance / _maxDistanceOfSecondArea) + 0.3f;
            }
            // 危険に近づくと段々早く
            if (_timer >= interval)
            {
                NotifyDanger(distance);
                _timer = 0;
            }
        }

        // お化けの追従方法
        if (_finalGhostFlags[2] == false)
        {
            // プレイヤーの方向を計算
            Vector3 direction = _Player.transform.position - _GhostObject.transform.position;

            // 対象の方向をプレイヤーの方向に滑らかに回転させる
            if (Vector3.Distance(_Player.transform.position, _GhostObject.transform.position) > 0.01)
            {
                _GhostObject.transform.LookAt(_Player.transform);
                // 対象を前方に移動させる
                _GhostObject.transform.Translate(Vector3.forward * movementSpeed * Time.deltaTime);
            }

            // 対象を拡大させる
            Vector3 currentScale = _GhostObject.transform.localScale;
            Vector3 targetScale = new Vector3(finalScale, finalScale, finalScale); // 目標のスケール
            _GhostObject.transform.localScale = Vector3.Lerp(currentScale, targetScale, scaleSpeed * Time.deltaTime);
        }

        if (_finalGhostFlags[3] == false)
        {
            float t = (Time.time - startTimeColor) / durationOfColorVariant; // 変化の進行度（0.0 ～ 1.0）
            GhostMaterial.color = Color.Lerp(startColor, endColor, t);

            if (t >= 1.0f)
            {
                // 変化が完了したら、開始色と終了色を入れ替えて再度変化させる
                Color temp = startColor;
                startColor = endColor;
                endColor = temp;
                startTimeColor = Time.time;
            }
        }

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
                    _SecondBranch = (_correctDir[0] == "left") ?
                     _SecondBranchLeft : _SecondBranchRight;
                    _maxDistanceOfFirstArea = Vector3.Distance(_SecondBranch.transform.position, _FirstBranch.transform.position);
                    _maxDistanceOfSecondArea = Vector3.Distance(_SecondBranch.transform.position, _TopArea.transform.position);
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
                    if (_correctDir[1] == "top") GhostInvitation();
                    else _isNotifyingDanger = true; break;
                case "TopArea":
                    GhostInvitation(); break;
                case "RightBotArea":
                case "LeftBotArea":
                    if (_correctDir[1] == "bot") GhostInvitation();
                    else _isNotifyingDanger = true; break;

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
                case "SecondBranchRight":
                    foreach (Transform child in _GimicWalls.transform)
                    {
                        GameObject obj = child.gameObject;
                        if (_correctDir[0] == "left")
                        {
                            if (obj.name == "TransLeftDeadend") obj.SetActive(true);
                            if (obj.name == "RightDeadend") obj.SetActive(true);
                        }
                        else if (_correctDir[0] == "right")
                        {
                            if (obj.name == "TransRightDeadend") obj.SetActive(true);
                            if (obj.name == "LeftDeadend") obj.SetActive(true);
                        }
                    }
                    break;
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
                    // case "GhostTriggerTouch":
                    if (_finalGhostFlags[2])
                    {
                        _SerialHandler.SendSerial("GhostTrigger3", "neck", "oneshot");
                        Color tmpColor = GhostMaterial.color;
                        tmpColor.a = 1f;
                        GhostMaterial.color = tmpColor;
                    }

                    _finalGhostFlags[2] = false; break;
                case "Ghost":
                    _finalGhostFlags[3] = false; break;
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
                ResetAll();
                // stop loop
                _SerialHandler.SendSerial("mazeloop", "neck", "loopstop"); break;
            case "FirstBranch":
                CancelInvoke("NotifyLeftOrRight"); break;
            case "LeftBotBotArea":
            case "RightBotBotArea":
                _isNotifyingDanger = false;
                _SerialHandlerScript._isGhostStepArea = false; break;


            case "RightTopArea":
            case "LeftTopArea":
            case "RightBotArea":
            case "LeftBotArea":
                _isNotifyingDanger = false; break;
            // case "TopArea":
            case "Ghost":
                _finalGhostFlags[3] = true; break;
                break;
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
        foreach (Transform child in _GimicWalls.transform)
        {
            GameObject obj = child.gameObject;
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

    // 迷路から抜けた時に実行
    private void ResetAll()
    {
        for (int i = 0; i < _finalGhostFlags.Length; i++)
        { _finalGhostFlags[i] = true; }
        foreach (Transform child in _GimicWalls.transform)
        {
            child.gameObject.SetActive(false);
        }
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