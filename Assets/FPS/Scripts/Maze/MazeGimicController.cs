using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Gameplay;
using Unity.FPS;
using UnityEngine.XR;
using System;
using UnityEngine.Events;
using Unity.FPS.Game;
using UnityEngine.SceneManagement;

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
    public Material _GhostMaterial;
    public float _rotationSpeed = 1.0f;
    public float _movementSpeed = 2.0f;
    public float _scaleSpeed = 0.05f;
    public float _finalScale = 10f;
    // 経過時間を追跡するための変数を追加
    private float _elapsedTime = 0f;
    public float _speedIncreaseRate = 0.1f; // 時間経過ごとの速度増加量

    // 色変化
    public Color _startColor = Color.red; // 開始色
    public Color _endColor = Color.blue; // 終了色
    private float _startTimeColor;
    public float _durationOfColorVariant = 1.0f;
    public float _fadeDuration = 3.0f;
    public float _riseHeight = 5.0f;
    private bool _isFading = false;
    private Vector3 _originalPosition;

    // params for notifying danger
    private bool _isNotifyingDanger = false;
    private float _timer = 0f;
    private Coroutine _notifyCoroutine;



    private Coroutine _notifyDangerCoroutine; // クラスレベルで宣言

    // last flag
    private bool[] _finalGhostFlags = { true, true, true, true };

    string[] _correctDir = { "", "" };

    // Start is called before the first frame update
    void Start()
    {
        _SerialHandler = GameObject.Find("SerialHandler").GetComponent<SerialHandler>();

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
            float distance;
            float interval; // 0.3 -- 1.0s の間。

            if (_Player.transform.position.z < _FirstFallTrigger.transform.position.z)
            {
                // Debug.Log("Player is in First Area");
                distance = Vector3.Distance(_Player.transform.position, _FirstBranch.transform.position);
                interval = Mathf.Lerp(2.0f, 1f, distance / _maxDistanceOfFirstArea);
            }
            else
            {
                // Debug.Log("Player is in Second Area");
                distance = Vector3.Distance(_Player.transform.position, _SecondBranch.transform.position);
                interval = Mathf.Lerp(2.0f, 1f, distance / _maxDistanceOfSecondArea);
            }
            // 危険に近づくと段々早く
            if (_timer >= interval)
            {
                Debug.Log("interval is " + interval + "seconds");
                NotifyDanger(distance);
                if (_notifyDangerCoroutine != null)
                {
                    StopCoroutine(_notifyDangerCoroutine);
                }
                float beatInterval = interval / 4f;

                _notifyDangerCoroutine = StartCoroutine(DelayedNotify(beatInterval, () => NotifyDanger(distance)));
                _timer = 0;
            }
        }

        // hearbeat2
        // お化けの追従方法
        if (_finalGhostFlags[2] == false)
        {
            // // 心臓の鼓動再生
            _timer += Time.deltaTime;
            float distance;
            distance = Vector3.Distance(_Player.transform.position, _GhostObject.transform.position);
            // intervalを距離に基づいて計算し、最大1.5f、最小0.3fに制限
            float interval = Mathf.Clamp(distance * 0.1f, 0.5f, 1.5f);
            if (_timer >= interval && _finalGhostFlags[3] == true)
            {
                // 距離に依存する変数を最大0.5、最小0.1の範囲内で設定
                float volume = Mathf.Clamp(0.5f - distance, 0.1f, 0.5f);
                _SerialHandler.SendSerial("ghostcoming", "neck", "oneshot", volume);

                if (_notifyDangerCoroutine != null)
                {
                    StopCoroutine(_notifyDangerCoroutine);
                }
                float beatInterval = interval / 4f;

                _notifyDangerCoroutine = StartCoroutine(DelayedNotify(beatInterval, () =>
                {
                    _SerialHandler.SendSerial("ghostcoming", "neck", "oneshot", volume);
                }));

                _timer = 0;
            }
            // // お化けが近づいてくる
            // プレイヤーの方向を計算
            _elapsedTime += Time.deltaTime; // 経過時間を更新
            Vector3 direction = _Player.transform.position - _GhostObject.transform.position;
            // 対象の方向をプレイヤーの方向に滑らかに回転させる
            if (Vector3.Distance(_Player.transform.position, _GhostObject.transform.position) > 0.01)
            {
                _GhostObject.transform.LookAt(_Player.transform);
                // 対象を前方に移動させる
                float currentSpeed = _movementSpeed + (_elapsedTime * _speedIncreaseRate);
                _GhostObject.transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
            }
            // 対象を拡大させる
            Vector3 currentScale = _GhostObject.transform.localScale;
            Vector3 targetScale = new Vector3(_finalScale, _finalScale, _finalScale); // 目標のスケール
            _GhostObject.transform.localScale = Vector3.Lerp(currentScale, targetScale, _scaleSpeed * Time.deltaTime);
        }

        // お化けに食べられた時
        if (_finalGhostFlags[3] == false)
        {
            if (!_isFading)
            {
                // プレイヤーに触れた時に一度だけ実行
                _originalPosition = new Vector3(_Player.transform.position.x, _GhostObject.transform.position.y, _Player.transform.position.z);
                _startTimeColor = Time.time; // 開始時間を記録
                _isFading = true; // フラグを立てる
            }

            float elapsed = Time.time - _startTimeColor;
            if (elapsed <= 2.0f)
            {
                float t = (elapsed % _durationOfColorVariant) / _durationOfColorVariant; // 変化の進行度（0.0 ～ 1.0）
                _GhostMaterial.color = Color.Lerp(_startColor, _endColor, t);
            }
            else
            {
                // 2秒経過後、透明化を開始
                float t = (elapsed - 2.0f) / _fadeDuration; // 進行度（0.0 ～ 1.0）
                t = Mathf.Clamp01(t); // tを0.0から1.0の範囲に制限

                // 徐々に透明に
                Color newColor = _GhostMaterial.color;
                newColor.a = Mathf.Lerp(1.0f, 0.0f, t);
                _GhostMaterial.color = newColor;

                // 透明化が完了したら非アクティブにし、入口に戻す（シーンの再リロード）
                if (t >= 1.0f)
                {
                    _GhostObject.SetActive(false);
                    SceneManager.LoadScene("MainScene");
                    // EventManager.Broadcast(Events.PlayerDeathEvent);
                }
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
                        int rnd = UnityEngine.Random.Range(0, 2);
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
                    break;
                case "MazeEntrance":
                    _SerialHandler.SendSerial("mazeloop", "neck", "loopstart");
                    break;
                // // first area
                // // 正解：お化けが後ろからついてくる。誤り：心臓の鼓動が早くなる
                case "LeftBotBotArea":
                    // 戻ってきたら足音を復活
                    _SerialHandlerScript._disableStepFeedBack = false;
                    if (_correctDir[0] == "left") _SerialHandlerScript._isGhostStepArea = true;
                    else _isNotifyingDanger = true;
                    break;
                case "RightBotBotArea":
                    // 戻ってきたら足音を復活
                    _SerialHandlerScript._disableStepFeedBack = false;
                    if (_correctDir[0] == "right") _SerialHandlerScript._isGhostStepArea = true;
                    else _isNotifyingDanger = true;
                    break;
                // // second area
                case "RightTopArea":
                case "LeftTopArea":
                    if (_correctDir[1] == "top")
                        _notifyCoroutine = StartCoroutine(RandomlyInvoke(GhostInvitation, 2f, 4f));
                    else _isNotifyingDanger = true;
                    break;
                case "TopArea":
                    break;
                case "RightBotArea":
                case "LeftBotArea":
                    if (_correctDir[1] == "bot")
                        _notifyCoroutine = StartCoroutine(RandomlyInvoke(GhostInvitation, 2f, 4f));
                    else _isNotifyingDanger = true;
                    break;
                case "CenterArea":
                    break;
                // Manage Fall events
                // // First Area 失敗で落とす
                case "FallTriggerLeft":
                    if (_correctDir[0] != "left") FallPlayer();
                    break;
                case "FallTriggerRight":
                    if (_correctDir[0] != "right") FallPlayer();
                    break;
                // // Second Area 失敗で落とす、正解を通過したら壁を出現させる
                case "FallTriggerRightTop":
                case "FallTriggerLeftTop":
                    if (_correctDir[1] != "top") FallPlayer();
                    else SpawnWall();
                    break;
                case "FallTriggerRightBot":
                case "FallTriggerLeftBot":
                    if (_correctDir[1] != "bot") FallPlayer();
                    else SpawnWall();
                    break;
                // Manage Branch events
                case "FirstBranch":
                    _notifyCoroutine = StartCoroutine(RandomlyInvoke(NotifyLeftOrRight, 2f, 3f));
                    break;
                case "SecondBranchLeft":
                case "SecondBranchRight":
                    // 奥以降は足音を消す
                    _SerialHandlerScript._disableStepFeedBack = true;

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
                case "TransLeftDeadend":
                case "TransRightDeadend":
                    _SerialHandler.SendSerial("passwall", "neck", "oneshot");
                    break;
                // Manage last trigger events
                case "GhostTrigger1":
                    if (_finalGhostFlags[0])
                    {
                        _SerialHandler.SendSerial("ghostleft2right", "neck", "oneshot");
                        _finalGhostFlags[0] = false;
                    }
                    break;
                case "GhostTrigger2":
                    if (_finalGhostFlags[1])
                    {
                        _SerialHandler.SendSerial("ghostright2left", "neck", "oneshot");
                        _finalGhostFlags[1] = false;
                    }
                    break;
                case "GhostTrigger3":
                    // case "GhostTriggerTouch":
                    if (_finalGhostFlags[2])
                    {
                        Color tmpColor = _GhostMaterial.color;
                        tmpColor.a = 1f;
                        _GhostMaterial.color = tmpColor;
                        _finalGhostFlags[2] = false;
                    }
                    break;
                case "Ghost":
                    _finalGhostFlags[3] = false;
                    _SerialHandler.SendSerial("ghosteat", "neck", "oneshot");
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
                break;
            case "MazeEntrance":
                _SerialHandler.SendSerial("mazeloop", "neck", "loopstop");
                break;
            case "FirstBranch":
                StopCoroutine(_notifyCoroutine);
                _notifyCoroutine = null;
                break;
            case "LeftBotBotArea":
            case "RightBotBotArea":
                _isNotifyingDanger = false;
                _SerialHandlerScript._isGhostStepArea = false;
                break;
            case "RightTopArea":
            case "LeftTopArea":
            case "RightBotArea":
            case "LeftBotArea":
                _isNotifyingDanger = false;
                StopCoroutine(_notifyCoroutine);
                _notifyCoroutine = null;
                break;
            // case "TopArea":
            case "Ghost":
                break;
            case "TransLeftDeadend":
            case "TransRightDeadend":
                // _SerialHandler.SendSerial("passwall", "neck", "loopstop");
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

    // 特定の関数をランダムな間隔で呼び出す汎用コルーチン
    private IEnumerator RandomlyInvoke(Action action, float minInterval, float maxInterval)
    {
        // 初回の遅延
        float initialDelay = UnityEngine.Random.Range(minInterval, maxInterval);
        yield return new WaitForSeconds(initialDelay);
        while (true)
        {
            action();
            float waitTime = UnityEngine.Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }

    // functions for repeat in areas
    private void NotifyLeftOrRight()
    {
        // Debug.Log("in the FirstBranch");
        if (_correctDir[0] == "left")
        {
            Debug.Log("turn left");
            _SerialHandler.SendSerial("leftnotify", "neck");
        }
        else if (_correctDir[0] == "right")
        {
            Debug.Log("turn right");
            _SerialHandler.SendSerial("rightnotify", "neck");
        }
    }

    // 危険を伝える。無視すると落ちる。心臓の鼓動を一定時間ごとに再生
    private void NotifyDanger(float distance)
    {
        // 距離をコンソールに出力する
        Debug.Log("Distance to target: " + distance);
        // volume should be within 0--1
        // float volume = distance / _maxDistanceOfFirstArea * 0.2f;
        float volume = 0.25f * Mathf.Exp(distance / _maxDistanceOfFirstArea - 1);
        _SerialHandler.SendSerial("heartbeat", "neck", "oneshot", volume);
    }

    // 奥の二股で利用
    private void GhostInvitation()
    {
        _SerialHandler.SendSerial("ghostinvite", "neck", "oneshot");
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

    private IEnumerator DelayedNotify(float waitTime, Action callback)
    {
        yield return new WaitForSeconds(waitTime);
        callback();
    }

}
