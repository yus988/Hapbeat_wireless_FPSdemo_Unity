using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Gameplay;
using Unity.FPS;

public class MazeGimicController : MonoBehaviour
{
    private SerialHandler _serialhandler;

    // attach reference objects
    [SerializeField] GameObject LeftDeadend;
    [SerializeField] GameObject RightDeadend;
    [SerializeField] GameObject Level;
    [SerializeField] GameObject FirstBranch;
    [SerializeField] GameObject SecondBranchLeft;
    [SerializeField] GameObject SecondBranchRight;

    [SerializeField] GameObject Colliders;

    private float lastTriggerTime;
    public float repeatInterval = 1f; // 実行間隔

    string[] _correctDir = { "", "" };
    // Start is called before the first frame update
    void Start()
    {
        _serialhandler = GameObject.Find("SerialHandler").GetComponent<SerialHandler>();
        // make invisible of all collidars

        // 子要素の MeshRenderer コンポーネントがあれば無効にする
        foreach (Transform child in Colliders.transform)
        {
            MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
            if (meshRenderer != null) { meshRenderer.enabled = false; }
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void TriggerEnterFunc(string colName, string oppName)
    {
        // Debug.Log("enter: " + colName + ", opponent: " + oppName);
        switch (colName)
        {
            case "MazeArea":
                // set correct direction for branches
                for (int i = 0; i < 2; i++)
                {
                    int rnd = Random.Range(0, 1);
                    if (rnd == 0) { _correctDir[i] = (i == 0) ? "left" : "top"; }
                    else { _correctDir[i] = (i == 0) ? "right" : "bot"; }
                }
                // start loop sound serial
                _serialhandler.SendSerial("mazeloop", "neck", "loopstart");
                break;
            case "courner_1R":
                ChangeLayersRecursively(RightDeadend.transform, "throughPlayer"); break;
            case "courner_1L":
                ChangeLayersRecursively(LeftDeadend.transform, "throughPlayer"); break;
            case "FirstBranch":
                InvokeRepeating("NotifyLeftOrRight", 0, 1); break;
                // ChangeLayersRecursively(Level.transform, "throughPlayer"); break;
        }
    }

    public void TriggerExitFunc(string colName, string oppName)
    {
        // Debug.Log("exit: " + colName + ", opponent: " + oppName);
        switch (colName)
        {
            case "MazeArea":
                // stop loop
                _serialhandler.SendSerial("mazeloop", "neck", "loopstop"); break;
            case "FirstBranch":
                CancelInvoke("NotifyLeftOrRight"); break;
        }
    }

    // functions for repeat in areas
    private void NotifyLeftOrRight()
    {
        // Debug.Log("in the FirstBranch");
        if (_correctDir[0] == "left")
        {
            Debug.Log("turn left");
            _serialhandler.SendSerial("leftNotify", "neck");
        }
        else if (_correctDir[1] == "right")
        {
            Debug.Log("turn right");
            _serialhandler.SendSerial("rightNotify", "neck");
        }
    }

    // 危険を伝える。無視すると落ちる。心臓の鼓動を一定時間ごとに差異性
    private void NotifyDanger()
    {
        // スタート地点からの距離に応じて大きさを変える
        float hoge;

        // _serialhandler.SendSerial("heartbeat", "neck", hoge);
    }

    // お化けが後ろからついてくる。最初の通路
    private void GhostFootstep()
    {

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