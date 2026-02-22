using System.Collections.Generic;
using DG.Tweening;
using Injection;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace GameScene.UI
{
    public class GameplayScreen : MonoBehaviour
    {
        [SerializeField] private GameObject gameplayScreen;
        [SerializeField] private Button resetButton;
        [SerializeField] private List<RectTransform> fromTop = new();
        [SerializeField] private List<RectTransform> fromRight = new();
        [SerializeField] private float enterOffset = 400f;
        [SerializeField] private float enterDuration = 0.35f;
        [Inject] private SignalBus _signalBus;
        private readonly Dictionary<RectTransform, Vector2> _homePositions = new();

        private void OnEnable()
        {
            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetClicked);

            _signalBus.Subscribe<OnWin>(OnWin);
            _signalBus.Subscribe<OnLevelLoad>(OnLevelLoad);
            _signalBus.Subscribe<OnLevelUnload>(OnLevelUnload);
            _signalBus.Subscribe<Restart>(OnRestart);
        }

        private void OnDisable()
        {
            if (resetButton != null)
                resetButton.onClick.RemoveListener(OnResetClicked);

            _signalBus.Unsubscribe<OnWin>(OnWin);
            _signalBus.Unsubscribe<OnLevelLoad>(OnLevelLoad);
            _signalBus.Unsubscribe<OnLevelUnload>(OnLevelUnload);
            _signalBus.Unsubscribe<Restart>(OnRestart);
        }

        private void OnResetClicked()
        {
            _signalBus.Fire<Restart>();
        }

        private void OnWin()
        {
            PlayExitAnimations();
        }

        private void OnLevelLoad(OnLevelLoad _)
        {
            gameplayScreen.SetActive(true);
            PlayEnterAnimations();
        }

        private void OnLevelUnload(OnLevelUnload _)
        {
            PlayExitAnimations();
        }

        private void OnRestart()
        {
            PlayExitAnimations();
        }

        private void PlayEnterAnimations()
        {
            foreach (var rt in fromTop)
            {
                if (rt == null) continue;
                var target = GetHome(rt);
                rt.anchoredPosition = new Vector2(target.x, target.y + enterOffset);
                rt.DOAnchorPos(target, enterDuration).SetEase(Ease.OutCubic);
            }

            foreach (var rt in fromRight)
            {
                if (rt == null) continue;
                var target = GetHome(rt);
                rt.anchoredPosition = new Vector2(target.x + enterOffset, target.y);
                rt.DOAnchorPos(target, enterDuration).SetEase(Ease.OutCubic);
            }
        }

        private void PlayExitAnimations()
        {
            foreach (var rt in fromTop)
            {
                if (rt == null) continue;
                var target = GetHome(rt);
                rt.DOAnchorPos(new Vector2(target.x, target.y + enterOffset), enterDuration).SetEase(Ease.InCubic);
            }

            foreach (var rt in fromRight)
            {
                if (rt == null) continue;
                var target = GetHome(rt);
                rt.DOAnchorPos(new Vector2(target.x + enterOffset, target.y), enterDuration).SetEase(Ease.InCubic);
            }
        }

        private Vector2 GetHome(RectTransform rt)
        {
            if (_homePositions.TryGetValue(rt, out var pos))
                return pos;
            pos = rt.anchoredPosition;
            _homePositions[rt] = pos;
            return pos;
        }
    }
}
