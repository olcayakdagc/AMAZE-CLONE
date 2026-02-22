using DG.Tweening;
using Injection;
using UnityEngine;
using Zenject;

namespace Managers
{
    public class CameraManager : MonoBehaviour
    {
        [Inject] private SignalBus _signalBus;
        private Tween _moveTween;
        private Vector3 _lastTargetPos;
        private float _lastGridHeightWorld;
        private bool _isOffscreen;

        private void Start()
        {
            _signalBus.Subscribe<OnLevelLoad>(SetCamera);
            _signalBus.Subscribe<NextLevel>(OnNextLevel);
            _signalBus.Subscribe<Restart>(OnRestart);
        }

        private void OnDestroy()
        {
            _signalBus.Unsubscribe<OnLevelLoad>(SetCamera);
            _signalBus.Unsubscribe<NextLevel>(OnNextLevel);
            _signalBus.Unsubscribe<Restart>(OnRestart);
            _moveTween?.Kill();
        }

        private void SetCamera(OnLevelLoad onLevelLoad)
        {
            var cellSize = onLevelLoad.CellSize;
            var gridWidthWorld = (onLevelLoad.GridWidth + 2f) * cellSize;
            var gridHeightWorld = (onLevelLoad.GridHeight + 2f) * cellSize;

            var x = gridWidthWorld % 2 != 0 ? 0 : -(cellSize / 2);
            var targetPos = new Vector3(x, onLevelLoad.CameraYOffset, -gridWidthWorld * 2);

            _lastTargetPos = targetPos;
            _lastGridHeightWorld = gridHeightWorld;
            _isOffscreen = false;

            _moveTween?.Kill();
            transform.position = targetPos + new Vector3(0, gridHeightWorld * 2f, 0);
            _moveTween = transform.DOMove(targetPos, 0.6f).SetEase(Ease.OutCubic);
        }

        private void OnNextLevel(NextLevel nextLevel)
        {
            if (_lastGridHeightWorld <= 0f)
            {
                _signalBus.Fire<OnCameraExitComplete>();
                return;
            }

            _moveTween?.Kill();
            var offscreen = _lastTargetPos + new Vector3(0, -_lastGridHeightWorld * 2f, 0);
            _moveTween = transform.DOMove(offscreen, 0.5f)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    _isOffscreen = true;
                    _signalBus.Fire<OnCameraExitComplete>();
                });
        }

        private void OnRestart(Restart restart)
        {
            if (_lastGridHeightWorld <= 0f)
            {
                _signalBus.Fire<OnCameraExitComplete>();
                return;
            }

            _moveTween?.Kill();
            var offscreen = _lastTargetPos + new Vector3(0, -_lastGridHeightWorld * 2f, 0);
            _moveTween = transform.DOMove(offscreen, 0.5f)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    _isOffscreen = true;
                    _signalBus.Fire<OnCameraExitComplete>();
                });
        }
    }
}
