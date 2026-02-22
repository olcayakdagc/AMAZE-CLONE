using System.Collections.Generic;
using Injection;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Zenject;

namespace GameScene.UI
{
    public class FillController : MonoBehaviour
    {
        [SerializeField] private Slider fillSlider;
        [SerializeField] private TextMeshProUGUI percentageText;

        [Inject] private SignalBus _signalBus;

        private readonly HashSet<int> _painted = new();
        private int _availableCount;
        private int _startKey;
        private bool _hasStartKey;

        private void OnEnable()
        {
            _signalBus.Subscribe<OnLevelLoad>(OnLevelLoad);
            _signalBus.Subscribe<OnNodePainted>(OnNodePainted);
        }

        private void OnDisable()
        {
            _signalBus.Unsubscribe<OnLevelLoad>(OnLevelLoad);
            _signalBus.Unsubscribe<OnNodePainted>(OnNodePainted);
        }

        private void OnLevelLoad(OnLevelLoad onLevelLoad)
        {
            _availableCount = Mathf.Max(0, onLevelLoad.AvailableCount);
            _painted.Clear();
            _startKey = (onLevelLoad.StartPos.x << 16) ^ (onLevelLoad.StartPos.y & 0xFFFF);
            _hasStartKey = true;
            UpdateFill();
        }

        private void OnNodePainted(OnNodePainted onNodePainted)
        {
            int key = (onNodePainted.X << 16) ^ (onNodePainted.Y & 0xFFFF);
            if (_hasStartKey && key == _startKey)
                return;
            if (_painted.Add(key))
                UpdateFill();
        }

        private void UpdateFill()
        {
            if (fillSlider == null)
                return;

            float ratio = _availableCount <= 0 ? 0f : (float)_painted.Count / _availableCount;
            float clamped = Mathf.Clamp01(ratio);
            fillSlider.value = clamped;
            if (percentageText != null)
                percentageText.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
        }
    }
}
