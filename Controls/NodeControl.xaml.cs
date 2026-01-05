using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommandFlow.NET.Models;
using Wpf.Ui.Controls;

namespace CommandFlow.NET.Controls;

public partial class NodeControl : UserControl
{
    public static readonly DependencyProperty NodeDataProperty =
        DependencyProperty.Register(nameof(NodeData), typeof(NodeBase), typeof(NodeControl),
            new PropertyMetadata(null, OnNodeDataChanged));

    public NodeBase? NodeData
    {
        get => (NodeBase?)GetValue(NodeDataProperty);
        set => SetValue(NodeDataProperty, value);
    }

    public event EventHandler<PortEventArgs>? PortMouseDown;
    public event EventHandler<PortEventArgs>? PortMouseUp;
    public event EventHandler<PortEventArgs>? PortMouseEnter;
    public event EventHandler<PortEventArgs>? PortMouseLeave;
    public event EventHandler<NodeEventArgs>? NodeSelected;

    private bool _isDragging;
    private Point _dragStartPoint;
    private Point _nodeStartPosition;

    public NodeControl()
    {
        InitializeComponent();
        MouseLeftButtonDown += NodeControl_MouseLeftButtonDown;
        MouseLeftButtonUp += NodeControl_MouseLeftButtonUp;
        MouseMove += NodeControl_MouseMove;
    }

    private static void OnNodeDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NodeControl control && e.NewValue is NodeBase node)
        {
            control.UpdateNodeDisplay(node);
        }
    }

    private void UpdateNodeDisplay(NodeBase node)
    {
        TitleText.Text = node.Title;
        InputPortsControl.ItemsSource = node.InputPorts;
        OutputPortsControl.ItemsSource = node.OutputPorts;

        // 更新图标
        if (Enum.TryParse<SymbolRegular>(node.Icon, out var symbol))
        {
            NodeIcon.Symbol = symbol;
        }

        // 绑定状态更新
        node.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(NodeBase.Status))
            {
                Dispatcher.Invoke(() => StatusText.Text = node.Status);
            }
            else if (e.PropertyName == nameof(NodeBase.IsExecuting))
            {
                Dispatcher.Invoke(() =>
                {
                    ExecutingIndicator.Visibility = node.IsExecuting ? Visibility.Visible : Visibility.Collapsed;
                });
            }
            else if (e.PropertyName == nameof(NodeBase.IsSelected))
            {
                Dispatcher.Invoke(() => UpdateSelection(node.IsSelected));
            }
        };

        // 设置颜色根据分类
        SetCategoryColor(node.Category);
    }

    private void SetCategoryColor(string category)
    {
        var color = category switch
        {
            "鼠标操作" => Color.FromRgb(66, 165, 245),   // 蓝色
            "键盘操作" => Color.FromRgb(156, 39, 176),   // 紫色
            "流程控制" => Color.FromRgb(76, 175, 80),    // 绿色
            _ => Color.FromRgb(96, 125, 139)             // 灰色
        };
        TitleBar.Background = new SolidColorBrush(color);
    }

    private void UpdateSelection(bool isSelected)
    {
        NodeBorder.BorderBrush = isSelected
            ? new SolidColorBrush(Color.FromRgb(0, 120, 215))
            : (Brush)FindResource("ControlElevationBorderBrush");
        NodeBorder.BorderThickness = isSelected ? new Thickness(2) : new Thickness(1);
    }

    private void NodeControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is System.Windows.Shapes.Ellipse) return;

        _isDragging = true;
        _dragStartPoint = e.GetPosition(Parent as IInputElement);
        _nodeStartPosition = new Point(NodeData?.X ?? 0, NodeData?.Y ?? 0);
        CaptureMouse();

        if (NodeData != null)
        {
            NodeData.IsSelected = true;
            NodeSelected?.Invoke(this, new NodeEventArgs(NodeData));
        }

        e.Handled = true;
    }

    private void NodeControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ReleaseMouseCapture();
        }
    }

    private void NodeControl_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && NodeData != null)
        {
            var currentPos = e.GetPosition(Parent as IInputElement);
            var offset = currentPos - _dragStartPoint;

            NodeData.X = _nodeStartPosition.X + offset.X;
            NodeData.Y = _nodeStartPosition.Y + offset.Y;

            Canvas.SetLeft(this, NodeData.X);
            Canvas.SetTop(this, NodeData.Y);
        }
    }

    private void Port_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NodePort port)
        {
            var position = element.TransformToAncestor(this).Transform(new Point(6, 6));
            PortMouseDown?.Invoke(this, new PortEventArgs(port, position));
            e.Handled = true;
        }
    }

    private void Port_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NodePort port)
        {
            var position = element.TransformToAncestor(this).Transform(new Point(6, 6));
            PortMouseUp?.Invoke(this, new PortEventArgs(port, position));
            e.Handled = true;
        }
    }

    private void Port_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NodePort port)
        {
            var position = element.TransformToAncestor(this).Transform(new Point(6, 6));
            PortMouseEnter?.Invoke(this, new PortEventArgs(port, position));
        }
    }

    private void Port_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is NodePort port)
        {
            var position = element.TransformToAncestor(this).Transform(new Point(6, 6));
            PortMouseLeave?.Invoke(this, new PortEventArgs(port, position));
        }
    }

    public Point GetPortPosition(NodePort port)
    {
        ItemsControl? itemsControl = port.Type == PortType.Input ? InputPortsControl : OutputPortsControl;

        var container = itemsControl.ItemContainerGenerator.ContainerFromItem(port) as FrameworkElement;
        if (container != null)
        {
            var ellipse = FindVisualChild<System.Windows.Shapes.Ellipse>(container);
            if (ellipse != null)
            {
                return ellipse.TransformToAncestor(this).Transform(new Point(6, 6));
            }
        }

        return new Point(port.Type == PortType.Input ? 0 : ActualWidth, ActualHeight / 2);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result) return result;
            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null) return childOfChild;
        }
        return null;
    }
}

public class PortEventArgs : EventArgs
{
    public NodePort Port { get; }
    public Point Position { get; }

    public PortEventArgs(NodePort port, Point position)
    {
        Port = port;
        Position = position;
    }
}

public class NodeEventArgs : EventArgs
{
    public NodeBase Node { get; }

    public NodeEventArgs(NodeBase node)
    {
        Node = node;
    }
}
