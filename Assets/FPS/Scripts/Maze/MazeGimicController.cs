using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Gameplay;
using Unity.FPS;

public class MazeGimicController : MonoBehaviour
{
    private SerialHandler _serialhandler;
    string[] m_correctDir = { "", "" };
    // Start is called before the first frame update
    void Start()
    {
        _serialhandler = GameObject.Find("SerialHandler").GetComponent<SerialHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_correctDir[0] == "left")
        {

        }
    }

    public void triggerEnterFunc(string colName, string oppName)
    {
        // Debug.Log("enter: " + colName + ", opponent: " + oppName);
        if (colName == "MazeArea")
        {
            // set correct direction for branches
            for (int i = 0; i < 2; i++)
            {
                int rnd = Random.Range(0, 1);
                if (rnd == 0) { m_correctDir[i] = "left"; }
                else { m_correctDir[i] = "right"; }
            }
            // start loop sound serial
            _serialhandler.SendSerial("mazeloop", "neck", "loopstart");

        }

        // コライダー無くすテスト
        if (colName == "courner_1R")
        {

        }


    }

    public void triggerExitFunc(string colName, string oppName)
    {
        // Debug.Log("exit: " + colName + ", opponent: " + oppName);
        if (colName == "MazeArea")
        {
            // stop loop
            _serialhandler.SendSerial("mazeloop", "neck", "loopstop");
        }
    }

}