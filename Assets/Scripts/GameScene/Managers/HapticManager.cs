using System;
using Injection;
using Lofelt.NiceVibrations;
using Zenject;

namespace GameScene.Managers
{
    public class HapticManager : MonoSingleton<HapticManager>
    {
        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void PlayHapticPattern(HapticPatterns.PresetType preset)
        {
            HapticPatterns.PlayPreset(preset);
        }
    }
}