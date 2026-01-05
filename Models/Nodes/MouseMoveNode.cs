using System.Runtime.InteropServices;

namespace CommandFlow.NET.Models.Nodes;

/// <summary>
/// 鼠标移动节点
/// </summary>
public class MouseMoveNode : NodeBase
{
    public override string Title => "鼠标移动";
    public override string Category => "鼠标操作";
    public override string Icon => "CursorHover24";

    public int TargetX { get; set; } = 0;
    public int TargetY { get; set; } = 0;
    public bool SmoothMove { get; set; } = false;
    public int MoveDuration { get; set; } = 500;

    protected override void InitializePorts()
    {
        InputPorts.Add(new NodePort { Name = "输入", Type = PortType.Input, ParentNode = this });
        OutputPorts.Add(new NodePort { Name = "输出", Type = PortType.Output, ParentNode = this });
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Status = "执行中...";
        IsExecuting = true;

        try
        {
            if (SmoothMove)
            {
                await SmoothMoveAsync(TargetX, TargetY, MoveDuration, cancellationToken);
            }
            else
            {
                SetCursorPos(TargetX, TargetY);
            }

            Status = "完成";
        }
        catch (Exception ex)
        {
            Status = $"错误: {ex.Message}";
            throw;
        }
        finally
        {
            IsExecuting = false;
        }
    }

    private async Task SmoothMoveAsync(int targetX, int targetY, int duration, CancellationToken ct)
    {
        GetCursorPos(out POINT currentPos);
        int startX = currentPos.X;
        int startY = currentPos.Y;

        int steps = duration / 10;
        for (int i = 0; i <= steps; i++)
        {
            if (ct.IsCancellationRequested) break;

            double progress = (double)i / steps;
            int x = (int)(startX + (targetX - startX) * progress);
            int y = (int)(startY + (targetY - startY) * progress);

            SetCursorPos(x, y);
            await Task.Delay(10, ct);
        }
    }

    #region Win32 API

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    #endregion
}
