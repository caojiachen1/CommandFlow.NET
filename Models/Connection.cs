using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CommandFlow.NET.Models;

/// <summary>
/// 节点连接
/// </summary>
public class Connection : INotifyPropertyChanged
{
    private double _startX;
    private double _startY;
    private double _endX;
    private double _endY;

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public NodePort? SourcePort { get; set; }
    public NodePort? TargetPort { get; set; }

    public double StartX
    {
        get => _startX;
        set { _startX = value; OnPropertyChanged(); OnPropertyChanged(nameof(PathData)); }
    }

    public double StartY
    {
        get => _startY;
        set { _startY = value; OnPropertyChanged(); OnPropertyChanged(nameof(PathData)); }
    }

    public double EndX
    {
        get => _endX;
        set { _endX = value; OnPropertyChanged(); OnPropertyChanged(nameof(PathData)); }
    }

    public double EndY
    {
        get => _endY;
        set { _endY = value; OnPropertyChanged(); OnPropertyChanged(nameof(PathData)); }
    }

    /// <summary>
    /// 贝塞尔曲线路径数据
    /// </summary>
    public string PathData
    {
        get
        {
            double controlPointOffset = Math.Abs(EndX - StartX) / 2;
            controlPointOffset = Math.Max(controlPointOffset, 50);

            return $"M {StartX},{StartY} C {StartX + controlPointOffset},{StartY} {EndX - controlPointOffset},{EndY} {EndX},{EndY}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
