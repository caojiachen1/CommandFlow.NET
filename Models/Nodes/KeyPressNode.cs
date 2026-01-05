using System.Runtime.InteropServices;

namespace CommandFlow.NET.Models.Nodes;

/// <summary>
/// 按键节点（支持组合键）
/// </summary>
public class KeyPressNode : NodeBase
{
    public override string Title => "按键";
    public override string Category => "键盘操作";
    public override string Icon => "KeyMultiple20";

    public VirtualKey Key { get; set; } = VirtualKey.Enter;
    public bool WithCtrl { get; set; } = false;
    public bool WithAlt { get; set; } = false;
    public bool WithShift { get; set; } = false;

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
            var modifiers = new List<byte>();
            if (WithCtrl) modifiers.Add(VK_CONTROL);
            if (WithAlt) modifiers.Add(VK_MENU);
            if (WithShift) modifiers.Add(VK_SHIFT);

            // 按下修饰键
            foreach (var mod in modifiers)
            {
                keybd_event(mod, 0, 0, 0);
            }

            // 按下并释放主键
            keybd_event((byte)Key, 0, 0, 0);
            keybd_event((byte)Key, 0, KEYEVENTF_KEYUP, 0);

            // 释放修饰键
            foreach (var mod in modifiers)
            {
                keybd_event(mod, 0, KEYEVENTF_KEYUP, 0);
            }

            await Task.Delay(50, cancellationToken);
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

    private const byte VK_CONTROL = 0x11;
    private const byte VK_MENU = 0x12;
    private const byte VK_SHIFT = 0x10;
    private const int KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    #endregion
}

public enum VirtualKey : byte
{
    Enter = 0x0D,
    Tab = 0x09,
    Escape = 0x1B,
    Space = 0x20,
    Backspace = 0x08,
    Delete = 0x2E,
    Home = 0x24,
    End = 0x23,
    PageUp = 0x21,
    PageDown = 0x22,
    Up = 0x26,
    Down = 0x28,
    Left = 0x25,
    Right = 0x27,
    F1 = 0x70,
    F2 = 0x71,
    F3 = 0x72,
    F4 = 0x73,
    F5 = 0x74,
    F6 = 0x75,
    F7 = 0x76,
    F8 = 0x77,
    F9 = 0x78,
    F10 = 0x79,
    F11 = 0x7A,
    F12 = 0x7B,
    A = 0x41,
    C = 0x43,
    V = 0x56,
    X = 0x58,
    Z = 0x5A,
    S = 0x53
}
