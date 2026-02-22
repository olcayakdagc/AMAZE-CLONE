using UnityEngine;

namespace GridSystem
{
    [CreateAssetMenu(menuName = "Settings/GridVisualSettings")]
    public class GridVisualSettings : ScriptableObject
    {
        public GridVisual VisualPrefab;
        public int DefaultCapacity = 4;
        public int MaxSize = 1000;
    }
}