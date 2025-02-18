﻿using UnityEngine;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour {
    public static Main SharedInstance;

    void Awake() {
        SharedInstance = this;
    }

    public void GameOver() {
        SceneManager.LoadScene("GameOver Screen");
    }

    public void Win() {
        Score.SharedInstance.saveScore();
        SceneManager.LoadScene("Win Screen");
    }
}
