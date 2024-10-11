using UnityEngine;

namespace FastPointCloud
{
    [CreateAssetMenu(fileName = "PointCloudSO", menuName = "ScriptableObjects/PointCloudSO", order = 0)]
    public class PointCloudSO : ScriptableObject
    {
        private PLYMesh mesh;
        public PLYMesh PLYMesh
        {
            get => mesh;
            set
            {
                mesh = value;
                onLoadPLYMesh?.Invoke();
            }
        }

        public event System.Action onLoadPLYMesh;
        
        [SerializeField] public Observer<int> pointSize = new Observer<int>(1);
        [SerializeField] public Observer<float> pointAlpha = new Observer<float>(1.0f);
        [SerializeField] public Observer<int> pointDensity = new Observer<int>(100);
    }
}
