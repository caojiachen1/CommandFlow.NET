using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CommandFlow.NET.Models;

namespace CommandFlow.NET.Controls;

public partial class WorkflowCanvas : UserControl
{
    public ObservableCollection<NodeBase> Nodes { get; } = new();
    public ObservableCollection<Connection> Connections { get; } = new();

    private readonly Dictionary<NodeBase, NodeControl> _nodeControls = new();
    private readonly Dictionary<Connection, Path> _connectionPaths = new();

    private NodePort? _draggingPort;
    private Point _draggingStartPoint;
    private NodePort? _hoveredPort;
    private NodePort? _snappedPort;
    private bool _isPanning;
    private Point _panStartPoint;
    private Point _panStartOffset;

    private const double SnapDistance = 30.0; // 吸附距离阈值

    public double ZoomLevel { get; private set; } = 1.0;

    public event EventHandler<NodeEventArgs>? NodeSelected;

    public WorkflowCanvas()
    {
        InitializeComponent();

        MouseWheel += WorkflowCanvas_MouseWheel;
        MouseLeftButtonDown += WorkflowCanvas_MouseLeftButtonDown;
        MouseLeftButtonUp += WorkflowCanvas_MouseLeftButtonUp;
        MouseMove += WorkflowCanvas_MouseMove;
        MouseRightButtonDown += WorkflowCanvas_MouseRightButtonDown;
        MouseRightButtonUp += WorkflowCanvas_MouseRightButtonUp;

        Drop += WorkflowCanvas_Drop;
        DragOver += WorkflowCanvas_DragOver;

        Nodes.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (NodeBase node in e.NewItems)
                {
                    AddNodeControl(node);
                }
            }
            if (e.OldItems != null)
            {
                foreach (NodeBase node in e.OldItems)
                {
                    RemoveNodeControl(node);
                }
            }
        };

        Connections.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (Connection conn in e.NewItems)
                {
                    AddConnectionPath(conn);
                }
            }
            if (e.OldItems != null)
            {
                foreach (Connection conn in e.OldItems)
                {
                    RemoveConnectionPath(conn);
                }
            }
        };
    }

    private void AddNodeControl(NodeBase node)
    {
        var control = new NodeControl { NodeData = node };
        control.PortMouseDown += NodeControl_PortMouseDown;
        control.PortMouseUp += NodeControl_PortMouseUp;
        control.PortMouseEnter += NodeControl_PortMouseEnter;
        control.PortMouseLeave += NodeControl_PortMouseLeave;
        control.NodeSelected += (s, e) =>
        {
            // 取消其他节点的选中
            foreach (var n in Nodes)
            {
                if (n != e.Node) n.IsSelected = false;
            }
            NodeSelected?.Invoke(this, e);
        };

        Canvas.SetLeft(control, node.X);
        Canvas.SetTop(control, node.Y);

        NodeLayer.Children.Add(control);
        _nodeControls[node] = control;

        // 监听节点位置变化
        node.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(NodeBase.X) || e.PropertyName == nameof(NodeBase.Y))
            {
                Dispatcher.Invoke(() =>
                {
                    Canvas.SetLeft(control, node.X);
                    Canvas.SetTop(control, node.Y);
                    UpdateConnections();
                });
            }
        };
    }

    private void RemoveNodeControl(NodeBase node)
    {
        if (_nodeControls.TryGetValue(node, out var control))
        {
            NodeLayer.Children.Remove(control);
            _nodeControls.Remove(node);

            // 移除相关连接
            var connectionsToRemove = Connections
                .Where(c => c.SourcePort?.ParentNode == node || c.TargetPort?.ParentNode == node)
                .ToList();
            foreach (var conn in connectionsToRemove)
            {
                Connections.Remove(conn);
            }
        }
    }

    private void AddConnectionPath(Connection connection)
    {
        var path = new Path
        {
            Stroke = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
            StrokeThickness = 2,
            Data = Geometry.Parse(connection.PathData)
        };

        connection.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Connection.PathData))
            {
                Dispatcher.Invoke(() => path.Data = Geometry.Parse(connection.PathData));
            }
        };

        ConnectionLayer.Children.Add(path);
        _connectionPaths[connection] = path;

        UpdateConnectionPosition(connection);
    }

    private void RemoveConnectionPath(Connection connection)
    {
        if (_connectionPaths.TryGetValue(connection, out var path))
        {
            ConnectionLayer.Children.Remove(path);
            _connectionPaths.Remove(connection);
        }
    }

    private void UpdateConnections()
    {
        foreach (var connection in Connections)
        {
            UpdateConnectionPosition(connection);
        }
    }

    private void UpdateConnectionPosition(Connection connection)
    {
        if (connection.SourcePort?.ParentNode != null &&
            connection.TargetPort?.ParentNode != null &&
            _nodeControls.TryGetValue(connection.SourcePort.ParentNode, out var sourceControl) &&
            _nodeControls.TryGetValue(connection.TargetPort.ParentNode, out var targetControl))
        {
            var sourcePos = sourceControl.GetPortPosition(connection.SourcePort);
            var targetPos = targetControl.GetPortPosition(connection.TargetPort);

            connection.StartX = connection.SourcePort.ParentNode.X + sourcePos.X;
            connection.StartY = connection.SourcePort.ParentNode.Y + sourcePos.Y;
            connection.EndX = connection.TargetPort.ParentNode.X + targetPos.X;
            connection.EndY = connection.TargetPort.ParentNode.Y + targetPos.Y;
        }
    }

    private void NodeControl_PortMouseDown(object? sender, PortEventArgs e)
    {
        if (sender is NodeControl nodeControl && e.Port.ParentNode != null)
        {
            _draggingPort = e.Port;
            _draggingStartPoint = new Point(
                e.Port.ParentNode.X + e.Position.X,
                e.Port.ParentNode.Y + e.Position.Y);

            TempConnectionLine.Visibility = Visibility.Visible;
            UpdateTempConnectionLine(_draggingStartPoint);
            CaptureMouse();
        }
    }

    private void NodeControl_PortMouseUp(object? sender, PortEventArgs e)
    {
        TryCreateConnection(e.Port);
    }

    private void NodeControl_PortMouseEnter(object? sender, PortEventArgs e)
    {
        _hoveredPort = e.Port;
    }

    private void NodeControl_PortMouseLeave(object? sender, PortEventArgs e)
    {
        if (_hoveredPort == e.Port)
        {
            _hoveredPort = null;
        }
    }

    private void TryCreateConnection(NodePort? targetPort)
    {
        if (_draggingPort != null && targetPort != null && targetPort != _draggingPort)
        {
            // 检查连接有效性
            if (_draggingPort.Type != targetPort.Type &&
                _draggingPort.ParentNode != targetPort.ParentNode)
            {
                var sourcePort = _draggingPort.Type == PortType.Output ? _draggingPort : targetPort;
                var targetInputPort = _draggingPort.Type == PortType.Input ? _draggingPort : targetPort;

                // 检查是否已存在相同连接
                var existingConnection = Connections.FirstOrDefault(c =>
                    c.SourcePort == sourcePort && c.TargetPort == targetInputPort);

                if (existingConnection == null)
                {
                    var connection = new Connection
                    {
                        SourcePort = sourcePort,
                        TargetPort = targetInputPort
                    };
                    Connections.Add(connection);

                    sourcePort.IsConnected = true;
                    targetInputPort.IsConnected = true;
                }
            }
        }

        _draggingPort = null;
        _hoveredPort = null;
        if (_snappedPort != null)
        {
            _snappedPort.IsHighlighted = false;
            _snappedPort = null;
        }
        TempConnectionLine.Visibility = Visibility.Collapsed;
        TempConnectionLine.StrokeDashArray = new DoubleCollection { 4, 2 };
        TempConnectionLine.StrokeThickness = 2;
        ReleaseMouseCapture();
    }

    private void UpdateTempConnectionLine(Point mousePos)
    {
        if (_draggingPort == null) return;

        // 清除之前的高亮
        if (_snappedPort != null)
        {
            _snappedPort.IsHighlighted = false;
        }

        // 查找最近的可吸附端口
        _snappedPort = FindNearestSnapPort(mousePos);

        // 设置新的高亮
        if (_snappedPort != null)
        {
            _snappedPort.IsHighlighted = true;
        }

        double startX = _draggingStartPoint.X;
        double startY = _draggingStartPoint.Y;
        double endX = mousePos.X;
        double endY = mousePos.Y;

        // 如果有吸附端口，使用端口位置
        if (_snappedPort != null && _snappedPort.ParentNode != null)
        {
            var targetControl = _nodeControls[_snappedPort.ParentNode];
            var portPos = targetControl.GetPortPosition(_snappedPort);
            endX = _snappedPort.ParentNode.X + portPos.X;
            endY = _snappedPort.ParentNode.Y + portPos.Y;

            // 视觉反馈：改变临时线的样式
            TempConnectionLine.StrokeDashArray = null;
            TempConnectionLine.StrokeThickness = 3;
        }
        else
        {
            // 恢复默认样式
            TempConnectionLine.StrokeDashArray = new DoubleCollection { 4, 2 };
            TempConnectionLine.StrokeThickness = 2;
        }

        if (_draggingPort.Type == PortType.Input)
        {
            (startX, endX) = (endX, startX);
            (startY, endY) = (endY, startY);
        }

        double controlPointOffset = Math.Abs(endX - startX) / 2;
        controlPointOffset = Math.Max(controlPointOffset, 50);

        var pathData = $"M {startX},{startY} C {startX + controlPointOffset},{startY} {endX - controlPointOffset},{endY} {endX},{endY}";
        TempConnectionLine.Data = Geometry.Parse(pathData);
    }

    private NodePort? FindNearestSnapPort(Point mousePos)
    {
        if (_draggingPort == null) return null;

        NodePort? nearestPort = null;
        double minDistance = SnapDistance;

        foreach (var nodeControl in _nodeControls.Values)
        {
            if (nodeControl.NodeData == null || nodeControl.NodeData == _draggingPort.ParentNode)
                continue;

            // 获取目标端口列表（与拖拽端口类型相反）
            var targetPorts = _draggingPort.Type == PortType.Output
                ? nodeControl.NodeData.InputPorts
                : nodeControl.NodeData.OutputPorts;

            foreach (var port in targetPorts)
            {
                if (port.ParentNode == null) continue;

                var portPos = nodeControl.GetPortPosition(port);
                double portX = port.ParentNode.X + portPos.X;
                double portY = port.ParentNode.Y + portPos.Y;

                double distance = Math.Sqrt(
                    Math.Pow(mousePos.X - portX, 2) +
                    Math.Pow(mousePos.Y - portY, 2));

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPort = port;
                }
            }
        }

        return nearestPort;
    }

    private void WorkflowCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var mousePos = e.GetPosition(TransformCanvas);
        double zoomDelta = e.Delta > 0 ? 1.1 : 0.9;

        ZoomLevel *= zoomDelta;
        ZoomLevel = Math.Clamp(ZoomLevel, 0.25, 4.0);

        ScaleTransform.ScaleX = ZoomLevel;
        ScaleTransform.ScaleY = ZoomLevel;
    }

    private void WorkflowCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Source == this || e.Source == TransformCanvas || e.Source == GridCanvas)
        {
            // 取消所有节点选中
            foreach (var node in Nodes)
            {
                node.IsSelected = false;
            }
        }
    }

    private void WorkflowCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggingPort != null)
        {
            // 优先使用吸附端口，其次是悬停端口
            var targetPort = _snappedPort ?? _hoveredPort;
            TryCreateConnection(targetPort);
        }
    }

    private void WorkflowCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingPort != null)
        {
            var mousePos = e.GetPosition(TransformCanvas);
            UpdateTempConnectionLine(mousePos);
        }
        else if (_isPanning)
        {
            var currentPos = e.GetPosition(this);
            var offset = currentPos - _panStartPoint;

            TranslateTransform.X = _panStartOffset.X + offset.X;
            TranslateTransform.Y = _panStartOffset.Y + offset.Y;
        }
    }

    private void WorkflowCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isPanning = true;
        _panStartPoint = e.GetPosition(this);
        _panStartOffset = new Point(TranslateTransform.X, TranslateTransform.Y);
        CaptureMouse();
    }

    private void WorkflowCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        ReleaseMouseCapture();
    }

    private void WorkflowCanvas_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(Type)) is Type nodeType)
        {
            var pos = e.GetPosition(TransformCanvas);

            if (Activator.CreateInstance(nodeType) is NodeBase node)
            {
                node.X = pos.X - 90; // 居中
                node.Y = pos.Y - 40;
                Nodes.Add(node);
            }
        }
    }

    private void WorkflowCanvas_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(Type)) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    public void ZoomIn()
    {
        ZoomLevel = Math.Min(ZoomLevel * 1.2, 4.0);
        ScaleTransform.ScaleX = ZoomLevel;
        ScaleTransform.ScaleY = ZoomLevel;
    }

    public void ZoomOut()
    {
        ZoomLevel = Math.Max(ZoomLevel / 1.2, 0.25);
        ScaleTransform.ScaleX = ZoomLevel;
        ScaleTransform.ScaleY = ZoomLevel;
    }

    public void ResetView()
    {
        ZoomLevel = 1.0;
        ScaleTransform.ScaleX = 1;
        ScaleTransform.ScaleY = 1;
        TranslateTransform.X = 0;
        TranslateTransform.Y = 0;
    }

    public void ClearAll()
    {
        Connections.Clear();
        Nodes.Clear();
    }

    public void DeleteSelectedNodes()
    {
        var selectedNodes = Nodes.Where(n => n.IsSelected).ToList();
        foreach (var node in selectedNodes)
        {
            Nodes.Remove(node);
        }
    }
}
