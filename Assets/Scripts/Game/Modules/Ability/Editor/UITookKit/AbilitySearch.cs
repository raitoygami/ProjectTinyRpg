#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;


public class AbilitySearch : ScriptableObject, ISearchWindowProvider
{
    public AbilityGraphView GraphView { get; private set; }

    public AbilityPort DragPort { get; set; }
    public bool IsInputPort { get; set; }

    public void Initialized(AbilityGraphView graphView)
    {
        GraphView = graphView;
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var indentationIcon = new Texture2D(1, 1);
        indentationIcon.SetPixel(0, 0, Color.clear);
        indentationIcon.Apply();
        var ret = new List<SearchTreeEntry> {new SearchTreeGroupEntry(new GUIContent("Search Effect"))};

        var types = TypeCache.GetTypesDerivedFrom<AbilityEffect>();
        if (DragPort != null && IsInputPort) return ret;

        var group = new Dictionary<string, List<Type>>();
        var ungroup = new List<Type>();

        foreach (var t in types)
        {
            var classify = AbilityEffect.GetClassify(t);
            var result = classify.Split('/');
            if (result.Length <= 1)
            {
                ungroup.Add(t);
                continue;
            }

            if (group.TryGetValue(result[0], out var ts))
                ts.Add(t);
            else
                group.Add(result[0], new List<Type> {t});
        }

        foreach (var (key, value) in group)
        {
            ret.Add(new SearchTreeGroupEntry(new GUIContent(key)) {level = 1});
            ret.AddRange(value.Select(t =>
                new SearchTreeEntry(new GUIContent(t.Name.Replace("E_", ""), indentationIcon))
                    {userData = t, level = 2}));
        }

        ret.AddRange(ungroup
            .Where(t => t != typeof(AbilityRoot))
            .Select(t => new SearchTreeEntry(new GUIContent(t.Name.Replace("E_", ""), indentationIcon))
                {userData = t, level = 1}));

        // composite
        // types = TypeCache.GetTypesDerivedFrom<AbilityComposite>();
        // ret.Add(new SearchTreeGroupEntry(new GUIContent("Composite"), 1));
        // ret.AddRange(types.Select(t => new SearchTreeEntry(new GUIContent(t.Name, indentationIcon))
        //     {userData = t, level = 2}));
        //
        //
        // types = TypeCache.GetTypesDerivedFrom<AbilityDecorator>();
        // ret.Add(new SearchTreeGroupEntry(new GUIContent("Decorator"), 1));
        // ret.AddRange(types.Select(t => new SearchTreeEntry(new GUIContent(t.Name, indentationIcon))
        //     {userData = t, level = 2}));


        return ret;
    }

    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
        var editor = GraphView.Editor;
        var windowMousePosition =
            editor.rootVisualElement.ChangeCoordinatesTo(editor.rootVisualElement.parent,
                context.screenMousePosition - editor.position.position);

        var graphMousePosition = GraphView.contentContainer.WorldToLocal(windowMousePosition);

        var t = (Type) SearchTreeEntry.userData;
        var obj = GraphView.NewAbilityEntry(t, graphMousePosition);

        if (DragPort == null) return true;

        if (GraphView.GetNodeByGuid(obj.guid) is AbilityEffectNode childNode)
        {
            // delete single port connnection first
            if (DragPort.capacity == Port.Capacity.Single)
            {
                GraphView.DeleteElements(DragPort.connections);
            }

            var edge = IsInputPort
                ? childNode.OutputPort.ConnectTo<AbilityEdge>(DragPort)
                : DragPort.ConnectTo<AbilityEdge>(childNode.InputPort);
            if (edge.output.node is AbilityEffectNode output && edge.input.node is AbilityEffectNode input)
                GraphView.AddChild(output.Entry, input.Entry);

            GraphView.AddElement(edge);
        }

        DragPort = null;
        return true;
    }
}

#endif