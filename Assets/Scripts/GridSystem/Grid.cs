using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridSystem
{
    public class Grid<TGridObject>
    {
        private int _height;

        public int height
        {
            get { return _height; }
        }

        private int _width;

        public int width
        {
            get { return _width; }
        }

        private TGridObject[,] _gridArray;
        private float _cellSize;
        private Vector3 _originPos;

        public Grid(int width, int height, float cellSize, Vector3 originPos,
            Func<Grid<TGridObject>, float, float, int, int, TGridObject> createGridObject)
        {
            this._width = width;
            this._height = height;
            this._cellSize = cellSize;
            this._originPos = originPos;
            _gridArray = new TGridObject[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    _gridArray[i, j] = createGridObject(this, i * _cellSize + _originPos.x, _originPos.y - j * _cellSize, i, j);
                }
            }
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x * _cellSize, _originPos.y - y * _cellSize) + new Vector3(_originPos.x, 0);

        }

        public TGridObject GetNode(float x, float y)
        {
            int _x = Mathf.RoundToInt((x - _originPos.x) / _cellSize);
            int _y = Mathf.RoundToInt((_originPos.y - y) / _cellSize);
            if (_x >= 0 && _y >= 0 && _x < _width && _y < _height)
            {
                return _gridArray[_x, _y];
            }
            else
            {
                return default(TGridObject);
            }
        }

        public TGridObject GetNodeWithoutCoord(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < _width && y < _height)
            {
                return _gridArray[x, y];
            }
            else
            {
                return default(TGridObject);
            }
        }
    }
}