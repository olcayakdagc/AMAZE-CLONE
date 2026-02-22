#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SaveTools
{
    private const string SaveFileName = "save.save";

    [MenuItem("Tools/Reset Save")]
    public static void ResetSave()
    {
        if (!EditorUtility.DisplayDialog("Reset Save", "Delete local save file?", "Delete", "Cancel"))
            return;

        string path = Path.Combine(Application.persistentDataPath, "saves", SaveFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"Save deleted: {path}");
        }
        else
        {
            Debug.Log($"Save not found: {path}");
        }
    }
}
#endif
