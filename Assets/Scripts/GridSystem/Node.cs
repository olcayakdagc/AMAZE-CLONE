using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridSystem
{
    public class Node
    {
        private Grid<Node> _grid;
        private float _xPos;
        public float XPos { get { return _xPos; } }
        private float _yPos;
        public float YPos { get { return _yPos; } }

        private int _x;
        public int X { get { return _x; } }
        private int _y;
        public int Y { get { return _y; } }

        public bool IsAvailble;
        

        public Node CameFromNode;
        public GridVisual GridVisual;

        
        public Node(Grid<Node> grid, float xPos, float yPos, int x, int y)
        {
            this._grid = grid;
            this._xPos = xPos;
            this._yPos = yPos;
            this._x = x;
            this._y = y;
            IsAvailble = true;
        }

        public List<Node> GetNeighbourList()
        {
            List<Node> list = new List<Node>();
            if (_x - 1 >= 0)
            {
                //Left
                list.Add(_grid.GetNodeWithoutCoord((int)(X - 1), Y));
                //LeftDown
                if (Y - 1 >= 0)
                {
                    list.Add(_grid.GetNodeWithoutCoord(X - 1, Y - 1));
                }
                //LeftUp
                if (Y + 1 < _grid.height)
                {
                    list.Add(_grid.GetNodeWithoutCoord(X - 1, Y + 1));
                }
            }

            if (_x + 1 < _grid.width)
            {
                //Right
                list.Add(_grid.GetNodeWithoutCoord((int)(X + 1), Y));
                //RightDown
                if (Y - 1 >= 0)
                {
                    list.Add(_grid.GetNodeWithoutCoord(X + 1, Y - 1));
                }
                //RightUp
                if (Y + 1 < _grid.height)
                {
                    list.Add(_grid.GetNodeWithoutCoord(X + 1, Y + 1));
                }
            }
            //Down
            if (Y - 1 >= 0)
            {
                list.Add(_grid.GetNodeWithoutCoord(X, Y - 1));
            }
            //Up
            if (Y + 1 < _grid.height)
            {
                list.Add(_grid.GetNodeWithoutCoord(X, Y + 1));
            }
            return list;
        }
        
    }
}