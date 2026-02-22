using GameScene.Managers;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class BaseButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        private void Start()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveAllListeners();
        }

        private void OnClick()
        {
            HapticManager.instance.PlayHapticPattern(HapticPatterns.PresetType.Selection);
        }
    }
}