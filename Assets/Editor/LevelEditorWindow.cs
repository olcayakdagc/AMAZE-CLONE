#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Managers.LevelSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class LevelEditorWindow : EditorWindow
{
    private LevelData _data;
    private readonly Stack<LevelData> _undo = new();
    private readonly Stack<LevelData> _redo = new();
    private bool _suppressHistory;

    private IntegerField _levelIdField;
    private IntegerField _widthField;
    private IntegerField _heightField;
    private EnumField _modeField;
    private Label _startLabel;
    private Label _validationLabel;
    private FloatField _autoGenDensityField;
    private Toggle _autoGenRandomDensityToggle;

    private ScrollView _gridScroll;
    private VisualElement _gridRoot;

    private const string MaterialsFolder = "Assets/Art/Materials/EditorMaterials";
    private readonly List<Material> _materials = new();

    private PopupField<Material> _groundMatPopup;
    private PopupField<Material> _wallMatPopup;
    private PopupField<Material> _paintMatPopup;
    private Image _groundPreview;
    private Image _wallPreview;
    private Image _paintPreview;

    private enum EditMode
    {
        ToggleCells,
        SetStartNode
    }

    [MenuItem("Tools/Level Editor")]
    public static void Open()
    {
        var w = GetWindow<LevelEditorWindow>("Level Editor");
        w.minSize = new Vector2(620, 560);
        w.Show();
    }

    private void OnEnable()
    {
        if (_data == null)
            _data = new LevelData(5, 5);

        _undo.Clear();
        _redo.Clear();

        EnsureStartNodeIsAlwaysAvailable();
        LoadMaterialsFromFolder();
        BuildUI();
        RefreshUIFromData();
        SyncMaterialUIFromData();
        RebuildGrid();
    }

    private void BuildUI()
    {
        rootVisualElement.Clear();

        rootVisualElement.style.flexDirection = FlexDirection.Column;
        rootVisualElement.style.paddingLeft = 10;
        rootVisualElement.style.paddingRight = 10;
        rootVisualElement.style.paddingTop = 10;
        rootVisualElement.style.paddingBottom = 10;

        var title = new Label("Level Editor");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 14;
        title.style.marginBottom = 8;
        rootVisualElement.Add(title);

        BuildSettingsPanel();

        var gridHeader = new VisualElement();
        gridHeader.style.flexDirection = FlexDirection.Row;
        gridHeader.style.justifyContent = Justify.SpaceBetween;
        gridHeader.style.alignItems = Align.Center;
        gridHeader.style.marginBottom = 6;
        rootVisualElement.Add(gridHeader);

        var gridTitle = new Label("Grid");
        gridTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        gridHeader.Add(gridTitle);

        _startLabel = new Label();
        gridHeader.Add(_startLabel);

        _validationLabel = new Label("Validation: not run");
        _validationLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        gridHeader.Add(_validationLabel);

        _gridScroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
        _gridScroll.style.flexGrow = 1;
        _gridScroll.style.marginBottom = 8;
        AddBorder(_gridScroll);
        rootVisualElement.Add(_gridScroll);

        _gridRoot = new VisualElement();
        _gridRoot.style.flexDirection = FlexDirection.Column;
        _gridRoot.style.paddingLeft = 10;
        _gridRoot.style.paddingTop = 10;
        _gridRoot.style.paddingBottom = 10;
        _gridScroll.Add(_gridRoot);

        var toolsRow = MakeWrapRow();
        toolsRow.style.marginBottom = 6;
        rootVisualElement.Add(toolsRow);

        toolsRow.Add(MakeButton("Fill All Available", () =>
        {
            FillAll(true);
            EnsureStartNodeIsAlwaysAvailable();
            RebuildGrid();
        }, 150));

        toolsRow.Add(MakeButton("Fill All Blocked", () =>
        {
            FillAll(false);
            EnsureStartNodeIsAlwaysAvailable();
            RebuildGrid();
        }, 140));

        toolsRow.Add(MakeButton("Invert", () =>
        {
            Invert();
            EnsureStartNodeIsAlwaysAvailable();
            RebuildGrid();
        }, 90));

        toolsRow.Add(MakeButton("Auto Generate", () =>
        {
            AutoGenerateLevel();
            EnsureStartNodeIsAlwaysAvailable();
            RebuildGrid();
            ValidateLevel();
        }, 140));

        var saveRow = MakeWrapRow();
        saveRow.style.marginBottom = 6;
        rootVisualElement.Add(saveRow);

        saveRow.Add(MakeButton("Undo", Undo, 80));
        saveRow.Add(MakeButton("Redo", Redo, 80));
        saveRow.Add(MakeButton("Validate", ValidateLevel, 90));
        saveRow.Add(MakeButton("Save JSON", SaveJsonAuto, 110));
        saveRow.Add(MakeButton("Load JSON", LoadJsonById, 110));

        var pathHint = new HelpBox(
            $"Auto path: Assets/Levels/Level_{{ID}}.json\nMaterials folder: {MaterialsFolder}",
            HelpBoxMessageType.None
        );
        rootVisualElement.Add(pathHint);
    }

    private void BuildSettingsPanel()
    {
        var settingsOuter = new VisualElement();
        settingsOuter.style.flexDirection = FlexDirection.Row;
        settingsOuter.style.justifyContent = Justify.Center;
        settingsOuter.style.marginBottom = 4;
        rootVisualElement.Add(settingsOuter);

        var settingsBox = new VisualElement();
        settingsBox.style.flexDirection = FlexDirection.Column;
        settingsBox.style.paddingTop = 4;
        settingsBox.style.paddingBottom = 4;
        settingsBox.style.paddingLeft = 6;
        settingsBox.style.paddingRight = 6;
        settingsBox.style.maxWidth = 560;
        settingsBox.style.width = Length.Percent(100);

        AddBorder(settingsBox);
        settingsOuter.Add(settingsBox);

        var row0 = MakeRow();
        settingsBox.Add(row0);

        _levelIdField = new IntegerField() { value = 1 };
        row0.Add(MakeLabeledControl("Level ID", _levelIdField, 55f, 110f));

        row0.Add(MakeButton("Reload Materials", () =>
        {
            LoadMaterialsFromFolder();
            RebuildMaterialsUI(settingsBox);
        }, 140));

        var rowA = MakeRow();
        settingsBox.Add(rowA);

        _widthField = new IntegerField() { value = _data.width };
        _heightField = new IntegerField() { value = _data.height };

        rowA.Add(MakeLabeledControl("Width", _widthField, 50f, 110f));
        rowA.Add(MakeLabeledControl("Height", _heightField, 55f, 110f));

        rowA.Add(MakeButton("Create / Resize", () =>
        {
            int w = Mathf.Max(1, _widthField.value);
            int h = Mathf.Max(1, _heightField.value);
            ResizeGrid(w, h);
        }, 130));

        var rowB = MakeRow();
        settingsBox.Add(rowB);

        _modeField = new EnumField(EditMode.ToggleCells);

        rowB.Add(MakeLabeledControl("Mode", _modeField, 40f, 160f));

        var rowC = MakeRow();
        settingsBox.Add(rowC);
        _autoGenDensityField = new FloatField() { value = 0.30f };
        _autoGenRandomDensityToggle = new Toggle() { value = true };
        _autoGenRandomDensityToggle.style.width = 20;
        rowC.Add(MakeLabeledControl("Wall Density", _autoGenDensityField, 90f, 110f));
        rowC.Add(MakeLabeledControl("Random", _autoGenRandomDensityToggle, 50f, 20f));

        var hint = new HelpBox(
            "Toggle: click a cell to invert (true<->false). Start is always Available.\nMode=SetStartNode: click a cell to set Start (it becomes Available).",
            HelpBoxMessageType.Info
        );
        hint.style.marginTop = 2;
        settingsBox.Add(hint);

        AddMaterialsPanel(settingsBox);
    }

    private void AddMaterialsPanel(VisualElement settingsBox)
    {
        var matTitle = new Label("Materials");
        matTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        matTitle.style.marginTop = 2;
        matTitle.style.marginBottom = 2;
        settingsBox.Add(matTitle);

        var list = _materials.Count > 0 ? _materials : new List<Material> { null };

        Func<Material, string> format = m => m == null ? "None" : m.name;

        _groundMatPopup = new PopupField<Material>("Ground", list, list[0], format, format);
        _wallMatPopup = new PopupField<Material>("Wall", list, list[Mathf.Min(1, list.Count - 1)], format, format);
        _paintMatPopup = new PopupField<Material>("Ground Paint", list, list[Mathf.Min(2, list.Count - 1)], format, format);

        _groundMatPopup.style.flexGrow = 0;
        _wallMatPopup.style.flexGrow = 0;
        _paintMatPopup.style.flexGrow = 0;
        _groundMatPopup.style.width = 200;
        _wallMatPopup.style.width = 200;
        _paintMatPopup.style.width = 200;
        _groundMatPopup.style.minWidth = 0;
        _wallMatPopup.style.minWidth = 0;
        _paintMatPopup.style.minWidth = 0;
        _groundMatPopup.style.maxWidth = 200;
        _wallMatPopup.style.maxWidth = 200;
        _paintMatPopup.style.maxWidth = 200;

        _groundPreview = new Image();
        _groundPreview.style.width = 48;
        _groundPreview.style.height = 48;
        _groundPreview.style.marginLeft = 10;

        _wallPreview = new Image();
        _wallPreview.style.width = 48;
        _wallPreview.style.height = 48;
        _wallPreview.style.marginLeft = 10;

        _paintPreview = new Image();
        _paintPreview.style.width = 48;
        _paintPreview.style.height = 48;
        _paintPreview.style.marginLeft = 10;

        _groundMatPopup.RegisterValueChangedCallback(_ => OnMaterialChanged());
        _wallMatPopup.RegisterValueChangedCallback(_ => OnMaterialChanged());
        _paintMatPopup.RegisterValueChangedCallback(_ => OnMaterialChanged());

        var row1 = MakeRow();
        row1.Add(_groundMatPopup);
        row1.Add(_groundPreview);
        settingsBox.Add(row1);

        var row2 = MakeRow();
        row2.Add(_wallMatPopup);
        row2.Add(_wallPreview);
        settingsBox.Add(row2);

        var row3 = MakeRow();
        row3.Add(_paintMatPopup);
        row3.Add(_paintPreview);
        settingsBox.Add(row3);

        SyncMaterialUIFromData();
        OnMaterialChanged();
    }

    private void RebuildMaterialsUI(VisualElement settingsBox)
    {
        BuildUI();
        RefreshUIFromData();
        SyncMaterialUIFromData();
        RebuildGrid();
    }

    private void LoadMaterialsFromFolder()
    {
        _materials.Clear();

        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            return;

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                _materials.Add(mat);
        }

        _materials.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
    }

    private void SyncMaterialUIFromData()
    {
        if (_groundMatPopup == null || _wallMatPopup == null || _paintMatPopup == null)
            return;

        var ground = LoadMatByPathSafe(_data.groundMaterialPath);
        var wall = LoadMatByPathSafe(_data.wallMaterialPath);
        var paint = LoadMatByPathSafe(_data.paintMaterialPath);

        if (ground != null)
            _groundMatPopup.SetValueWithoutNotify(ground);

        if (wall != null)
            _wallMatPopup.SetValueWithoutNotify(wall);

        if (paint != null)
            _paintMatPopup.SetValueWithoutNotify(paint);

        OnMaterialChanged();
    }

    private Material LoadMatByPathSafe(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    private void OnMaterialChanged()
    {
        var g = _groundMatPopup?.value;
        var w = _wallMatPopup?.value;
        var p = _paintMatPopup?.value;

        string gPath = g != null ? AssetDatabase.GetAssetPath(g) : null;
        string wPath = w != null ? AssetDatabase.GetAssetPath(w) : null;
        string pPath = p != null ? AssetDatabase.GetAssetPath(p) : null;

        if (gPath != _data.groundMaterialPath || wPath != _data.wallMaterialPath || pPath != _data.paintMaterialPath)
            PushUndoState();

        if (g != null)
        {
            _data.groundMaterialPath = gPath;
            _groundPreview.image = AssetPreview.GetAssetPreview(g) ?? AssetPreview.GetMiniThumbnail(g);
        }
        else
        {
            _data.groundMaterialPath = null;
            if (_groundPreview != null) _groundPreview.image = null;
        }

        if (w != null)
        {
            _data.wallMaterialPath = wPath;
            _wallPreview.image = AssetPreview.GetAssetPreview(w) ?? AssetPreview.GetMiniThumbnail(w);
        }
        else
        {
            _data.wallMaterialPath = null;
            if (_wallPreview != null) _wallPreview.image = null;
        }

        if (p != null)
        {
            _data.paintMaterialPath = pPath;
            _paintPreview.image = AssetPreview.GetAssetPreview(p) ?? AssetPreview.GetMiniThumbnail(p);
        }
        else
        {
            _data.paintMaterialPath = null;
            if (_paintPreview != null) _paintPreview.image = null;
        }
    }

    private static VisualElement MakeWrapRow()
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.flexWrap = Wrap.Wrap;
        return row;
    }

    private static VisualElement MakeRow()
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.flexWrap = Wrap.Wrap;
        row.style.marginBottom = 6;
        return row;
    }

    private static VisualElement MakeLabeledControl(string label, VisualElement control, float labelWidth, float controlWidth)
    {
        var wrap = new VisualElement();
        wrap.style.flexDirection = FlexDirection.Row;
        wrap.style.alignItems = Align.Center;
        wrap.style.marginRight = 8;
        wrap.style.marginBottom = 4;

        var l = new Label(label);
        l.style.minWidth = labelWidth;
        l.style.width = labelWidth;

        control.style.flexGrow = 0;
        control.style.width = controlWidth;
        control.style.minWidth = controlWidth;

        wrap.Add(l);
        wrap.Add(control);
        return wrap;
    }

    private static Button MakeButton(string text, Action onClick, float width)
    {
        var b = new Button(onClick) { text = text };
        b.style.flexGrow = 0;
        b.style.width = width;
        b.style.height = 22;
        b.style.marginRight = 12;
        b.style.marginBottom = 8;
        return b;
    }

    private static void AddBorder(VisualElement ve)
    {
        ve.style.borderTopWidth = 1;
        ve.style.borderBottomWidth = 1;
        ve.style.borderLeftWidth = 1;
        ve.style.borderRightWidth = 1;

        var c = new Color(0, 0, 0, 0.25f);
        ve.style.borderTopColor = c;
        ve.style.borderBottomColor = c;
        ve.style.borderLeftColor = c;
        ve.style.borderRightColor = c;
    }

    private void RefreshUIFromData()
    {
        _suppressHistory = true;
        _widthField.value = _data.width;
        _heightField.value = _data.height;
        _startLabel.text = $"Start: ({_data.startNode.x}, {_data.startNode.y})";
        _suppressHistory = false;
    }

    private void RebuildGrid()
    {
        EnsureStartNodeIsAlwaysAvailable();
        RefreshUIFromData();

        _gridRoot.Clear();

        const int cellPx = 24;

        for (int y = 0; y < _data.height; y++)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 4;
            _gridRoot.Add(row);

            for (int x = 0; x < _data.width; x++)
            {
                int cx = x;
                int cy = y;

                bool avail = _data.Get(cx, cy);
                bool isStart = (_data.startNode.x == cx && _data.startNode.y == cy);

                var btn = new Button(() => OnCellClicked(cx, cy)) { text = "" };
                btn.style.width = cellPx;
                btn.style.height = cellPx;
                btn.style.marginRight = 4;

                if (isStart) btn.style.backgroundColor = new StyleColor(new Color(0f, 1f, 1f, 1f));
                else if (avail) btn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.9f, 0.2f, 1f));
                else btn.style.backgroundColor = new StyleColor(new Color(0.9f, 0.2f, 0.2f, 1f));

                row.Add(btn);
            }
        }
    }

    private void OnCellClicked(int x, int y)
    {
        var mode = (EditMode)_modeField.value;

        if (mode == EditMode.SetStartNode)
        {
            PushUndoState();
            _data.startNode = new Vector2Int(x, y);
            _data.Set(x, y, true);
            RebuildGrid();
            return;
        }

        bool isStart = (_data.startNode.x == x && _data.startNode.y == y);
        if (isStart)
        {
            PushUndoState();
            _data.Set(x, y, true);
            RebuildGrid();
            return;
        }

        PushUndoState();
        bool current = _data.Get(x, y);
        _data.Set(x, y, !current);
        RebuildGrid();
    }

    private void EnsureStartNodeIsAlwaysAvailable()
    {
        if (_data == null || !_data.IsValid())
            return;

        int sx = Mathf.Clamp(_data.startNode.x, 0, _data.width - 1);
        int sy = Mathf.Clamp(_data.startNode.y, 0, _data.height - 1);
        _data.startNode = new Vector2Int(sx, sy);
        _data.Set(sx, sy, true);
    }

    private void ResizeGrid(int newW, int newH)
    {
        newW = Mathf.Max(1, newW);
        newH = Mathf.Max(1, newH);

        PushUndoState();
        var newData = new LevelData(newW, newH);

        if (_data != null && _data.IsValid())
        {
            int copyW = Mathf.Min(_data.width, newW);
            int copyH = Mathf.Min(_data.height, newH);

            for (int x = 0; x < copyW; x++)
            for (int y = 0; y < copyH; y++)
                newData.Set(x, y, _data.Get(x, y));

            newData.startNode = new Vector2Int(
                Mathf.Clamp(_data.startNode.x, 0, newW - 1),
                Mathf.Clamp(_data.startNode.y, 0, newH - 1)
            );

            newData.groundMaterialPath = _data.groundMaterialPath;
            newData.wallMaterialPath = _data.wallMaterialPath;
            newData.paintMaterialPath = _data.paintMaterialPath;
        }

        _data = newData;
        EnsureStartNodeIsAlwaysAvailable();
        RebuildGrid();
    }

    private void FillAll(bool value)
    {
        PushUndoState();
        for (int x = 0; x < _data.width; x++)
        for (int y = 0; y < _data.height; y++)
            _data.Set(x, y, value);
    }

    private void Invert()
    {
        PushUndoState();
        for (int x = 0; x < _data.width; x++)
        for (int y = 0; y < _data.height; y++)
            _data.Set(x, y, !_data.Get(x, y));
    }

    private void AutoGenerateLevel()
    {
        PushUndoState();
        if (_data == null || !_data.IsValid())
            return;

        int w = _data.width;
        int h = _data.height;

        int totalCells = w * h;
        float baseDensity = _autoGenRandomDensityToggle != null && _autoGenRandomDensityToggle.value
            ? UnityEngine.Random.Range(0.20f, 0.45f)
            : Mathf.Clamp01(_autoGenDensityField != null ? _autoGenDensityField.value : 0.30f);

        // Try random layouts with decreasing density until valid.
        float density = baseDensity;
        while (density >= 0.05f)
        {
            int attempts = totalCells * 8;
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    _data.Set(x, y, UnityEngine.Random.value > density);

                _data.startNode = new Vector2Int(
                    UnityEngine.Random.Range(0, w),
                    UnityEngine.Random.Range(0, h));
                EnsureStartNodeIsAlwaysAvailable();

                if (IsLevelValid())
                    return;
            }

            density -= 0.05f;
        }

        // Fallback: fully open grid to guarantee validity.
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            _data.Set(x, y, true);
        _data.startNode = new Vector2Int(
            UnityEngine.Random.Range(0, w),
            UnityEngine.Random.Range(0, h));
        EnsureStartNodeIsAlwaysAvailable();
    }

    private bool TryCarveWalls(float density)
    {
        int w = _data.width;
        int h = _data.height;
        int totalCells = w * h;
        int targetWalls = Mathf.Clamp(Mathf.RoundToInt(totalCells * density), 0, totalCells - 1);
        if (targetWalls <= 0)
            return false;

        var candidates = new List<Vector2Int>(totalCells - 1);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (x == _data.startNode.x && y == _data.startNode.y)
                continue;
            candidates.Add(new Vector2Int(x, y));
        }

        // Fisher-Yates shuffle
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int walls = 0;
        for (int i = 0; i < candidates.Count && walls < targetWalls; i++)
        {
            var c = candidates[i];
            if (!_data.Get(c.x, c.y))
                continue;

            _data.Set(c.x, c.y, false);
            if (IsLevelValid())
            {
                walls++;
                continue;
            }

            _data.Set(c.x, c.y, true);
        }

        return walls > 0;
    }

    private bool IsLevelValid()
    {
        int totalAvailable = 0;
        for (int i = 0; i < _data.grid.Length; i++)
            if (_data.grid[i]) totalAvailable++;

        if (totalAvailable == 0)
            return false;

        int reachable = CountReachableCellsBySwipe();
        if (reachable != totalAvailable)
            return false;

        return IsStopGraphStronglyConnected();
    }

    private string GetLevelsFolderAbsolute()
    {
        return Path.Combine(Application.dataPath, "Levels");
    }

    private string GetLevelPathAbsolute(int levelId)
    {
        string folder = GetLevelsFolderAbsolute();
        return Path.Combine(folder, $"Level_{levelId}.json");
    }

    private void SaveJsonAuto()
    {
        EnsureStartNodeIsAlwaysAvailable();
        OnMaterialChanged();

        int levelId = Mathf.Max(1, _levelIdField.value);

        string folder = GetLevelsFolderAbsolute();
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string path = GetLevelPathAbsolute(levelId);

        string json = JsonUtility.ToJson(_data, true);
        File.WriteAllText(path, json);

        AssetDatabase.Refresh();
        Debug.Log($"Saved level json: {path}");
    }

    private void LoadJsonById()
    {
        int levelId = Mathf.Max(1, _levelIdField.value);
        string folder = GetLevelsFolderAbsolute();

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string path = GetLevelPathAbsolute(levelId);

        if (!File.Exists(path))
            return;

        string json = File.ReadAllText(path);
        var loaded = JsonUtility.FromJson<LevelData>(json);

        if (loaded == null || !loaded.IsValid())
            return;

        _data = loaded;
        _undo.Clear();
        _redo.Clear();
        EnsureStartNodeIsAlwaysAvailable();

        RefreshUIFromData();

        LoadMaterialsFromFolder();
        SyncMaterialUIFromData();

        RebuildGrid();
    }

    private void PushUndoState()
    {
        if (_suppressHistory || _data == null)
            return;
        _undo.Push(CloneData(_data));
        _redo.Clear();
    }

    private void Undo()
    {
        if (_undo.Count == 0)
            return;
        _redo.Push(CloneData(_data));
        _data = _undo.Pop();
        EnsureStartNodeIsAlwaysAvailable();
        RefreshUIFromData();
        SyncMaterialUIFromData();
        RebuildGrid();
    }

    private void Redo()
    {
        if (_redo.Count == 0)
            return;
        _undo.Push(CloneData(_data));
        _data = _redo.Pop();
        EnsureStartNodeIsAlwaysAvailable();
        RefreshUIFromData();
        SyncMaterialUIFromData();
        RebuildGrid();
    }

    private static LevelData CloneData(LevelData data)
    {
        if (data == null)
            return null;
        string json = JsonUtility.ToJson(data);
        return JsonUtility.FromJson<LevelData>(json);
    }

    private void ValidateLevel()
    {
        EnsureStartNodeIsAlwaysAvailable();
        if (_data == null || !_data.IsValid())
        {
            SetValidationResult(false, 0, 0, false);
            return;
        }

        int totalAvailable = 0;
        for (int i = 0; i < _data.grid.Length; i++)
        {
            if (_data.grid[i])
                totalAvailable++;
        }

        if (totalAvailable == 0)
        {
            SetValidationResult(false, 0, 0, false);
            return;
        }

        int reachable = CountReachableCellsBySwipe();
        bool allReachable = reachable == totalAvailable;
        bool noDeadEnds = IsStopGraphStronglyConnected();
        SetValidationResult(allReachable && noDeadEnds, reachable, totalAvailable, noDeadEnds);
    }

    private int CountReachableCellsBySwipe()
    {
        int w = _data.width;
        int h = _data.height;
        var visitedStops = new bool[w, h];
        var reachable = new bool[w, h];
        var queue = new Queue<Vector2Int>();

        if (!_data.Get(_data.startNode.x, _data.startNode.y))
            return 0;

        visitedStops[_data.startNode.x, _data.startNode.y] = true;
        reachable[_data.startNode.x, _data.startNode.y] = true;
        queue.Enqueue(_data.startNode);

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();

            TryEnqueueSlide(cur, 0, 1, visitedStops, reachable, queue);  // up (y+1)
            TryEnqueueSlide(cur, 0, -1, visitedStops, reachable, queue); // down (y-1)
            TryEnqueueSlide(cur, -1, 0, visitedStops, reachable, queue); // left
            TryEnqueueSlide(cur, 1, 0, visitedStops, reachable, queue);  // right
        }

        int count = 0;
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            if (reachable[x, y])
                count++;
        }
        return count;
    }

    private void TryEnqueueSlide(
        Vector2Int from,
        int dx,
        int dy,
        bool[,] visitedStops,
        bool[,] reachable,
        Queue<Vector2Int> queue)
    {
        int x = from.x;
        int y = from.y;
        Vector2Int last = new Vector2Int(-1, -1);

        while (true)
        {
            int nx = x + dx;
            int ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= _data.width || ny >= _data.height)
                break;
            if (!_data.Get(nx, ny))
                break;
            last = new Vector2Int(nx, ny);
            x = nx;
            y = ny;
            reachable[x, y] = true;
        }

        if (last.x == -1)
            return;

        if (!visitedStops[last.x, last.y])
        {
            visitedStops[last.x, last.y] = true;
            queue.Enqueue(last);
        }
    }

    private void SetValidationResult(bool ok, int reachable, int total, bool noDeadEnds)
    {
        if (_validationLabel == null)
            return;
        _validationLabel.text = $"Validation: {(ok ? "PASSED" : "FAILED")}";
        _validationLabel.style.color = ok ? new StyleColor(new Color(0.2f, 0.9f, 0.2f)) : new StyleColor(new Color(0.9f, 0.2f, 0.2f));
    }

    private bool IsStopGraphStronglyConnected()
    {
        int w = _data.width;
        int h = _data.height;

        var stopList = new List<Vector2Int>();
        var index = new Dictionary<int, int>();

        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            if (!_data.Get(x, y))
                continue;

            bool isStop = IsStopCell(x, y);
            if (!isStop)
                continue;

            int key = (x << 16) ^ (y & 0xFFFF);
            index[key] = stopList.Count;
            stopList.Add(new Vector2Int(x, y));
        }

        if (stopList.Count == 0)
            return true;

        var edges = new List<int>[stopList.Count];
        var rev = new List<int>[stopList.Count];
        for (int i = 0; i < stopList.Count; i++)
        {
            edges[i] = new List<int>(4);
            rev[i] = new List<int>(4);
        }

        for (int i = 0; i < stopList.Count; i++)
        {
            var p = stopList[i];
            AddEdgeFromStop(p, i, 1, 0, index, edges, rev);
            AddEdgeFromStop(p, i, -1, 0, index, edges, rev);
            AddEdgeFromStop(p, i, 0, 1, index, edges, rev);
            AddEdgeFromStop(p, i, 0, -1, index, edges, rev);
        }

        var startIndices = GetStartStopIndices(index);
        if (startIndices.Count == 0)
            return true;

        var reachableStops = AllReachableFromStarts(startIndices, edges);
        if (reachableStops.Count == 0)
            return true;

        if (!AllReachableSubset(reachableStops, edges))
            return false;
        if (!AllReachableSubset(reachableStops, rev))
            return false;

        return true;
    }

    private List<int> GetStartStopIndices(Dictionary<int, int> index)
    {
        var list = new List<int>();

        int sx = _data.startNode.x;
        int sy = _data.startNode.y;

        int startKey = (sx << 16) ^ (sy & 0xFFFF);
        if (index.TryGetValue(startKey, out int idx))
            list.Add(idx);

        AddStartStopInDirection(sx, sy, 1, 0, index, list);
        AddStartStopInDirection(sx, sy, -1, 0, index, list);
        AddStartStopInDirection(sx, sy, 0, 1, index, list);
        AddStartStopInDirection(sx, sy, 0, -1, index, list);

        return list;
    }

    private void AddStartStopInDirection(int sx, int sy, int dx, int dy, Dictionary<int, int> index, List<int> list)
    {
        int x = sx;
        int y = sy;
        Vector2Int last = new Vector2Int(-1, -1);

        while (true)
        {
            int nx = x + dx;
            int ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= _data.width || ny >= _data.height)
                break;
            if (!_data.Get(nx, ny))
                break;
            last = new Vector2Int(nx, ny);
            x = nx;
            y = ny;
        }

        if (last.x == -1)
            return;

        int key = (last.x << 16) ^ (last.y & 0xFFFF);
        if (index.TryGetValue(key, out int idx) && !list.Contains(idx))
            list.Add(idx);
    }

    private bool IsStopCell(int x, int y)
    {
        // Stop if at least one direction is blocked or out of bounds
        if (!_data.Get(x, y))
            return false;

        return !IsAvailable(x + 1, y) || !IsAvailable(x - 1, y) || !IsAvailable(x, y + 1) || !IsAvailable(x, y - 1);
    }

    private bool IsAvailable(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _data.width || y >= _data.height)
            return false;
        return _data.Get(x, y);
    }

    private void AddEdgeFromStop(
        Vector2Int from,
        int fromIdx,
        int dx,
        int dy,
        Dictionary<int, int> index,
        List<int>[] edges,
        List<int>[] rev)
    {
        int x = from.x;
        int y = from.y;
        Vector2Int last = new Vector2Int(-1, -1);

        while (true)
        {
            int nx = x + dx;
            int ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= _data.width || ny >= _data.height)
                break;
            if (!_data.Get(nx, ny))
                break;
            last = new Vector2Int(nx, ny);
            x = nx;
            y = ny;
        }

        if (last.x == -1)
            return;

        int key = (last.x << 16) ^ (last.y & 0xFFFF);
        if (!index.TryGetValue(key, out int toIdx))
            return;

        if (!edges[fromIdx].Contains(toIdx))
            edges[fromIdx].Add(toIdx);
        if (!rev[toIdx].Contains(fromIdx))
            rev[toIdx].Add(fromIdx);
    }

    private HashSet<int> AllReachableFromStarts(List<int> startIndices, List<int>[] edges)
    {
        var visited = new bool[edges.Length];
        var q = new Queue<int>();
        for (int i = 0; i < startIndices.Count; i++)
        {
            int s = startIndices[i];
            if (s < 0 || s >= edges.Length || visited[s])
                continue;
            visited[s] = true;
            q.Enqueue(s);
        }

        while (q.Count > 0)
        {
            int cur = q.Dequeue();
            var list = edges[cur];
            for (int i = 0; i < list.Count; i++)
            {
                int nxt = list[i];
                if (visited[nxt])
                    continue;
                visited[nxt] = true;
                q.Enqueue(nxt);
            }
        }

        var result = new HashSet<int>();
        for (int i = 0; i < visited.Length; i++)
            if (visited[i])
                result.Add(i);
        return result;
    }

    private bool AllReachableSubset(HashSet<int> subset, List<int>[] edges)
    {
        int any = -1;
        foreach (var s in subset)
        {
            any = s;
            break;
        }
        if (any < 0)
            return true;

        var visited = new bool[edges.Length];
        var q = new Queue<int>();
        visited[any] = true;
        q.Enqueue(any);

        while (q.Count > 0)
        {
            int cur = q.Dequeue();
            var list = edges[cur];
            for (int i = 0; i < list.Count; i++)
            {
                int nxt = list[i];
                if (!subset.Contains(nxt) || visited[nxt])
                    continue;
                visited[nxt] = true;
                q.Enqueue(nxt);
            }
        }

        foreach (var s in subset)
            if (!visited[s])
                return false;
        return true;
    }
}
#endif
