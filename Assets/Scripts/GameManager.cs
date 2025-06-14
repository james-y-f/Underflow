using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
public enum Scene
{
    Start,
    Battle,
    Pause,
    HowTo,
    Levels,
    Credits
}
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public Scene CurrentScene
    {
        get; private set;
    }
    int LevelsUnlocked;
    CameraMotor MainCamera;
    [SerializeField] List<Level> Levels;
    [SerializeField] List<ButtonController> LevelSelectButtons;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        CurrentScene = Scene.Start;
        MainCamera = Camera.main.GetComponent<CameraMotor>();
        Assert.IsNotNull(MainCamera);
        Assert.IsNotNull(Levels);
        Assert.IsNotNull(LevelSelectButtons);
        Assert.IsTrue(Levels.Count >= LevelSelectButtons.Count);
        LevelsUnlocked = 0;
    }

    void Start()
    {
        MainCamera.Move(Constants.StartCameraPosition);
        LevelSelectButtons[0].SetInteractible(true);
        for (int i = 1; i < LevelSelectButtons.Count; i++)
        {
            LevelSelectButtons[i].SetInteractible(false);
        }
        for (int i = 0; i < LevelSelectButtons.Count; i++)
        {
            int levelNumber = i; // this is somehow necessary
            LevelSelectButtons[levelNumber].OnClick.AddListener(() => LoadLevel(levelNumber));
        }
    }

    public void UnlockLevel(int level)
    {
        level = Math.Min(level, LevelSelectButtons.Count - 1);
        DebugConsole.Instance.Log($"Unlocking Level {level}");
        for (int i = LevelsUnlocked; i <= level; i++)
        {
            LevelSelectButtons[i].SetInteractible(true);
        }
        LevelsUnlocked = Math.Min(level, LevelsUnlocked);
    }

    public void MoveToStart()
    {
        CurrentScene = Scene.Start;
        MainCamera.Move(Constants.StartCameraPosition);
    }

    public void MoveToBattle()
    {
        CurrentScene = Scene.Battle;
        MainCamera.Move(Constants.BattleCameraPosition);
    }

    public void MoveToPause()
    {
        CurrentScene = Scene.Pause;
        MainCamera.Move(Constants.PauseCameraPositon);
    }

    public void MoveToHowTo()
    {
        CurrentScene = Scene.HowTo;
        MainCamera.Move(Constants.HowToCameraPosition);
    }

    public void MoveToLevels()
    {
        CurrentScene = Scene.Levels;
        MainCamera.Move(Constants.TransitionCameraPosition);
    }

    public void MoveToCredits()
    {
        CurrentScene = Scene.Credits;
        MainCamera.Move(Constants.CreditsCameraPosition);
    }

    void LoadLevel(int levelNumber)
    {
        Debug.Log($"Trying to load level {levelNumber}");
        Assert.IsTrue(Levels.Count > levelNumber);
        MoveToBattle();
        BattleManager.Instance.LoadLevel(Levels[levelNumber]);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
