using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GridSystem;
using Managers;
using Injection;
using SaveSystem;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zenject;
using Random = UnityEngine.Random;

namespace Managers.LevelSystem
{
    public class LevelManager : MonoBehaviour
    {
        private const string LevelAssetNameFormat = "Level_{0}";
        private const string LevelsLabel = "Levels";
        private const string GameSettingsAddress = "GameSettings";

        public int CurrentLevelIndex { get; private set; }

        [Tooltip("Randomizes levels after all levels are played.\nIf unchecked, levels loop in order.")]
        [SerializeField]
        private bool randomizeAfterRotation = true;
        
        [SerializeField]
        private MeshRenderer bgPlane;

        [SerializeField]
        private float defaultCellSize = 1f;

        private float _defaultBallSpeed = 20f;
        private float _defaultRollSpeedMultiplier = 10f;
        private float _defaultCameraYOffset = 1f;

        [Inject] private GridController _gridController;
        [Inject] private SignalBus _signalBus;
        [Inject] private GameManager _gameManager;

        private int _levelCount;
        private List<TextAsset> _levelAssets = new();
        private bool _pendingNextLevel;
        private bool _pendingRestart;

        private void OnEnable()
        {
            _signalBus.Subscribe<NextLevel>(NextLevel);
            _signalBus.Subscribe<Restart>(Restart);
            _signalBus.Subscribe<OnCameraExitComplete>(OnCameraExitComplete);
        }

        private void OnDisable()
        {
            _signalBus.Unsubscribe<NextLevel>(NextLevel);
            _signalBus.Unsubscribe<Restart>(Restart);
            _signalBus.Unsubscribe<OnCameraExitComplete>(OnCameraExitComplete);
        }

        private async void Start()
        {
            await SetupAsync();
        }

        private async Task SetupAsync()
        {
            _levelAssets = await LoadLevelAssets();
            _levelCount = _levelAssets.Count;

            LoadCurrentLevel();
        }

        private void NextLevel()
        {
            SaveData.Instance.Level++;
            SaveController.instance.Save();

            _pendingNextLevel = true;
        }

        private void Restart()
        {
            _pendingRestart = true;
        }

        private void UnloadLevel()
        {
            _signalBus.Fire<OnLevelUnload>();
        }

        private void OnCameraExitComplete()
        {
            if (_pendingNextLevel)
            {
                _pendingNextLevel = false;
                UnloadLevel();
                LoadCurrentLevel();
                return;
            }

            if (_pendingRestart)
            {
                _pendingRestart = false;
                UnloadLevel();
                _ = LoadLevel();
            }
        }

        private async void LoadCurrentLevel()
        {
            _gameManager.GameStart();

            if (_levelCount <= 0)
            {
                Debug.LogError($"No addressable levels found with label: {LevelsLabel}");
                return;
            }

            int levelIndex = SaveData.Instance.Level;

            if (levelIndex <= _levelCount)
            {
                CurrentLevelIndex = levelIndex;
            }
            else if (randomizeAfterRotation)
            {
                if (_levelCount <= 1)
                {
                    CurrentLevelIndex = 1;
                }
                else
                {
                    int randLevel = Random.Range(1, _levelCount + 1);
                    while (randLevel == CurrentLevelIndex)
                        randLevel = Random.Range(1, _levelCount + 1);

                    CurrentLevelIndex = randLevel;
                }
            }
            else
            {
                levelIndex %= _levelCount;
                CurrentLevelIndex = levelIndex == 0 ? _levelCount : levelIndex;
            }

            await LoadLevel();
        }

        private async Task LoadLevel()
        {
            TextAsset asset = GetLevelAsset(CurrentLevelIndex);
            if (asset == null)
            {
                Debug.LogError($"Level addressable not found: {string.Format(LevelAssetNameFormat, CurrentLevelIndex)} (Label: {LevelsLabel})");
                return;
            }

            string json = asset.text;

            var levelData = JsonUtility.FromJson<LevelData>(json);
            if (levelData == null || !levelData.IsValid())
            {
                Debug.LogError($"Invalid level json: {asset.name}");
                return;
            }

            var groundMatTask = LoadMaterialAsync(levelData.groundMaterialPath);
            var wallMatTask = LoadMaterialAsync(levelData.wallMaterialPath);
            var paintMatTask = LoadMaterialAsync(levelData.paintMaterialPath);
            await Task.WhenAll(groundMatTask, wallMatTask, paintMatTask);

            _signalBus.Fire(new OnLevelMaterialsLoaded
            {
                Ground = groundMatTask.Result,
                Wall = wallMatTask.Result,
                Paint = paintMatTask.Result
            });

            SetupLevel(levelData, groundMatTask.Result, wallMatTask.Result, paintMatTask.Result);

            var settings = await LoadGlobalSettingsAsync();
            _signalBus.Fire(new OnLevelLoad
            {
                StartPos = levelData.startNode,
                GridWidth = levelData.width,
                GridHeight = levelData.height,
                CellSize = defaultCellSize,
                BallSpeed = settings.ballSpeed,
                RollSpeedMultiplier = settings.rollSpeedMultiplier,
                CameraYOffset = settings.cameraYOffset,
                AvailableCount = CountAvailableNodes(levelData)
            });
            _gameManager.GamePlay();
        }

        private void SetupLevel(LevelData levelData, Material groundMat, Material wallMat, Material paintMat)
        {
            bgPlane.material = wallMat;
            _gridController.CreateGrid(levelData.width, levelData.height, defaultCellSize, false);
            ApplyLevelToNodes(levelData, groundMat, wallMat, paintMat);
        }

        private void ApplyLevelToNodes(LevelData data, Material groundMat, Material wallMat, Material paintMat)
        {
            var grid = _gridController.Grid;
            if (grid == null)
                return;

            if (grid.width != data.width || grid.height != data.height)
            {
                Debug.LogError("Grid size mismatch with level data.");
                return;
            }

            for (int x = 0; x < grid.width; x++)
            {
                for (int y = 0; y < grid.height; y++)
                {
                    var node = grid.GetNodeWithoutCoord(x, y);
                    if (node == null)
                        continue;

                    if (node.GridVisual != null)
                        node.GridVisual.SetMaterials(groundMat, wallMat, paintMat);

                    node.IsAvailble = data.grid[x + y * data.width];

                    if (node.IsAvailble)
                        node.GridVisual.SetGround();
                    else
                        node.GridVisual.SetWall();
                }
            }
        }

        private static async Task<Material> LoadMaterialAsync(string address)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            var handle = Addressables.LoadAssetAsync<Material>(address);
            return await handle.Task;
        }

        private static int CountAvailableNodes(LevelData data)
        {
            if (data == null || data.grid == null)
                return 0;

            int count = 0;
            for (int i = 0; i < data.grid.Length; i++)
            {
                if (data.grid[i])
                    count++;
            }

            return count - 1;
        }

        private async Task<List<TextAsset>> LoadLevelAssets()
        {
            var handle = Addressables.LoadAssetsAsync<TextAsset>(LevelsLabel, null);
            var assets = await handle.Task;
            return assets?.Where(a => a != null).ToList() ?? new List<TextAsset>();
        }

        private TextAsset GetLevelAsset(int levelIndex)
        {
            string expectedName = string.Format(LevelAssetNameFormat, levelIndex);
            for (int i = 0; i < _levelAssets.Count; i++)
            {
                var asset = _levelAssets[i];
                if (asset != null && asset.name == expectedName)
                    return asset;
            }

            return null;
        }

        private async Task<GameSettingsData> LoadGlobalSettingsAsync()
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(GameSettingsAddress);
            try
            {
                var asset = await handle.Task;
                if (asset == null)
                    return new GameSettingsData
                    {
                        ballSpeed = _defaultBallSpeed,
                        rollSpeedMultiplier = _defaultRollSpeedMultiplier,
                        cameraYOffset = _defaultCameraYOffset
                    };

                var data = JsonUtility.FromJson<GameSettingsData>(asset.text);
                if (data != null)
                    return data;
                return new GameSettingsData
                {
                    ballSpeed = _defaultBallSpeed,
                    rollSpeedMultiplier = _defaultRollSpeedMultiplier,
                    cameraYOffset = _defaultCameraYOffset
                };
            }
            catch
            {
                return new GameSettingsData
                {
                    ballSpeed = _defaultBallSpeed,
                    rollSpeedMultiplier = _defaultRollSpeedMultiplier,
                    cameraYOffset = _defaultCameraYOffset
                };
            }
            finally
            {
                Addressables.Release(handle);
            }
        }
    }
}
