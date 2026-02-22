using System;
using Injection;
using UnityEngine;
using Zenject;

namespace GameScene.UI
{
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [Inject] private SignalBus _signalBus;

        private void Start()
        {
            panel.SetActive(true);
            _signalBus.Subscribe<OnLevelLoad>(OnLevelLoad);
        }

        private void OnDestroy()
        {
            _signalBus.Unsubscribe<OnLevelLoad>(OnLevelLoad);
        }

        private void OnLevelLoad(OnLevelLoad onLevelLoad)
        {
            panel.SetActive(false);
        }
    }
}
