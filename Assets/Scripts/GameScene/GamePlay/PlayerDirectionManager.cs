using System;
using Injection;
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace GameScene.GamePlay
{
    public class PlayerDirectionManager : IInitializable, IDisposable
    {
        [Inject] private PlayerInput _playerInput;
        [Inject] private GameManager _gameManager;
        [Inject] private SignalBus _signalBus;

        private float _swipeThresholdPixels;

        private const string ActionPoint = "Point";
        private const string ActionPress = "Press";

        private InputAction _pointAction;
        private InputAction _pressAction;

        private Vector2 _startPos;
        private bool _isPressing;

        [Inject]
        public void Construct([InjectOptional] float swipeThresholdPixels = 100f)
        {
            _swipeThresholdPixels = swipeThresholdPixels;
        }

        public void Initialize()
        {
            _pointAction = _playerInput.actions[ActionPoint];
            _pressAction = _playerInput.actions[ActionPress];

            _pointAction.Enable();
            _pressAction.Enable();

            _pressAction.started += OnPressStarted;
            _pressAction.canceled += OnPressCanceled;
        }

        public void Dispose()
        {
            if (_pressAction != null)
            {
                _pressAction.started -= OnPressStarted;
                _pressAction.canceled -= OnPressCanceled;
                _pressAction.Disable();
            }

            _pointAction?.Disable();

            _startPos = Vector2.zero;
            _isPressing = false;
        }

        private void OnPressStarted(InputAction.CallbackContext ctx)
        {
            if (_gameManager.GameState != GameStates.GamePlay) return;

            _isPressing = true;
            _startPos = _pointAction.ReadValue<Vector2>();
        }

        private void OnPressCanceled(InputAction.CallbackContext ctx)
        {
            if (_gameManager.GameState != GameStates.GamePlay) return;
            if (!_isPressing) return;

            _isPressing = false;

            var endPos = _pointAction.ReadValue<Vector2>();
            var direction = GetSwipeDirection(_startPos, endPos);

            if (direction != Direction.None)
                _signalBus.Fire(new OnSwipe { Direction = direction });

            _startPos = Vector2.zero;
        }

        private Direction GetSwipeDirection(Vector2 start, Vector2 end)
        {
            var delta = end - start;

            if (delta.magnitude < _swipeThresholdPixels)
                return Direction.None;

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return delta.x > 0 ? Direction.Right : Direction.Left;

            return delta.y > 0 ? Direction.Up : Direction.Down;
        }
    }
}