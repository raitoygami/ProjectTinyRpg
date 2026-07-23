#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class AbilityPort : Port {
    // GITHUB:UnityCsReference-master\UnityCsReference-master\Modules\GraphViewEditor\Elements\Port.cs
    public class DefaultEdgeConnectorListener : IEdgeConnectorListener {
        private readonly GraphViewChange m_GraphViewChange;
        private readonly List<Edge> m_EdgesToCreate;
        private readonly List<GraphElement> m_EdgesToDelete;

        public DefaultEdgeConnectorListener() {
            m_EdgesToCreate = new List<Edge>();
            m_EdgesToDelete = new List<GraphElement>();

            m_GraphViewChange.edgesToCreate = m_EdgesToCreate;
        }
        
        public void OnDropOutsidePort(Edge edge, Vector2 position) {
            var draggedPort = edge.output?.edgeConnector.edgeDragHelper.draggedPort ??
                              edge.input?.edgeConnector.edgeDragHelper.draggedPort;

            AbilityGraphView.searchWindow.DragPort = draggedPort as AbilityPort;
            AbilityGraphView.searchWindow.IsInputPort = draggedPort == edge.input;
            var mousePosition = AbilityGraphView.searchWindow.GraphView.Editor.position.position + position;
            SearchWindow.Open(new SearchWindowContext(mousePosition), AbilityGraphView.searchWindow);
        }

        public void OnDrop(GraphView graphView, Edge edge) {
            m_EdgesToCreate.Clear();
            m_EdgesToCreate.Add(edge);

            // We can't just add these edges to delete to the m_GraphViewChange
            // because we want the proper deletion code in GraphView to also
            // be called. Of course, that code (in DeleteElements) also
            // sends a GraphViewChange.
            m_EdgesToDelete.Clear();
            if (edge.input.capacity == Capacity.Single)
                foreach (var edgeToDelete in edge.input.connections)
                    if (edgeToDelete != edge)
                        m_EdgesToDelete.Add(edgeToDelete);
            if (edge.output.capacity == Capacity.Single)
                foreach (var edgeToDelete in edge.output.connections)
                    if (edgeToDelete != edge)
                        m_EdgesToDelete.Add(edgeToDelete);
            if (m_EdgesToDelete.Count > 0)
                graphView.DeleteElements(m_EdgesToDelete);

            var edgesToCreate = m_EdgesToCreate;
            if (graphView.graphViewChanged != null) {
                edgesToCreate = graphView.graphViewChanged(m_GraphViewChange).edgesToCreate;
            }

            foreach (var e in edgesToCreate) {
                graphView.AddElement(e);
                edge.input.Connect(e);
                edge.output.Connect(e);
            }
        }
    }

    public AbilityPort(Direction direction, Capacity capacity, List<string> styleClasses = null) : base(
        Orientation.Vertical, direction, capacity, typeof(bool)) {
        var connectorListener = new DefaultEdgeConnectorListener();
        m_EdgeConnector = new EdgeConnector<Edge>(connectorListener);
        this.AddManipulator(m_EdgeConnector);

        style.flexGrow = 1;
        style.height = 15;
        m_ConnectorText.style.height = 0;
        m_ConnectorBox.visible = false;
    }

    public override bool ContainsPoint(Vector2 localPoint) {
        var rect = new Rect(0, 0, layout.width, layout.height);
        return rect.Contains(localPoint);
    }
}

#endif