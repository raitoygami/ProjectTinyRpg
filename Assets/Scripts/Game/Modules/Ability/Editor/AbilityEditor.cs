#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;


    public class AbilityEditor : EditorWindow {
        private static AbilityEditor instance;
        public static string AbilityEditorDirRoot = "Assets/Scripts/Game/Modules/Ability/Editor/"; 
        private Ability m_Ability;
        private AbilityGraphView m_GraphView;

        [SerializeField] private VisualTreeAsset m_VisualTreeAsset;

        private static AbilitySettings m_Settings;

        private bool m_FrameAllAfterLayout;
        
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line) {
            
            var tree = EditorUtility.EntityIdToObject(instanceID) as Ability;
            if (!tree)
                return false;

            var wnd = GetWindow<AbilityEditor>();
            wnd.titleContent = new GUIContent("AbilityEditor");
            
            // Object oo = Resources.Load("CineAsset/PlaneCine");
            //
            // Test_ScriptObject test_ScriptObject = ScriptableObject.Instantiate(oo) as Test_ScriptObject;
            //
            // Debug.LogFormat("aaaaaaaaaaaaaa___{0}", test_ScriptObject.m_szPoints.Count);
            //
            // test_ScriptObject.m_szPoints.Clear();
            //
            // Debug.LogFormat("bbbbbbbbbbbbbb___{0}", test_ScriptObject.m_szPoints.Count);
            // wnd.m_Settings.instanceid = instanceID;

            m_Settings = CreateInstance<AbilitySettings>();
            if (m_Settings != null){
                m_Settings.instanceid = instanceID;
                AssetDatabase.CreateAsset(m_Settings, $"{AbilityEditorDirRoot}AbilitySetting.asset");  
            } 
            
            // m_Settings = CreateInstance(settings);
            var fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath((EntityId)instanceID));
            instance.UpdateTree(tree, fileName);
            wnd.Focus();
            return true;
        }

        private void OnEnable() {
            instance = this;
        }

        private void OnDisable() {
            instance = null;
        }

        public void CreateGUI() {

            // Each editor window contains a root VisualElement object
            var root = rootVisualElement;
            // Instantiate UXML
            m_VisualTreeAsset =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{AbilityEditorDirRoot}AbilityEditor.uxml");
            m_VisualTreeAsset.CloneTree(root);

            m_GraphView = root.Q<AbilityGraphView>("GraphView");
            m_GraphView.Editor = this;
            m_FrameAllAfterLayout = true;
            
            m_GraphView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            minSize = new Vector2(1080, 720);
            maxSize = new Vector2(1080, 720);
            
            m_Settings = AssetDatabase.LoadAssetAtPath<AbilitySettings>($"{AbilityEditorDirRoot}AbilitySetting.asset");
            if (!m_Settings) return;
            
            var tree = EditorUtility.EntityIdToObject(m_Settings.instanceid) as Ability;
            UpdateTree(tree);
            
            Repaint();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) {
            if (m_GraphView == null)
                return;

            // this callback is only so we can run post-layout behaviors after the graph loads for the first time
            // we immediately unregister it so it doesn't get called again
            m_GraphView.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (m_FrameAllAfterLayout)
                m_GraphView.FrameAll();
            m_FrameAllAfterLayout = false;
        }

        private void UpdateTree(Ability Ability, string rootName ="") {
            if (!Ability) return;
            
            m_Ability = Ability;
            m_GraphView.PopulateView(m_Ability, rootName);
            
            m_GraphView.FrameAll();
        }
        
    }

#endif
