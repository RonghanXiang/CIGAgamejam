using UnityEngine;

[CreateAssetMenu(fileName = "LevelEntry", menuName = "Game/Level Entry")]
public class LevelEntry : ScriptableObject
{
    public int levelIndex = 1;
    public string displayName = "第一关";
    public string sceneName = "Level1";
    public string scenePath = "Assets/Levels/Level1/Level1.unity";
    public bool isAvailable = true;
    public Sprite thumbnail;
}
