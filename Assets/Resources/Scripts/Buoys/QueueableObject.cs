﻿using UnityEngine;

public class QueueableObject : MonoBehaviour {
    public GameObject objectInQueue;
    public Vector3 spawnPos = new Vector3(-1.83333f, 3f, 0f);

    void Start() {
        objectInQueue.SetActive(false);
    }

    public void OnTriggerEnter2D(Collider2D collider) {
        if (collider.name.Equals("Queue Collider")) {
            objectInQueue.transform.position = spawnPos;
            objectInQueue.SetActive(true);
            Destroy(gameObject);
        }
    }
}
