#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class DialogueGraphView : GraphView {
    // Add to Custom Libiary
    // DialogueEditor

    public DialogueEditor Editor { get; set; }

    private Dialogue m_Dialogue;
    
    public static DialogueSearch searchWindow;
    

    private readonly List<DialogueEntry> NodesCopy = new();

    public DialogueGraphView() {
        // grid back ground
        Insert(0, new GridBackground());

        SetupZoom(0.25f, 4.0f);
        // this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // style sheet
        var styleSheet =
            (StyleSheet) EditorGUIUtility.Load(
                "Assets/Scripts/Tools/DialogueSystem/Editor/StyleSheet/DialogueGraphView.uss");
        styleSheets.Add(styleSheet);

        serializeGraphElements += CopyOperation;
        unserializeAndPaste += PasteOperation;

        Undo.undoRedoPerformed += OnUndoRedo;
        
        // search window
        searchWindow = ScriptableObject.CreateInstance<DialogueSearch>();
        searchWindow.Initialized(this);
        
        nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
    }

    private void OnUndoRedo() {
        PopulateView(m_Dialogue);
        AssetDatabase.SaveAssets();
    }

    private void PasteOperation(string operationname, string data) {
        ClearSelection();

        var guidMapDic = new Dictionary<string, string>();

        foreach (var node in NodesCopy) {
            var obj = NewDialogueEntry(node.GetType(),
                contentViewContainer.ChangeCoordinatesTo(this, node.localtion), true);
            guidMapDic[node.guid] = obj.guid;
        }

        foreach (var node in NodesCopy) {
            // new paste guid
            var guid = guidMapDic[node.guid];

            var children = m_Dialogue.GetChildren(node);
            children.ForEach(child => {
                if (!guidMapDic.TryGetValue(child.guid, out var childGuid)) return;

                var parentNode = GetNodeByGuid(guid) as DialogueEntryNode;
                var childNode = GetNodeByGuid(childGuid) as DialogueEntryNode;

                if (parentNode == null || childNode == null) return;

                var edge = parentNode.OutputPort.ConnectTo<DialogueEdge>(childNode.InputPort);
                AddElement(edge);

                m_Dialogue.AddChild(parentNode.Entry, childNode.Entry);
            });
        }

        guidMapDic.Clear();
    }
    
    private string CopyOperation(IEnumerable<GraphElement> elements) {
        NodesCopy.Clear();

        foreach (var e in elements) {
            if (e is DialogueEntryNode {Entry: not DialogueRoot} n) {
                NodesCopy.Add(n.Entry);
            }
        }

        return NodesCopy.Count > 0 ? "Copy" : "";
    }


    public void PopulateView(Dialogue tree, string rootName = "") {
        m_Dialogue = tree;

        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements);
        graphViewChanged += OnGraphViewChanged;
        var rootLocation = new Vector2(layout.width * 0.5f, layout.height * 0.1f);
        if (tree.TreeRoot == null) {
            tree.TreeRoot = tree.CreateDialogueEntry(typeof(DialogueRoot), rootLocation) as DialogueRoot;
            EditorUtility.SetDirty(tree);
        }

        if (!string.IsNullOrEmpty(rootName) && tree.TreeRoot != null) {
            tree.TreeRoot.Description = rootName;
        }
        
        foreach (var obj in m_Dialogue.Nodes) {
            // create nodes
            InstanceNode(obj);
        }

        // create edges
        foreach (var obj in m_Dialogue.Nodes) {
            var children = m_Dialogue.GetChildren(obj);
            children.ForEach(child => {
                var parentNode = GetNodeByGuid(obj.guid) as DialogueEntryNode;
                var childNode = GetNodeByGuid(child.guid) as DialogueEntryNode;

                if (parentNode == null || childNode == null) return;

                var edge = parentNode.OutputPort.ConnectTo<DialogueEdge>(childNode.InputPort);
                AddElement(edge);
            });
        }

        // RecalculatePriority();
        
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change) {
        // delete Entry
        change.elementsToRemove?.ForEach(elem => {
            switch (elem) {
                case DialogueEntryNode node:
                    m_Dialogue.DeleteNode(node.Entry);
                    break;
                case Edge edge: {
                    if (edge.output.node is DialogueEntryNode output && edge.input.node is DialogueEntryNode input)
                        m_Dialogue.RemoveChild(output.Entry, input.Entry);
                    break;
                }
            }
        });

        // add edge
        change.edgesToCreate?.ForEach(edge => {
            if (edge.output.node is DialogueEntryNode output && edge.input.node is DialogueEntryNode input)
                AddChild(output.Entry, input.Entry);
        });

        m_Dialogue.TreeRoot.UpdateChildren();

        return change;
    }


    public void AddChild(DialogueEntry entryParent, DialogueEntry entryChild) {
        m_Dialogue.AddChild(entryParent, entryChild);
        // RecalculatePriority();
    }


    public DialogueEntry NewDialogueEntry(Type t, Vector2 mousePosition, bool selected = false) {
        var location = this.ChangeCoordinatesTo(contentViewContainer, mousePosition);
        
        var obj = m_Dialogue.CreateDialogueEntry(t, location);
        var node = InstanceNode(obj, selected);
        
        node.SetPosition(new Rect(location + new Vector2(-122, -41), Vector2.zero));
        return obj;
    }
    
    private DialogueEntryNode InstanceNode(DialogueEntry obj, bool selected = false) {
        var n = new DialogueEntryNode(obj) {
            onNodeSelected = OnSelected,
            onNodeUnSelected = OnUnSelected
        };
        
        
        AddElement(n);
        if (selected) {
            AddToSelection(n);
        }

        return n;
    }

    private void OnSelected(DialogueEntryNode node) {
        Selection.SetActiveObjectWithContext(node.Entry,node.Entry);
    }

    private void OnUnSelected(DialogueEntryNode node) {
        Selection.SetActiveObjectWithContext(m_Dialogue,m_Dialogue);
    }
    
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
        if (!m_Dialogue) return;
  
        // base.BuildContextualMenu(evt);
        var location = evt.localMousePosition;
        {
            var types = TypeCache.GetTypesDerivedFrom<DialogueEntry>();
            foreach (var t in types.Where(t => t != typeof(DialogueRoot))) {
                // ReSharper disable once PossibleNullReferenceException
                evt.menu.AppendAction($"{t.Name}", (_) => NewDialogueEntry(t, location));
            }
        }
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
        return ports.Where(endPort =>
            endPort.direction != startPort.direction &&
            endPort.node != startPort.node &&
            endPort.portType == startPort.portType
        ).ToList();
    }
}
#endif