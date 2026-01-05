using CommandFlow.NET.Models;
using CommandFlow.NET.Models.Nodes;

namespace CommandFlow.NET.Services;

/// <summary>
/// 节点工厂
/// </summary>
public static class NodeFactory
{
    public static List<NodeCategory> GetNodeCategories()
    {
        return new List<NodeCategory>
        {
            new NodeCategory
            {
                Name = "流程控制",
                Icon = "Flow20",
                NodeTypes = new List<NodeTypeInfo>
                {
                    new NodeTypeInfo { Name = "开始", Icon = "Play24", Type = typeof(StartNode) },
                    new NodeTypeInfo { Name = "延时", Icon = "Timer24", Type = typeof(DelayNode) },
                    new NodeTypeInfo { Name = "循环", Icon = "ArrowRepeatAll24", Type = typeof(LoopNode) }
                }
            },
            new NodeCategory
            {
                Name = "鼠标操作",
                Icon = "CursorDefault24",
                NodeTypes = new List<NodeTypeInfo>
                {
                    new NodeTypeInfo { Name = "鼠标点击", Icon = "CursorDefault24", Type = typeof(MouseClickNode) },
                    new NodeTypeInfo { Name = "鼠标移动", Icon = "CursorHover24", Type = typeof(MouseMoveNode) }
                }
            },
            new NodeCategory
            {
                Name = "键盘操作",
                Icon = "Keyboard24",
                NodeTypes = new List<NodeTypeInfo>
                {
                    new NodeTypeInfo { Name = "键盘输入", Icon = "Keyboard24", Type = typeof(KeyboardInputNode) },
                    new NodeTypeInfo { Name = "按键", Icon = "KeyMultiple20", Type = typeof(KeyPressNode) }
                }
            }
        };
    }
}

public class NodeCategory
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public List<NodeTypeInfo> NodeTypes { get; set; } = new();
}

public class NodeTypeInfo
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public Type Type { get; set; } = typeof(NodeBase);
}
