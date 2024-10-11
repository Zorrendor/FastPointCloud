using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FastPointCloud
{
    public class PointCloudUpdater : MonoBehaviour
    {
        [SerializeField] private PointCloudSO pointCloudSO;

        private PointCloudRenderer PointCloudRenderer
        {
            get
            {
                if (pointCloudRenderer == null)
                {
                    pointCloudRenderer = this.GetComponent<PointCloudRenderer>();
                }
                return pointCloudRenderer;
            }
        }
        private PointCloudRenderer pointCloudRenderer = null;

        private void OnEnable()
        {
            pointCloudSO.onLoadPLYMesh += OnLoadPLYMesh;
            pointCloudSO.pointSize.AddListener(OnPointSizeChanged);
            pointCloudSO.pointDensity.AddListener(OnPointDensityChanged);
            pointCloudSO.pointAlpha.AddListener(OnPointAlphaChanged);
        }

        private void OnDisable()
        {
            pointCloudSO.onLoadPLYMesh -= OnLoadPLYMesh;
            pointCloudSO.pointSize.RemoveListener(OnPointSizeChanged);
            pointCloudSO.pointDensity.RemoveListener(OnPointDensityChanged);
            pointCloudSO.pointAlpha.RemoveListener(OnPointAlphaChanged);
        }
        
        private void OnLoadPLYMesh()
        {
            PointCloudRenderer.Init(pointCloudSO.PLYMesh);
        }
        
        private void OnPointAlphaChanged(float value)
        {
            PointCloudRenderer.PointAlpha = value;
        }

        private void OnPointDensityChanged(int value)
        {
            PointCloudRenderer.Density = value;
        }

        private void OnPointSizeChanged(int value)
        {
            PointCloudRenderer.PointSize = value;
        }
    }
}

