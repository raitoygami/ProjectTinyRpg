#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;


    public class DialogueEditor : EditorWindow {
        private static DialogueEditor instance;
        
        private Dialogue m_Dialogue;
        private DialogueGraphView m_GraphView;

        [SerializeField] private VisualTreeAsset m_VisualTreeAsset;

        private static DialogueSettings m_Settings;

        private bool m_FrameAllAfterLayout;
        
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line) {
            
            var tree = EditorUtility.EntityIdToObject(instanceID) as Dialogue;
            if (!tree)
                return false;

            var wnd = GetWindow<DialogueEditor>();
            wnd.titleContent = new GUIContent("DialogueEditor");
            
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

            m_Settings = CreateInstance<DialogueSettings>();
            if (m_Settings != null){
                m_Settings.instanceid = instanceID;
                AssetDatabase.CreateAsset(m_Settings, "Assets/Scripts/Tools/DialogueSystem/Editor/BTEditorSetting.asset");  
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
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Tools/DialogueSystem/Editor/DialogueEditor.uxml");
            m_VisualTreeAsset.CloneTree(root);

            m_GraphView = root.Q<DialogueGraphView>("GraphView");
            m_GraphView.Editor = this;
            m_FrameAllAfterLayout = true;
            
            m_GraphView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            minSize = new Vector2(1080, 720);
            maxSize = new Vector2(1080, 720);

            m_Settings = AssetDatabase.LoadAssetAtPath<DialogueSettings>("Assets/Scripts/Tools/DialogueSystem/Editor/BTEditorSetting.asset");
            if (!m_Settings) return;
            
            var tree = EditorUtility.EntityIdToObject(m_Settings.instanceid) as Dialogue;
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

        private void UpdateTree(Dialogue dialogue, string rootName ="") {
            if (!dialogue) return;
            
            m_Dialogue = dialogue;
            m_GraphView.PopulateView(m_Dialogue, rootName);
            
            m_GraphView.FrameAll();
        }
        
    }

#endif
