using System.Collections;
using System.Collections.Generic;
using Injection;
using UnityEngine;
using Zenject;

namespace GridSystem
{
    public class GridController
    {
        public Grid<Node> Grid { get; private set; }


        public bool IsCompleted { get; private set; }

        public float CellSize { get; private set; }

        private SignalBus _signalBus;

        public GridController(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        public void CreateGrid(int width, int height, float cellSize, bool isZ)
        {
            CellSize = cellSize;
            IsCompleted = false;
            Vector3 point = new Vector3(-width / 2 * cellSize, height / 2 * cellSize);
           
            Grid = new Grid<Node>(width, height, cellSize, point,
                (Grid<Node> g, float xPos, float yPos, int x, int y) =>
                    new Node(g, xPos, yPos, x, y));
            IsCompleted = true;
            _signalBus.Fire(new CreateSprites { IsZ = isZ });
        }
    }
}