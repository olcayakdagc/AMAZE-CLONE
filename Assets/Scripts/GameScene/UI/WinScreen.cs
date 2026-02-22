using Injection;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace GameScene.UI
{
    public class WinScreen : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button nextButton;

        [Inject] private SignalBus _signalBus;

        private void OnEnable()
        {
            _signalBus.Subscribe<OnWin>(OnWin);
          
            nextButton.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _signalBus.Unsubscribe<OnWin>(OnWin);

            nextButton.onClick.RemoveListener(OnClick);
        }

        private void Start()
        {
            panel.SetActive(false);
        }


        private void OnWin()
        {
            panel.SetActive(true);
        }

        private void OnClick()
        {
            _signalBus.Fire<NextLevel>();
            panel.SetActive(false);
        }
    }
}