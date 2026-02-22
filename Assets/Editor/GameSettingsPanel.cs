#if UNITY_EDITOR
using System.IO;
using Managers;
using UnityEditor;
using UnityEngine;

public class GameSettingsPanel : EditorWindow
{
    private const string SettingsFolder = "Assets/Settings";
    private const string SettingsFileName = "GameSettings.json";
    private const string SettingsAddress = "GameSettings";

    private GameSettingsData _data = new();
    private Vector2 _scroll;

    [MenuItem("Tools/Game Settings Panel")]
    public static void Open()
    {
        var w = GetWindow<GameSettingsPanel>("Game Settings");
        w.minSize = new Vector2(320, 160);
        w.Show();
    }

    private void OnEnable()
    {
        Load();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        GUILayout.Space(6);

        EditorGUILayout.LabelField("Game Settings (Global)", EditorStyles.boldLabel);
        GUILayout.Space(6);

        _data.ballSpeed = EditorGUILayout.FloatField("Ball Speed", _data.ballSpeed);
        _data.rollSpeedMultiplier = EditorGUILayout.FloatField("Roll Speed Multiplier", _data.rollSpeedMultiplier);
        _data.cameraYOffset = EditorGUILayout.FloatField("Camera Y Offset", _data.cameraYOffset);

        GUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Load"))
                Load();

            if (GUILayout.Button("Save"))
                Save();
        }

        GUILayout.Space(6);
        EditorGUILayout.HelpBox($"Path: {SettingsFolder}/{SettingsFileName}\nAddressables Address: {SettingsAddress}", MessageType.Info);
        EditorGUILayout.EndScrollView();
    }

    private void Load()
    {
        string folderAbs = Path.Combine(Application.dataPath, "Settings");
        string fileAbs = Path.Combine(folderAbs, SettingsFileName);

        if (!File.Exists(fileAbs))
            return;

        try
        {
            string json = File.ReadAllText(fileAbs);
            var loaded = JsonUtility.FromJson<GameSettingsData>(json);
            if (loaded != null)
                _data = loaded;
        }
        catch
        {
            // ignore parse errors, keep defaults
        }
    }

    private void Save()
    {
        if (!Directory.Exists(SettingsFolder))
            Directory.CreateDirectory(SettingsFolder);

        string path = Path.Combine(SettingsFolder, SettingsFileName);
        string json = JsonUtility.ToJson(_data, true);
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
    }
}
#endif
