using System.Collections.Generic;
using DG.Tweening;
using GameScene.Managers;
using GridSystem;
using Injection;
using Lofelt.NiceVibrations;
using UnityEngine;
using Zenject;

namespace GameScene.GamePlay
{
    public class PlayerBall : MonoBehaviour
    {
        [SerializeField] private ParticleSystem trailParticle;
        [SerializeField] private Transform modelHolder;
        private float _speed = 10f;
        private float rollSpeedMultiplier = 4f;

        [Inject] private GridController _gridController;
        [Inject] private SignalBus _signalBus;

        private Node _currentNode;
        private bool _canGo = true;
        private Vector3 _baseScale;
        private Vector3 _modelBaseScale;
        private Node _lastPaintedNode;
        private List<Node> _pendingPaintNodes = new();
        private int _pendingPaintIndex;
        private int _moveAxisX;
        private int _moveAxisY;
        private int _moveAxisWorldSignX;
        private int _moveAxisWorldSignY;
        private Vector2 _lastMoveDir;

        private void Awake()
        {
            _signalBus.Subscribe<OnSwipe>(OnSwipeReceived);
            _signalBus.Subscribe<OnLevelLoad>(OnLevelLoad);
            _signalBus.Subscribe<OnWin>(OnWin);
            _baseScale = transform.localScale;
            _modelBaseScale = modelHolder != null ? modelHolder.localScale : Vector3.one;
        }

        private void OnDestroy()
        {
            _signalBus.Unsubscribe<OnSwipe>(OnSwipeReceived);
            _signalBus.Unsubscribe<OnLevelLoad>(OnLevelLoad);
            _signalBus.Unsubscribe<OnWin>(OnWin);
            DOTween.Kill(this);

            if (_currentNode != null)
                _currentNode.IsAvailble = true;
        }

        private void OnWin(OnWin obj)
        {
            trailParticle.Stop();
        }

        private void OnLevelLoad(OnLevelLoad onLevelLoad)
        {
            _speed = onLevelLoad.BallSpeed;
            rollSpeedMultiplier = onLevelLoad.RollSpeedMultiplier;
            var node = _gridController.Grid.GetNodeWithoutCoord(onLevelLoad.StartPos.x, onLevelLoad.StartPos.y);
            transform.position = new Vector3(node.XPos, node.YPos);
            node.IsAvailble = false;
            _currentNode = node;
            _lastPaintedNode = node;
            node.GridVisual.Paint();
            trailParticle.Play();
        }

        private void OnSwipeReceived(OnSwipe onSwipe)
        {
            if (!_canGo || _currentNode == null) return;

            Node targetNode = FindTargetNode(onSwipe.Direction);

            if (targetNode == null)
            {
                return;
            }

            MoveToNode(targetNode);
        }

        private Node FindTargetNode(Direction direction)
        {
            int x = _currentNode.X;
            int y = _currentNode.Y;
            Node lastAvailable = null;

            switch (direction)
            {
                case Direction.Up:
                    for (int i = y - 1; i >= 0; i--)
                    {
                        var n = _gridController.Grid.GetNodeWithoutCoord(x, i);
                        if (!n.IsAvailble) break;
                        lastAvailable = n;
                    }
                    break;

                case Direction.Down:
                    for (int i = y + 1; i < _gridController.Grid.height; i++)
                    {
                        var n = _gridController.Grid.GetNodeWithoutCoord(x, i);
                        if (!n.IsAvailble) break;
                        lastAvailable = n;
                    }
                    break;

                case Direction.Left:
                    for (int i = x - 1; i >= 0; i--)
                    {
                        var n = _gridController.Grid.GetNodeWithoutCoord(i, y);
                        if (!n.IsAvailble) break;
                        lastAvailable = n;
                    }
                    break;

                case Direction.Right:
                    for (int i = x + 1; i < _gridController.Grid.width; i++)
                    {
                        var n = _gridController.Grid.GetNodeWithoutCoord(i, y);
                        if (!n.IsAvailble) break;
                        lastAvailable = n;
                    }
                    break;
            }

            return lastAvailable;
        }

        private void MoveToNode(Node targetNode)
        {
            _canGo = false;

            var moveDir = new Vector2(targetNode.XPos - _currentNode.XPos, targetNode.YPos - _currentNode.YPos);
            moveDir.Normalize();
            _lastMoveDir = moveDir;
            PlaySquish(moveDir);
            _lastPaintedNode = _currentNode;
            BuildPendingPaintPath(_currentNode, targetNode);
            StartRoll(moveDir);

            var pos = new Vector2(targetNode.XPos, targetNode.YPos);

            transform.DOMove(pos, _speed)
                .SetSpeedBased()
                .SetEase(Ease.InCubic)
                .OnUpdate(PaintCurrentNode)
                .OnComplete(() =>
                {
                    DOTween.Kill(this);
                    DOTween.Kill(modelHolder != null ? modelHolder : transform);
                    transform.localScale = _baseScale;
                    ResetModelScale();
                    PlayEndSquishBump();
                    _currentNode.IsAvailble = true;

                    _currentNode = targetNode;
                    _currentNode.IsAvailble = false;

                    _canGo = true;
                });
        }


        private void PlaySquish(Vector2 moveDir)
        {
            DOTween.Kill(this);

            bool horizontal = Mathf.Abs(moveDir.x) >= Mathf.Abs(moveDir.y);
            float stretch = 1.4f;
            float squish = 0.6f;

            Vector3 targetScale = horizontal
                ? new Vector3(_baseScale.x * stretch, _baseScale.y * squish, _baseScale.z)
                : new Vector3(_baseScale.x * squish, _baseScale.y * stretch, _baseScale.z);

            transform.DOScale(targetScale, 0.12f)
                .SetEase(Ease.OutQuad)
                .SetTarget(this)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void PlayEndSquishBump()
        {
            bool horizontal = Mathf.Abs(_lastMoveDir.x) > Mathf.Abs(_lastMoveDir.y);
            float stretch = 1.3f;
            float squish = 0.7f;
            Vector3 targetScale = horizontal
                ? new Vector3(_baseScale.x * stretch, _baseScale.y * squish, _baseScale.z)
                : new Vector3(_baseScale.x * squish, _baseScale.y * stretch, _baseScale.z);

            transform.DOScale(targetScale, 0.08f)
                .SetEase(Ease.OutQuad)
                .SetTarget(this)
                .SetLoops(2, LoopType.Yoyo);
        }

        private void ResetModelScale()
        {
            if (modelHolder == null)
                return;

            modelHolder.localScale = _modelBaseScale;
        }

        private void StartRoll(Vector2 moveDir)
        {
            if (moveDir.sqrMagnitude <= 0f)
                return;

            Vector3 rotateDirection;
            if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y))
                rotateDirection = new Vector3(0f, moveDir.x > 0f ? -360f : 360f, 0f);
            else
                rotateDirection = new Vector3(moveDir.y > 0f ? 360f : -360f, 0f, 0f);

            var rotTarget = modelHolder != null ? modelHolder : transform;
            rotTarget.DOLocalRotate(rotateDirection * rollSpeedMultiplier, 0.2f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetTarget(rotTarget)
                .SetLoops(-1, LoopType.Restart)
                .SetRelative();
        }

        private void PaintCurrentNode()
        {
            if (_pendingPaintNodes == null || _pendingPaintNodes.Count == 0)
                return;

            while (_pendingPaintIndex < _pendingPaintNodes.Count)
            {
                var node = _pendingPaintNodes[_pendingPaintIndex];
                if (node == null)
                {
                    _pendingPaintIndex++;
                    continue;
                }

                if (HasEnteredNode(node))
                {
                    _pendingPaintIndex++;
                    PaintNode(node);
                    continue;
                }

                break;
            }
        }

        private bool HasEnteredNode(Node node)
        {
            float pos = _moveAxisX != 0 ? transform.position.x : transform.position.y;
            float target = _moveAxisX != 0 ? node.XPos : node.YPos;

            int worldSign = _moveAxisX != 0 ? _moveAxisWorldSignX : _moveAxisWorldSignY;
            return worldSign >= 0 ? pos >= target : pos <= target;
        }

        private void PaintNode(Node node)
        {
            if (node == null || node == _lastPaintedNode)
                return;

            _lastPaintedNode = node;
            if (node.GridVisual != null)
                node.GridVisual.Paint();
            HapticManager.instance.PlayHapticPattern(HapticPatterns.PresetType.LightImpact);
            _signalBus.Fire(new OnNodePainted { X = node.X, Y = node.Y });
        }

        private void BuildPendingPaintPath(Node from, Node to)
        {
            _pendingPaintNodes.Clear();
            _pendingPaintIndex = 0;

            if (from == null || to == null)
                return;

            int dx = to.X - from.X;
            int dy = to.Y - from.Y;
            _moveAxisX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
            _moveAxisY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);
            _moveAxisWorldSignX = 0;
            _moveAxisWorldSignY = 0;

            if (_moveAxisX != 0)
                _moveAxisWorldSignX = to.XPos >= from.XPos ? 1 : -1;
            if (_moveAxisY != 0)
                _moveAxisWorldSignY = to.YPos >= from.YPos ? 1 : -1;

            int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
            int x = from.X;
            int y = from.Y;
            for (int i = 0; i < steps; i++)
            {
                x += _moveAxisX;
                y += _moveAxisY;
                var node = _gridController.Grid.GetNodeWithoutCoord(x, y);
                if (node != null)
                    _pendingPaintNodes.Add(node);
            }
        }
    }
}
