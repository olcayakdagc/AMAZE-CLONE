using System;
using System.Collections.Generic;
using Injection;
using Managers;
using UnityEngine;
using Zenject;

namespace GameScene.GamePlay
{
    public class WinManager : IInitializable, IDisposable
    {
        [Inject] private GameManager _gameManager;
        [Inject] private SignalBus _signalBus;

        private readonly HashSet<int> _painted = new();
        private int _availableCount;
        private bool _winFired;
        private int _startKey;
        private bool _hasStartKey;

        public void Initialize()
        {
            _signalBus.Subscribe<OnLevelLoad>(OnLevelLoad);
            _signalBus.Subscribe<OnNodePainted>(OnNodePainted);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<OnLevelLoad>(OnLevelLoad);
            _signalBus.Unsubscribe<OnNodePainted>(OnNodePainted);
        }

        private void OnLevelLoad(OnLevelLoad onLevelLoad)
        {
            _availableCount = Mathf.Max(0, onLevelLoad.AvailableCount);
            _painted.Clear();
            _winFired = false;
            _startKey = (onLevelLoad.StartPos.x << 16) ^ (onLevelLoad.StartPos.y & 0xFFFF);
            _hasStartKey = true;
        }

        private void OnNodePainted(OnNodePainted onNodePainted)
        {
            if (_winFired || _availableCount <= 0)
                return;

            int key = (onNodePainted.X << 16) ^ (onNodePainted.Y & 0xFFFF);
            if (_hasStartKey && key == _startKey)
                return;
            if (_painted.Add(key) && _painted.Count >= _availableCount)
            {
                _winFired = true;
                _gameManager.Win();
            }
        }
    }
}
