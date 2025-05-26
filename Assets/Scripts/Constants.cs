using UnityEngine;

public static class Constants
{
    // tags
    public const string PlayerCardTag = "PlayerCard";
    public const string EnemyCardTag = "EnemyCard";

    public const string PlayerStackTag = "PlayerStack";
    public const string EnemyStackTag = "EnemyStack";

    // layers
    public const string PlayerCardsLayerName = "PlayerCards";
    public const string EnemyCardsLayerName = "EnemyCards";

    // Card Movement    
    public const float PosTolerance = 0.05f;
    public const float DefaultCardMoveSpeedFactor = 5;
    public const float BaseHeight = 0.1f;
    public const float HoverHeight = 0.2f;

    // Colors
    // TODO: finalize colors
    public const float DefaultColorFadeSpeed = 0.05f;
    public const float DefaultColorFlashSpeed = 0.1f;
    public static float ColorTolerance = 0.02f;
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

    public static Color SelectionHighlight
    {
        get { return new Color(0.1f, 0.1f, 0.1f, 0f); }
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
}