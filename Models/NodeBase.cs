using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CommandFlow.NET.Models;

/// <summary>
/// 节点端口类型
/// </summary>
public enum PortType
{
    Input,
    Output
}

/// <summary>
/// 节点端口
/// </summary>
public class NodePort : INotifyPropertyChanged
{
    private bool _isConnected;
    private bool _isHighlighted;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public PortType Type { get; set; }
    public NodeBase? ParentNode { get; set; }

    public bool IsConnected
    {
        get => _isConnected;
        set { _isConnected = value; OnPropertyChanged(); }
    }

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set { _isHighlighted = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// 节点基类
/// </summary>
public abstract class NodeBase : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private bool _isSelected;
    private bool _isExecuting;
    private string _status = "就绪";

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public abstract string Title { get; }
    public abstract string Category { get; }
    public abstract string Icon { get; }

    public double X
    {
        get => _x;
        set { _x = value; OnPropertyChanged(); }
    }

    public double Y
    {
        get => _y;
        set { _y = value; OnPropertyChanged(); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public bool IsExecuting
    {
        get => _isExecuting;
        set { _isExecuting = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public List<NodePort> InputPorts { get; } = new();
    public List<NodePort> OutputPorts { get; } = new();

    protected NodeBase()
    {
        InitializePorts();
    }

    protected abstract void InitializePorts();

    public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
