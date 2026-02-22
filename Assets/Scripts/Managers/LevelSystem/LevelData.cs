using System;
using UnityEngine;

namespace Managers.LevelSystem
{
    [Serializable]
    public class LevelData
    {
        public int width;
        public int height;

        public bool[] grid;

        public Vector2Int startNode;

        public string groundMaterialPath;
        public string wallMaterialPath;
        public string paintMaterialPath;
        

        public LevelData(int width, int height)
        {
            this.width = Mathf.Max(1, width);
            this.height = Mathf.Max(1, height);

            grid = new bool[this.width * this.height];
            for (int i = 0; i < grid.Length; i++)
                grid[i] = true;

            startNode = new Vector2Int(0, 0);
        }

        public int ToIndex(int x, int y) => x + y * width;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

        public bool Get(int x, int y)
        {
            if (!InBounds(x, y) || grid == null) return false;
            int idx = ToIndex(x, y);
            if (idx < 0 || idx >= grid.Length) return false;
            return grid[idx];
        }

        public void Set(int x, int y, bool value)
        {
            if (!InBounds(x, y) || grid == null) return;
            int idx = ToIndex(x, y);
            if (idx < 0 || idx >= grid.Length) return;
            grid[idx] = value;
        }

        public bool IsValid()
        {
            return width > 0 && height > 0 && grid != null && grid.Length == width * height;
        }
    }
}
