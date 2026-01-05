namespace CommandFlow.NET.Models.Nodes;

/// <summary>
/// 循环节点
/// </summary>
public class LoopNode : NodeBase
{
    public override string Title => "循环";
    public override string Category => "流程控制";
    public override string Icon => "ArrowRepeatAll24";

    public int LoopCount { get; set; } = 3;
    public int CurrentIteration { get; private set; } = 0;

    protected override void InitializePorts()
    {
        InputPorts.Add(new NodePort { Name = "输入", Type = PortType.Input, ParentNode = this });
        OutputPorts.Add(new NodePort { Name = "循环体", Type = PortType.Output, ParentNode = this });
        OutputPorts.Add(new NodePort { Name = "完成", Type = PortType.Output, ParentNode = this });
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        CurrentIteration++;
        Status = $"迭代 {CurrentIteration}/{LoopCount}";
        return Task.CompletedTask;
    }

    public bool ShouldContinue()
    {
        return CurrentIteration < LoopCount;
    }

    public void Reset()
    {
        CurrentIteration = 0;
        Status = "就绪";
    }
}
