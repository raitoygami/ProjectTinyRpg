#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


public sealed class AbilityEffectNode : Node {
    public readonly AbilityEffect Entry;

    public AbilityPort InputPort { get; private set; }

    public AbilityPort OutputPort { get; private set; }

    public Action<AbilityEffectNode> onNodeSelected;
    public Action<AbilityEffectNode> onNodeUnSelected;

    public AbilityEffectNode(AbilityEffect entry) : base(
        $"{AbilityEditor.AbilityEditorDirRoot}AbilityEffectNode.uxml") {
        Entry = entry;

        viewDataKey = entry.guid;

        SetPosition(new Rect(entry.localtion , layout.size));
        // port
        CreateInputPort();
        CreateOutputPort();
        
        // style sheet class
        SetupUIClass();
        capabilities |= Capabilities.Snappable | Capabilities.Collapsible;

        var description = this.Q<Label>("description");
        description.text = Entry.GetDescription();
        title = string.IsNullOrEmpty(Entry.Content) ? Entry.GetType().Name.Replace("E_","") : Entry.Content;

        var sb = new SerializedObject(Entry);

        description.TrackSerializedObjectValue(sb, callback => {
            // title 
            title = string.IsNullOrEmpty(Entry.Content) ? Entry.GetType().Name.Replace("E_","") : Entry.Content;

            //description
            description.text = Entry.GetDescription();
        });

        // update property
        // entry.PropertyChangedCallback = OnPropertyChanged;
    }

    // private void OnPropertyChanged()
    // {
    //     var lablePriority = this.Q<Label>("label-priority");
    //     lablePriority.text = Entry.Priority.ToString();
    //     
    //     var nodePriority = this.Q<VisualElement>("entry-priority");
    //     nodePriority.visible = Entry.Priority >= -1;
    // }

    private void SetupUIClass() {
        var classes = Entry.GetStyleClasses();
        if (classes == null) return;
        var outputNode = this.Q<VisualElement>("output");
        var inputNode = this.Q<VisualElement>("input");

        foreach (var c in classes) {
            AddToClassList(c);
            outputNode.AddToClassList(c);
            inputNode.AddToClassList(c);
        }
        
    }

    public sealed override string title {
        get => base.title;
        set => base.title = value;
    }

    public sealed override void SetPosition(Rect newPos) {
        base.SetPosition(newPos);

        Undo.RecordObject(Entry, "BT Entry Set Position");
        Entry.localtion.x = newPos.xMin;
        Entry.localtion.y = newPos.yMin;
        EditorUtility.SetDirty(Entry);
    }

    public override Port InstantiatePort(Orientation orientation, Direction direction, Port.Capacity capacity, Type type) {
        return Port.Create<AbilityEdge>(orientation, direction, capacity, type);
    }

    private void CreateInputPort() {
        if (Entry is not AbilityRoot)
        {
            InputPort = new AbilityPort(Direction.Input, Port.Capacity.Single, Entry.GetStyleClasses());
        }

        if (InputPort == null) return;

        InputPort.portName = "";
        InputPort.style.flexDirection = FlexDirection.Column;
        inputContainer.Add(InputPort);
    }

    private void CreateOutputPort() {
        /*OutputPort = Entry switch {
            // DialogueComposite => new DialoguePort(Direction.Output, Port.Capacity.Multi, Entry.GetStyleClasses()),
            AbilityRoot => new AbilityPort(Direction.Output, Port.Capacity.Multi, Entry.GetStyleClasses()),
            /*DialogueOption => new AbilityPort(Direction.Output, Port.Capacity.Multi, Entry.GetStyleClasses()),
            DialogueLines => new AbilityPort(Direction.Output, Port.Capacity.Multi, Entry.GetStyleClasses()),#1#
            _ => OutputPort
        };*/
        OutputPort = new AbilityPort(Direction.Output, Port.Capacity.Multi, Entry.GetStyleClasses());
        if (OutputPort == null) return;

        OutputPort.portName = "";
        // mOutputPort.
        OutputPort.style.flexDirection = FlexDirection.ColumnReverse;
        outputContainer.Add(OutputPort);
    }

    public override void OnSelected() {
        base.OnSelected();
        onNodeSelected?.Invoke(this);
    }

    public override void OnUnselected() {
        base.OnUnselected();
        onNodeUnSelected?.Invoke(this);
    }
}
#endif