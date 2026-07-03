using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-10)]
public class LevelSelectMenu : MonoBehaviour
{
    [SerializeField] LevelEntry level1Entry;
    [SerializeField] LevelSelectCell[] cells;

    void Awake()
    {
        if (cells == null || cells.Length == 0)
            cells = GetComponentsInChildren<LevelSelectCell>(true);

        ApplyLevelEntries();
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ApplyLevelEntries()
    {
        if (cells == null)
            return;

        foreach (LevelSelectCell cell in cells)
        {
            if (cell == null)
                continue;

            if (cell.LevelIndex == 1 && level1Entry != null)
                cell.LevelEntry = level1Entry;

            cell.ApplyVisuals();
        }
    }
}
