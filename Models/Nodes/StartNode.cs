namespace CommandFlow.NET.Models.Nodes;

/// <summary>
/// 开始节点
/// </summary>
public class StartNode : NodeBase
{
    public override string Title => "开始";
    public override string Category => "流程控制";
    public override string Icon => "Play24";

    protected override void InitializePorts()
    {
        OutputPorts.Add(new NodePort { Name = "输出", Type = PortType.Output, ParentNode = this });
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Status = "完成";
        return Task.CompletedTask;
    }
}
