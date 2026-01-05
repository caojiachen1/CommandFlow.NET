using System.Runtime.InteropServices;

namespace CommandFlow.NET.Models.Nodes;

/// <summary>
/// 键盘输入节点
/// </summary>
public class KeyboardInputNode : NodeBase
{
    public override string Title => "键盘输入";
    public override string Category => "键盘操作";
    public override string Icon => "Keyboard24";

    public string InputText { get; set; } = string.Empty;
    public int DelayBetweenKeys { get; set; } = 50;

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
            foreach (char c in InputText)
            {
                if (cancellationToken.IsCancellationRequested) break;

                SendChar(c);
                await Task.Delay(DelayBetweenKeys, cancellationToken);
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

    private void SendChar(char c)
    {
        INPUT[] inputs = new INPUT[2];

        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = KEYEVENTF_UNICODE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    #region Win32 API

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    #endregion
}
