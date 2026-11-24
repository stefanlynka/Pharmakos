using UnityEngine;

/// <summary>
/// One encounter's combat board: the ground under everything plus the two lanes. Lives at
/// Resources/Content/Boards/&lt;Name&gt;/&lt;Name&gt;.asset, where Name is the board name View maps the enemy deck to.
/// Made by Tools → Boards → Build Board Styles from the textures Tools/Boards/boards.py paints; see Docs/Boards.md.
/// View.LoadBackground uses it when present and falls back to the old Images/Backgrounds/Materials otherwise.
/// </summary>
[CreateAssetMenu(menuName = "Pharmakos/Board Style", fileName = "BoardStyle")]
public class BoardStyle : ScriptableObject
{
    [Tooltip("The large plane under the board (NewBackgroundHolder/Background).")]
    public Material Ground;

    [Tooltip("The player's lane (NewBackgroundHolder/PlayerBattleRow).")]
    public Material PlayerLane;

    [Tooltip("The enemy's lane (NewBackgroundHolder/AIBattleRow). Leave empty to use PlayerLane.")]
    public Material EnemyLane;

    [Header("Glow")]
    [Tooltip("How much the ground's glowing parts (embers, candles, torches, the sea) waver: 0 = steady.")]
    [Range(0f, 1f)]
    public float Flicker = 0f;

    [Tooltip("How fast they waver.")]
    public float FlickerSpeed = 1.5f;
}
