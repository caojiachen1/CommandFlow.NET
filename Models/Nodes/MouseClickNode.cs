using System.Runtime.InteropServices;

namespace CommandFlow.NET.Models.Nodes;

/// <summary>
/// 鼠标点击节点
/// </summary>
public class MouseClickNode : NodeBase
{
    public override string Title => "鼠标点击";
    public override string Category => "鼠标操作";
    public override string Icon => "CursorDefault24";

    public int ClickX { get; set; } = 0;
    public int ClickY { get; set; } = 0;
    public MouseButton Button { get; set; } = MouseButton.Left;
    public int ClickCount { get; set; } = 1;
    public int DelayBetweenClicks { get; set; } = 100;

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
            for (int i = 0; i < ClickCount; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                SetCursorPos(ClickX, ClickY);

                if (Button == MouseButton.Left)
                {
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                }
                else if (Button == MouseButton.Right)
                {
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                }

                if (i < ClickCount - 1)
                    await Task.Delay(DelayBetweenClicks, cancellationToken);
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

    #region Win32 API

    private const int MOUSEEVENTF_LEFTDOWN = 0x02;
    private const int MOUSEEVENTF_LEFTUP = 0x04;
    private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
    private const int MOUSEEVENTF_RIGHTUP = 0x10;

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    #endregion
}

public enum MouseButton
{
    Left,
    Right,
    Middle
}
