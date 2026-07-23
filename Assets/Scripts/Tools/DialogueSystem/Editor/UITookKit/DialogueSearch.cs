#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;


    public class DialogueSearch : ScriptableObject, ISearchWindowProvider {
        public DialogueGraphView GraphView { get; private set; }
        
        public DialoguePort DragPort { get; set; }
        public bool IsInputPort { get; set; }
        public void Initialized(DialogueGraphView graphView) {
            GraphView = graphView;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context) {
            var indentationIcon = new Texture2D(1, 1);
            indentationIcon.SetPixel(0, 0, Color.clear);
            indentationIcon.Apply();
            var ret = new List<SearchTreeEntry> {new SearchTreeGroupEntry(new GUIContent("Create Elements"))};

            var types = TypeCache.GetTypesDerivedFrom<DialogueEntry>();
            if (DragPort != null && IsInputPort) return ret;
            // actions
            // ret.Add(new SearchTreeGroupEntry(new GUIContent("Action"), 1));
            // ret.AddRange(types.Select(t => new SearchTreeEntry(new GUIContent(t.Name, indentationIcon))
            //     {userData = t, level = 1}));

            ret.AddRange(types
                .Where(t => t != typeof(DialogueRoot))
                .Select(t => new SearchTreeEntry(new GUIContent(t.Name, indentationIcon)) {userData = t, level = 1}));

            // composite
            // types = TypeCache.GetTypesDerivedFrom<DialogueComposite>();
            // ret.Add(new SearchTreeGroupEntry(new GUIContent("Composite"), 1));
            // ret.AddRange(types.Select(t => new SearchTreeEntry(new GUIContent(t.Name, indentationIcon))
            //     {userData = t, level = 2}));
            //
            //
            // types = TypeCache.GetTypesDerivedFrom<DialogueDecorator>();
            // ret.Add(new SearchTreeGroupEntry(new GUIContent("Decorator"), 1));
            // ret.AddRange(types.Select(t => new SearchTreeEntry(new GUIContent(t.Name, indentationIcon))
            //     {userData = t, level = 2}));


            return ret;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context) {
            var editor = GraphView.Editor;
            var windowMousePosition =
                editor.rootVisualElement.ChangeCoordinatesTo(editor.rootVisualElement.parent,
                    context.screenMousePosition - editor.position.position);
            
            var graphMousePosition = GraphView.contentContainer.WorldToLocal(windowMousePosition);

            var t = (Type) SearchTreeEntry.userData;
            var obj = GraphView.NewDialogueEntry(t, graphMousePosition);

            if (DragPort == null) return true;

            if (GraphView.GetNodeByGuid(obj.guid) is DialogueEntryNode childNode) {
                // delete single port connnection first
                if (DragPort.capacity == Port.Capacity.Single) {
                    GraphView.DeleteElements(DragPort.connections);
                }

                var edge = IsInputPort
                    ? childNode.OutputPort.ConnectTo<DialogueEdge>(DragPort)
                    : DragPort.ConnectTo<DialogueEdge>(childNode.InputPort);
                if (edge.output.node is DialogueEntryNode output && edge.input.node is DialogueEntryNode input)
                    GraphView.AddChild(output.Entry, input.Entry);

                GraphView.AddElement(edge);
            }
            
            DragPort = null;
            return true;
        }
    }

#endif