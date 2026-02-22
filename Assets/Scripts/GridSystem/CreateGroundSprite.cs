using System;
using System.Collections.Generic;
using Injection;
using UnityEngine;
using Utility;
using Zenject;

namespace GridSystem
{
    public class CreateGroundSprite : IInitializable, IDisposable
    {
        private readonly GridController _gridController;
        private readonly SignalBus _signalBus;
        private readonly GridVisualSettings _settings;

        private ObjectPooler<GridVisual> _pooler;
        private readonly List<GridVisual> _frameVisuals = new();
        private Material _wallMat;
        private bool _isZ;

        public CreateGroundSprite(
            SignalBus signalBus,
            GridController gridController,
            GridVisualSettings settings)
        {
            _signalBus = signalBus;
            _gridController = gridController;
            _settings = settings;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<OnLevelUnload>(OnLevelUnload);
            _signalBus.Subscribe<CreateSprites>(CreateSprites);
            _signalBus.Subscribe<OnLevelMaterialsLoaded>(OnLevelMaterialsLoaded);
            SetupPool();
        }

        private void SetupPool()
        {
            var poolItems = new List<ObjectPooler<GridVisual>.PoolItem>
            {
                new(PoolNames.GridVisual, _settings.VisualPrefab, _settings.DefaultCapacity, _settings.MaxSize)
            };

            _pooler = new ObjectPooler<GridVisual>(poolItems);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<CreateSprites>(CreateSprites);
            _signalBus.Unsubscribe<OnLevelUnload>(OnLevelUnload);
            _signalBus.Unsubscribe<OnLevelMaterialsLoaded>(OnLevelMaterialsLoaded);
        }

        private void OnLevelUnload(OnLevelUnload obj)
        {
            ReleaseAll();
        }

        private void CreateSprites(CreateSprites createSprites)
        {
            ReleaseAll();
            _isZ = createSprites.IsZ;
            for (int i = 0; i < _gridController.Grid.width; i++)
            {
                for (int j = 0; j < _gridController.Grid.height; j++)
                {
                    var visual = CreateGridSprite(
                        _gridController.Grid.GetWorldPosition(i, j),
                        _gridController.CellSize,
                        createSprites.IsZ);

                    _gridController.Grid.GetNodeWithoutCoord(i, j).GridVisual = visual;
                }
            }

            CreateFrame();
        }


        private void ReleaseAll()
        {
            if (_gridController.Grid == null)
                return;

            for (int i = 0; i < _gridController.Grid.width; i++)
            {
                for (int j = 0; j < _gridController.Grid.height; j++)
                {
                    var node = _gridController.Grid.GetNodeWithoutCoord(i, j);
                    if (node?.GridVisual == null)
                        continue;

                    _pooler.Release(PoolNames.GridVisual, node.GridVisual);
                    node.GridVisual = null;
                }
            }

            for (int i = 0; i < _frameVisuals.Count; i++)
            {
                if (_frameVisuals[i] != null)
                    _pooler.Release(PoolNames.GridVisual, _frameVisuals[i]);
            }
            _frameVisuals.Clear();
        }

        private GridVisual CreateGridSprite(Vector3 pos, float cellSize, bool isZ)
        {
            if (isZ) pos = new Vector3(pos.x, 0, pos.y);

            var obj = _pooler.Spawn(PoolNames.GridVisual, pos, Quaternion.identity);
            obj.transform.localScale = Vector3.one * cellSize;
            return obj;
        }

        private void OnLevelMaterialsLoaded(OnLevelMaterialsLoaded materials)
        {
            _wallMat = materials.Wall;
        }

        private void CreateFrame()
        {
            if (_gridController.Grid == null)
                return;

            int width = _gridController.Grid.width;
            int height = _gridController.Grid.height;
            float cellSize = _gridController.CellSize;
            Vector3 origin = _gridController.Grid.GetWorldPosition(0, 0);

            for (int x = -1; x <= width; x++)
            {
                for (int y = -1; y <= height; y++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                        continue;

                    var pos = new Vector3(origin.x + x * cellSize, origin.y - y * cellSize);
                    if (_isZ) pos = new Vector3(pos.x, 0, pos.y);

                    var visual = _pooler.Spawn(PoolNames.GridVisual, pos, Quaternion.identity);
                    visual.transform.localScale = Vector3.one * cellSize;
                    visual.SetMaterials(_wallMat, _wallMat, _wallMat);
                    visual.SetWall();
                    _frameVisuals.Add(visual);
                }
            }
        }
    }
}
