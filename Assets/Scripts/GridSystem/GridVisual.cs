using UnityEngine;

namespace GridSystem
{
    public class GridVisual : MonoBehaviour
    {
        [SerializeField] private MeshRenderer wall;
        [SerializeField] private MeshRenderer ground;

        private Material _groundMat;
        private Material _wallMat;
        private Material _paintMat;

        public void SetMaterials(Material groundMat, Material wallMat, Material paintMat)
        {
            _groundMat = groundMat;
            _wallMat = wallMat;
            _paintMat = paintMat;

            ground.sharedMaterial = _groundMat;
            wall.sharedMaterial = _wallMat;
        }

        public void SetWall()
        {
            wall.gameObject.SetActive(true);
            ground.gameObject.SetActive(false);
            wall.sharedMaterial = _wallMat;
        }

        public void SetGround()
        {
            wall.gameObject.SetActive(false);
            ground.gameObject.SetActive(true);
            ground.sharedMaterial = _groundMat;
        }

        public void Paint()
        {
            wall.gameObject.SetActive(false);
            ground.gameObject.SetActive(true);
            ground.sharedMaterial = _paintMat != null ? _paintMat : _groundMat;
        }
    }
}
