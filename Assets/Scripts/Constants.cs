using UnityEngine;

public static class Constants
{
    // tags
    public const string PlayerCardTag = "PlayerCard";
    public const string EnemyCardTag = "EnemyCard";

    public const string PlayerStackTag = "PlayerStack";
    public const string EnemyStackTag = "EnemyStack";

    public const string ExecuteNextButtonTag = "ExecuteNextButton";

    public const string EndTurnButtonTag = "EndTurnButton";

    public const string ExecuteAllButtonTag = "ExecuteAllButton";
    public const string PauseButtonTag = "PauseButton";
    public const string LevelSelectButtonTag = "BackButton";
    public const string RestartButtonTag = "RestartButton";
    // layers
    public const string PlayerCardsLayerName = "PlayerCards";
    public const string EnemyCardsLayerName = "EnemyCards";

    // Camera Movement 
    public const float DefaultCameraMoveSpeedFactor = 7;
    public const float CameraHeight = 10f;
    public static Vector3 BattleCameraPosition
    {
        get { return new Vector3(-20f, CameraHeight, -12f); }
        private set { }
    }

    public static Vector3 StartCameraPosition
    {
        get { return new Vector3(0f, CameraHeight, 0f); }
        private set { }
    }

    public static Vector3 CreditsCameraPosition
    {
        get { return new Vector3(0, CameraHeight, 12f); }
        private set { }
    }

    public static Vector3 TransitionCameraPosition
    {
        get { return new Vector3(-20f, CameraHeight, 0f); }
        private set { }
    }
    public static Vector3 HowToCameraPosition
    {
        get { return new Vector3(0f, CameraHeight, -12f); }
        private set { }
    }
    public static Vector3 PauseCameraPositon
    {
        get { return new Vector3(-40f, CameraHeight, -12f); }
        private set { }
    }

    // Card Movement    
    public const float PosTolerance = 0.02f;
    public const float DefaultCardMoveSpeedFactor = 5;
    public const float BaseHeight = 0.1f;
    public const float HoverHeight = 0.2f;
    public const float CardHeight = 0.1f;

    public static Quaternion FaceUp
    {
        get { return Quaternion.Euler(0, 0, 0); }
        private set { }
    }

    public static Quaternion FaceDown
    {
        get { return Quaternion.Euler(0, 0, 180); }
        private set { }
    }

    // Colors
    // TODO: finalize colors
    public const float DefaultColorFadeSpeed = 0.05f;
    public const float DefaultColorFlashSpeed = 0.1f;
    public const float ColorTolerance = 0.02f;
    public const float DimFactor = 0f;
    public static Color PlayerCardColor
    {
        get { return new Color(0.1f, 0.1f, 0.3f, 1f); }
        private set { }
    }
    public static Color EnemyCardColor
    {
        get { return new Color(0.3f, 0.1f, 0.1f, 1f); }
        private set { }
    }

    public static Color UnswappableColor
    {
        get { return new Color(0.15f, 0.15f, 0.15f, 1f); }
        private set { }
    }

    public static Color SelectionHighlight
    {
        get { return new Color(0.1f, 0.1f, 0.1f, 0f); }
        private set { }
    }

    public static Color FlashHighlight
    {
        get { return new Color(0.95f, 0.95f, 0.95f, 1f); }
        private set { }
    }

    public static Color DeletionEffectColor = Color.red;
    public static Color ExecutionDoneColor = Color.green;
    public static Color AddEffectColor = Color.yellow;

    // Energy
    public static Vector3 EnergySpawnDisplacement
    {
        get { return new Vector3(-0.5f, 0f, 0f); }
        private set { }
    }

    public static Vector3 BaseEnergySpawnHeightAdjustment
    {
        get { return new Vector3(0f, 0.1f, 0f); }
        private set { }
    }

    // Animation
    public const float StandardActionDelay = 0.5f;
    public const float FastActionDelay = 0.25f;
}