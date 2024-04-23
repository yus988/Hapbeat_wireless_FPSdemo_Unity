using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Unity.FPS;

public class CollisionEventDispacher : MonoBehaviour
{
    private MazeGimicController Maze;
    void Start()
    {
        Maze = GameObject.Find("MazeGimicController").GetComponent<MazeGimicController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Maze.TriggerEnterFunc(this.name, other.name);
    }

    // private void OnTriggerStay(Collider other)
    // {
    //     Maze.triggerStayFunc(this.name, other.name);
    // }

    private void OnTriggerExit(Collider other)
    {
        Maze.TriggerExitFunc(this.name, other.name);
    }

    // private void OnCollisionEnter(Collision collision)
    // {
    //     _OnColliderEvent.Invoke(collision.collider);
    // }

}