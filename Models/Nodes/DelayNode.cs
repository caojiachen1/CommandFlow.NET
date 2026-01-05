namespace CommandFlow.NET.Models.Nodes;

/// <summary>
/// 延时节点
/// </summary>
public class DelayNode : NodeBase
{
    public override string Title => "延时";
    public override string Category => "流程控制";
    public override string Icon => "Timer24";

    public int DelayMilliseconds { get; set; } = 1000;

    protected override void InitializePorts()
    {
        InputPorts.Add(new NodePort { Name = "输入", Type = PortType.Input, ParentNode = this });
        OutputPorts.Add(new NodePort { Name = "输出", Type = PortType.Output, ParentNode = this });
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Status = $"等待 {DelayMilliseconds}ms...";
        IsExecuting = true;

        try
        {
            await Task.Delay(DelayMilliseconds, cancellationToken);
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
}
