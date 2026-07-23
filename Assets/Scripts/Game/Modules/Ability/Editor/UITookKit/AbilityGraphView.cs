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
public partial class AbilityGraphView : GraphView {
    // Add to Custom Libiary
    // AbilityEditor

    public AbilityEditor Editor { get; set; }

    private Ability m_Ability;
    
    public static AbilitySearch searchWindow;
    

    private readonly List<AbilityEffect> NodesCopy = new();

    public AbilityGraphView() {
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
                $"{AbilityEditor.AbilityEditorDirRoot}StyleSheet/GraphView.uss");
        styleSheets.Add(styleSheet);

        serializeGraphElements += CopyOperation;
        unserializeAndPaste += PasteOperation;

        Undo.undoRedoPerformed += OnUndoRedo;
        
        // search window
        searchWindow = ScriptableObject.CreateInstance<AbilitySearch>();
        searchWindow.Initialized(this);
        
        nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
    }

    private void OnUndoRedo() {
        PopulateView(m_Ability);
        AssetDatabase.SaveAssets();
    }

    private void PasteOperation(string operationname, string data) {
        ClearSelection();

        var guidMapDic = new Dictionary<string, string>();

        foreach (var node in NodesCopy) {
            var obj = NewAbilityEntry(node.GetType(),
                contentViewContainer.ChangeCoordinatesTo(this, node.localtion), true);
            guidMapDic[node.guid] = obj.guid;
        }

        foreach (var node in NodesCopy) {
            // new paste guid
            var guid = guidMapDic[node.guid];

            var children = m_Ability.GetChildren(node);
            children.ForEach(child => {
                if (!guidMapDic.TryGetValue(child.guid, out var childGuid)) return;

                var parentNode = GetNodeByGuid(guid) as AbilityEffectNode;
                var childNode = GetNodeByGuid(childGuid) as AbilityEffectNode;

                if (parentNode == null || childNode == null) return;

                var edge = parentNode.OutputPort.ConnectTo<AbilityEdge>(childNode.InputPort);
                AddElement(edge);

                m_Ability.AddChild(parentNode.Entry, childNode.Entry);
            });
        }

        guidMapDic.Clear();
    }
    
    private string CopyOperation(IEnumerable<GraphElement> elements) {
        NodesCopy.Clear();

        foreach (var e in elements) {
            if (e is AbilityEffectNode {Entry: not AbilityRoot} n) {
                NodesCopy.Add(n.Entry);
            }
        }

        return NodesCopy.Count > 0 ? "Copy" : "";
    }


    public void PopulateView(Ability tree, string rootName = "") {
        m_Ability = tree;

        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements);
        graphViewChanged += OnGraphViewChanged;
        var rootLocation = new Vector2(layout.width * 0.5f, layout.height * 0.1f);
        if (tree.TreeRoot == null) {
            tree.TreeRoot = tree.CreateEffect(typeof(AbilityRoot), rootLocation) as AbilityRoot;
            EditorUtility.SetDirty(tree);
        }

        /*if (!string.IsNullOrEmpty(rootName) && tree.TreeRoot != null) {
            tree.TreeRoot.Description = rootName;
        }*/
        
        foreach (var obj in m_Ability.Effects) {
            // create nodes
            InstanceNode(obj);
        }

        // create edges
        foreach (var obj in m_Ability.Effects) {
            var children = m_Ability.GetChildren(obj);
            children.ForEach(child => {
                var parentNode = GetNodeByGuid(obj.guid) as AbilityEffectNode;
                var childNode = GetNodeByGuid(child.guid) as AbilityEffectNode;

                if (parentNode == null || childNode == null) return;

                var edge = parentNode.OutputPort.ConnectTo<AbilityEdge>(childNode.InputPort);
                AddElement(edge);
            });
        }

        // RecalculatePriority();
        
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change) {
        // delete Entry
        change.elementsToRemove?.ForEach(elem => {
            switch (elem) {
                case AbilityEffectNode node:
                    m_Ability.DeleteNode(node.Entry);
                    break;
                case Edge edge: {
                    if (edge.output.node is AbilityEffectNode output && edge.input.node is AbilityEffectNode input)
                        m_Ability.RemoveChild(output.Entry, input.Entry);
                    break;
                }
            }
        });

        // add edge
        change.edgesToCreate?.ForEach(edge => {
            if (edge.output.node is AbilityEffectNode output && edge.input.node is AbilityEffectNode input)
                AddChild(output.Entry, input.Entry);
        });

        m_Ability.TreeRoot.UpdateChildren();

        return change;
    }


    public void AddChild(AbilityEffect entryParent, AbilityEffect entryChild) {
        m_Ability.AddChild(entryParent, entryChild);
        // RecalculatePriority();
    }


    public AbilityEffect NewAbilityEntry(Type t, Vector2 mousePosition, bool selected = false) {
        var location = this.ChangeCoordinatesTo(contentViewContainer, mousePosition);
        
        var obj = m_Ability.CreateEffect(t, location);
        var node = InstanceNode(obj, selected);
        
        node.SetPosition(new Rect(location + new Vector2(-122, -41), Vector2.zero));
        return obj;
    }
    
    private AbilityEffectNode InstanceNode(AbilityEffect obj, bool selected = false) {
        var n = new AbilityEffectNode(obj) {
            onNodeSelected = OnSelected,
            onNodeUnSelected = OnUnSelected
        };
        
        
        AddElement(n);
        if (selected) {
            AddToSelection(n);
        }

        return n;
    }

    private void OnSelected(AbilityEffectNode node) {
        Selection.SetActiveObjectWithContext(node.Entry,node.Entry);
    }

    private void OnUnSelected(AbilityEffectNode node) {
        Selection.SetActiveObjectWithContext(m_Ability,m_Ability);
    }
    
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
        if (!m_Ability) return;
  
        // base.BuildContextualMenu(evt);
        var location = evt.localMousePosition;
        {
            var types = TypeCache.GetTypesDerivedFrom<AbilityEffect>();
            foreach (var t in types.Where(t => t != typeof(AbilityRoot))) {
                // ReSharper disable once PossibleNullReferenceException
                evt.menu.AppendAction(AbilityEffect.GetClassify(t), (_) => NewAbilityEntry(t, location));
                /*evt.menu.AppendAction(AbilityEffect.GetClassify(t), (_) => NewAbilityEntry(t, location));*/
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