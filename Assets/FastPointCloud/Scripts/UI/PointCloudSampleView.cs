using UnityEngine;
using UnityEngine.UIElements;

namespace FastPointCloud
{
    public class PointCloudSampleView : UIView
    {
        const string directoryPath = "Assets/FastPointCloud/Samples";
        
        [SerializeField] private PointCloudSO pointCloudSO;

        private Button loadFromFileButton;
        private Button saveToFileButton;
        
        private SliderInt pointSizeSliderInt;
        private Slider alphaSlider;
        private SliderInt densitySliderInt;

        public override void Init(ViewController viewController)
        {
            base.Init(viewController);

            loadFromFileButton = root.Q<Button>("LoadFromFileButton");
            saveToFileButton = root.Q<Button>("SaveToFileButton");

            pointSizeSliderInt = root.Q<SliderInt>("PointSizeSliderSliderInt");
            alphaSlider = root.Q<Slider>("AlphaSlider");
            densitySliderInt = root.Q<SliderInt>("DensitySliderInt");
            
            pointSizeSliderInt.SetValueWithoutNotify(pointCloudSO.pointSize);
            alphaSlider.SetValueWithoutNotify(pointCloudSO.pointAlpha);
            densitySliderInt.SetValueWithoutNotify(pointCloudSO.pointDensity);

            pointSizeSliderInt.RegisterValueChangedCallback(evt => pointCloudSO.pointSize.Value = evt.newValue);
            alphaSlider.RegisterValueChangedCallback(evt => pointCloudSO.pointAlpha.Value = evt.newValue);
            densitySliderInt.RegisterValueChangedCallback(evt => pointCloudSO.pointDensity.Value = evt.newValue);
        }

        public override void Show()
        {
            base.Show();

            loadFromFileButton.clicked += LoadFromFileClicked;
            saveToFileButton.clicked += SaveToFileClicked;
        }
        
        public override void Hide()
        {
            base.Hide();
            
            loadFromFileButton.clicked -= LoadFromFileClicked;
            saveToFileButton.clicked -= SaveToFileClicked;
        }
        
        private void LoadFromFileClicked()
        {
#if UNITY_EDITOR
            this.Load();
#endif
        }

        private void SaveToFileClicked()
        {
            
        }
        
#if UNITY_EDITOR
        [ContextMenu("Load from file")]
        public void Load()
        {
            this.CreateSamplesFolder();
            
            string path = UnityEditor.EditorUtility.OpenFilePanel("Select PLY file", directoryPath, "ply");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
         
            this.LoadFromFile(path);
        }
        
#endif
        
        private void CreateSamplesFolder()
        {
            string projectPath = System.IO.Path.GetDirectoryName(Application.dataPath);
            System.IO.Directory.CreateDirectory(projectPath + "/" + directoryPath);
        }
        
        private void LoadFromFile(string path)
        {
            PLYFileReader fileReader = new PLYFileReader();
            PLYMesh pointCloud = fileReader.Read(path);
            pointCloudSO.PLYMesh = pointCloud;
        }
    }
}

