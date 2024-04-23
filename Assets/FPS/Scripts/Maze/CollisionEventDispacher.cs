using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Unity.FPS;

public class CollisionEventDispacher : MonoBehaviour
{
    public MazeGimicController maze;
    void Start()
    {
        maze = maze.GetComponent<MazeGimicController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        maze.triggerEnterFunc(this.name, other.name);
    }

    private void OnTriggerExit(Collider other)
    {
        maze.triggerExitFunc(this.name, other.name);
    }

    // private void OnCollisionEnter(Collision collision)
    // {
    //     _OnColliderEvent.Invoke(collision.collider);
    // }

}