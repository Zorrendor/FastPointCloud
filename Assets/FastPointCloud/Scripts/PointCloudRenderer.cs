#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace FastPointCloud
{
    public class PointCloudRenderer : MonoBehaviour
    {
        public Material material = null;

        [SerializeField] Shader shader = null;
        [SerializeField] Camera renderCamera = null;

        [SerializeField] uint renderPointsNumber = 0;

        public int PointSize { get => pointSize; set => pointSize = value; }
        [SerializeField, Range(1, 10)] int pointSize = 1;

        public float PointAlpha { get => pointAlpha; set => pointAlpha = value; }
        [SerializeField, Range(0, 1)] float pointAlpha = 1;

        public int Density
        {
            get => density;
            set
            {
                density = value;
                updateIndirectArgs = true;
            }
        }
        [SerializeField, Range(1, 100)] int density = 100;
        
        private bool updateIndirectArgs = false;

        public PLYMesh PointCloud => pointCloud;
        private PLYMesh pointCloud = null;

        private ComputeBuffer indirectArgs;
        private ComputeBuffer pointsBuffer;
        private Mesh quadsMesh;

        public bool IsInitialized => isInitialized;
        private bool isInitialized;

        const int maxPointPerMesh = 16384;

        static readonly int kPoints = Shader.PropertyToID("_Points");
        static readonly int kMVP = Shader.PropertyToID("_MVP");
        static readonly int kPointAlpha = Shader.PropertyToID("_PointAlpha");
        static readonly int kDensity = Shader.PropertyToID("_Density");
        static readonly int kUnityObjectToWorld = Shader.PropertyToID("u_unity_ObjectToWorld");
        static readonly int kScreenSize = Shader.PropertyToID("_ScreenSize");
        static readonly int kPointCount = Shader.PropertyToID("_PointCount");

        public void Init(PLYMesh pointCloud)
        {
            if (isInitialized)
            {
                this.Unload();
            }

            this.pointCloud = pointCloud;

            renderPointsNumber = (uint)pointCloud.vertexCount;

            this.GenerateMesh();
            this.UpdateIndirectArgs();

            material = new Material(shader);
            material.hideFlags = HideFlags.DontSave;

            if (SystemInfo.supportsComputeShaders)
            {
                pointsBuffer = new ComputeBuffer(pointCloud.vertices.Length, sizeof(float) * 3 + sizeof(uint), ComputeBufferType.Structured);
                pointsBuffer.SetData(pointCloud.vertices);
            }
            else
            {
                Debug.LogWarning("System doesn't support compute buffers " + SystemInfo.graphicsDeviceType.ToString());
                Texture2D pointsTexture = new Texture2D(2048, 2048, TextureFormat.RGBAFloat, 0, false);
                pointsTexture.hideFlags = HideFlags.DontSave;
                var rawData = pointsTexture.GetRawTextureData<PLYMesh.Vertex>();
                for (int i = 0; i < pointCloud.vertexCount; i++)
                {
                    rawData[i] = pointCloud.vertices[i];
                }
                pointsTexture.Apply(false, false);
                material.SetTexture("_PointsTex", pointsTexture);
            }

            isInitialized = true;
        }

        private void GenerateMesh()
        {
            quadsMesh = new Mesh();
            quadsMesh.name = "QuadsMesh";
            quadsMesh.hideFlags = HideFlags.DontSave;

            Vector3[] vertices = new Vector3[maxPointPerMesh * 4];
            int[] indices = new int[maxPointPerMesh * 6];
            for (int i = 0; i < maxPointPerMesh; i++)
            {
                int offV = i * 4;
                int offI = i * 6;
                vertices[offV] = new Vector3(-0.5f, -0.5f, i);
                vertices[offV + 1] = new Vector3(0.5f, -0.5f, i);
                vertices[offV + 2] = new Vector3(-0.5f, 0.5f, i);
                vertices[offV + 3] = new Vector3(0.5f, 0.5f, i);

                indices[offI + 0] = offV + 0;
                indices[offI + 1] = offV + 1;
                indices[offI + 2] = offV + 2;
                indices[offI + 3] = offV + 3;
                indices[offI + 4] = offV + 2;
                indices[offI + 5] = offV + 1;
            }
            quadsMesh.SetVertexBufferParams(maxPointPerMesh * 4, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
            quadsMesh.SetVertexBufferData(vertices, 0, 0, vertices.Length, 0, MeshUpdateFlags.DontRecalculateBounds);
            quadsMesh.SetIndices(indices, MeshTopology.Triangles, 0);
            quadsMesh.bounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100));
            quadsMesh.UploadMeshData(true);
        }

        private void UpdateIndirectArgs()
        {
            renderPointsNumber = (uint)Mathf.CeilToInt(pointCloud.vertices.Length / (100.0f / density));
            uint[] args = new uint[] { 0, 0, 0, 0, 0 };
            args[0] = quadsMesh.GetIndexCount(0);
            args[1] = (uint)(Mathf.CeilToInt(((float)pointCloud.vertices.Length) / (maxPointPerMesh * (100.0f / density))));
            if (indirectArgs == null)
            {
                indirectArgs = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            }      
            indirectArgs.SetData(args);
        }

        private void OnDestroy()
        {
            this.Unload();
        }

        private void Unload()
        {
            if (indirectArgs != null)
            {
                indirectArgs.Release();
                indirectArgs = null;
            }

            if (pointsBuffer != null)
            {
                pointsBuffer.Release();
                pointsBuffer = null;
            }

            if (material != null)
            {
                Destroy(material);
                material = null;
            }

            if (quadsMesh != null)
            {
                Destroy(quadsMesh);
                quadsMesh = null;
            }

            isInitialized = false;
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            if (renderCamera == null)
            {
                renderCamera = Camera.main;
            }

            if (updateIndirectArgs)
            {
                this.UpdateIndirectArgs();
                updateIndirectArgs = false;
            }

            Matrix4x4 M = this.transform.localToWorldMatrix;
            Matrix4x4 V = renderCamera.worldToCameraMatrix;
            Matrix4x4 P = GL.GetGPUProjectionMatrix(renderCamera.projectionMatrix, true);
            Matrix4x4 MVP = P * V * M;

            material.SetBuffer(kPoints, pointsBuffer);
            material.SetMatrix(kMVP, MVP);
            material.SetFloat(kPointAlpha, pointAlpha);
            material.SetFloat(kDensity, 100.0f / density);
            material.SetMatrix(kUnityObjectToWorld, M);
            material.SetVector(kScreenSize, new Vector4(Screen.width, Screen.height, (float)pointSize / Screen.width, (float)pointSize / Screen.height));
            material.SetInt(kPointCount, pointCloud.vertexCount);
            Graphics.DrawMeshInstancedIndirect(quadsMesh, 0, material, quadsMesh.bounds, indirectArgs, 0, null, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off);
        }

    #if UNITY_EDITOR
        private void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += AssemblyReloadEvents_BeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += AssemblyReloadEvents_AfterAssemblyReload;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= AssemblyReloadEvents_BeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= AssemblyReloadEvents_AfterAssemblyReload;
        }

        private void OnValidate()
        {
            updateIndirectArgs = true;
        }

        private void AssemblyReloadEvents_BeforeAssemblyReload()
        {
            this.Unload();
        }

        private void AssemblyReloadEvents_AfterAssemblyReload()
        {
            this.Init(pointCloud);
        }
    #endif
    }
}

