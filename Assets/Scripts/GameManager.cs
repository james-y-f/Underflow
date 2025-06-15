using System;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;
public enum Scene
{
    Start,
    Battle,
    Pause,
    Levels,
    Credits,
    Victory
}
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public Scene CurrentScene
    {
        get; private set;
    }
    int LevelsUnlocked;
    public int MaxLevel
    {
        get { return LevelSelectButtons.Count; }
        private set { }
    }
    CameraMotor MainCamera;
    [SerializeField] List<Level> Levels;
    [SerializeField] List<ButtonController> LevelSelectButtons;
    [SerializeField] AudioClip MenuMusic;
    [SerializeField] AudioClip BattleMusic;
    [SerializeField] AudioClip VictorySound;
    AudioSource BackgroundMusic;

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

        Assert.IsNotNull(MenuMusic);
        Assert.IsNotNull(BattleMusic);
        Assert.IsNotNull(VictorySound);

        BackgroundMusic = Camera.main.GetComponent<AudioSource>();
        Assert.IsNotNull(BackgroundMusic);
    }

    void Start()
    {
        Cursor.visible = true;
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
        SwitchMusic(MenuMusic);
    }

    public void MoveToBattle()
    {
        CurrentScene = Scene.Battle;
        MainCamera.Move(Constants.BattleCameraPosition);
        SwitchMusic(BattleMusic);
    }

    public void MoveToPause()
    {
        CurrentScene = Scene.Pause;
        MainCamera.Move(Constants.PauseCameraPositon);
    }

    public void MoveToLevels()
    {
        CurrentScene = Scene.Levels;
        MainCamera.Move(Constants.TransitionCameraPosition);
        SwitchMusic(MenuMusic);
    }

    public void MoveToCredits()
    {
        CurrentScene = Scene.Credits;
        MainCamera.Move(Constants.CreditsCameraPosition);
    }

    public void MoveToVictory()
    {
        CurrentScene = Scene.Victory;
        MainCamera.Move(Constants.VictoryCameraPositon);
        BackgroundMusic.enabled = false;
        BackgroundMusic.loop = false;
        BackgroundMusic.clip = VictorySound;
        BackgroundMusic.enabled = true;
        BackgroundMusic.Play();
    }

    void LoadLevel(int levelNumber)
    {
        Debug.Log($"Trying to load level {levelNumber}");
        Assert.IsTrue(Levels.Count > levelNumber);
        MoveToBattle();
        BattleManager.Instance.LoadLevel(Levels[levelNumber]);
    }

    void SwitchMusic(AudioClip newMusic)
    {
        if (BackgroundMusic.clip != newMusic)
        {
            BackgroundMusic.enabled = false;
            BackgroundMusic.loop = true;
            BackgroundMusic.clip = newMusic;
            BackgroundMusic.enabled = true;
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
