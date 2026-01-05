using System.Collections.ObjectModel;
using CommandFlow.NET.Models;
using CommandFlow.NET.Models.Nodes;

namespace CommandFlow.NET.Services;

/// <summary>
/// 工作流执行器
/// </summary>
public class WorkflowExecutor
{
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public event EventHandler<LogEventArgs>? LogMessage;
    public event EventHandler? ExecutionStarted;
    public event EventHandler? ExecutionStopped;

    public async Task ExecuteAsync(ObservableCollection<NodeBase> nodes, ObservableCollection<Connection> connections)
    {
        if (_isRunning) return;

        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        ExecutionStarted?.Invoke(this, EventArgs.Empty);

        try
        {
            Log("开始执行工作流...", LogLevel.Info);

            // 找到开始节点
            var startNodes = nodes.OfType<StartNode>().ToList();
            if (startNodes.Count == 0)
            {
                // 如果没有开始节点，找没有输入连接的节点
                startNodes = nodes
                    .Where(n => !connections.Any(c => c.TargetPort?.ParentNode == n))
                    .OfType<StartNode>()
                    .ToList();

                if (startNodes.Count == 0 && nodes.Count > 0)
                {
                    Log("未找到开始节点，从第一个节点开始执行", LogLevel.Warning);
                    await ExecuteNodeChain(nodes[0], connections, _cancellationTokenSource.Token);
                }
            }

            foreach (var startNode in startNodes)
            {
                await ExecuteNodeChain(startNode, connections, _cancellationTokenSource.Token);
            }

            Log("工作流执行完成", LogLevel.Success);
        }
        catch (OperationCanceledException)
        {
            Log("工作流已停止", LogLevel.Warning);
        }
        catch (Exception ex)
        {
            Log($"执行错误: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            _isRunning = false;
            ExecutionStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task ExecuteNodeChain(NodeBase node, ObservableCollection<Connection> connections, CancellationToken ct)
    {
        var visited = new HashSet<string>();
        await ExecuteNodeRecursive(node, connections, visited, ct);
    }

    private async Task ExecuteNodeRecursive(NodeBase node, ObservableCollection<Connection> connections,
        HashSet<string> visited, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;
        if (visited.Contains(node.Id)) return;

        visited.Add(node.Id);
        Log($"执行节点: {node.Title}", LogLevel.Info);

        try
        {
            await node.ExecuteAsync(ct);

            // 处理循环节点
            if (node is LoopNode loopNode)
            {
                while (loopNode.ShouldContinue() && !ct.IsCancellationRequested)
                {
                    await loopNode.ExecuteAsync(ct);

                    // 执行循环体连接的节点
                    var loopBodyPort = loopNode.OutputPorts.FirstOrDefault(p => p.Name == "循环体");
                    if (loopBodyPort != null)
                    {
                        var loopConnection = connections.FirstOrDefault(c => c.SourcePort == loopBodyPort);
                        if (loopConnection?.TargetPort?.ParentNode != null)
                        {
                            var loopVisited = new HashSet<string> { node.Id };
                            await ExecuteNodeRecursive(loopConnection.TargetPort.ParentNode, connections, loopVisited, ct);
                        }
                    }
                }
                loopNode.Reset();

                // 执行完成端口
                var completePort = loopNode.OutputPorts.FirstOrDefault(p => p.Name == "完成");
                if (completePort != null)
                {
                    var completeConnection = connections.FirstOrDefault(c => c.SourcePort == completePort);
                    if (completeConnection?.TargetPort?.ParentNode != null)
                    {
                        await ExecuteNodeRecursive(completeConnection.TargetPort.ParentNode, connections, visited, ct);
                    }
                }
            }
            else
            {
                // 执行下一个节点
                foreach (var outputPort in node.OutputPorts)
                {
                    var connection = connections.FirstOrDefault(c => c.SourcePort == outputPort);
                    if (connection?.TargetPort?.ParentNode != null)
                    {
                        await ExecuteNodeRecursive(connection.TargetPort.ParentNode, connections, visited, ct);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"节点 {node.Title} 执行失败: {ex.Message}", LogLevel.Error);
            throw;
        }
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
    }

    private void Log(string message, LogLevel level)
    {
        LogMessage?.Invoke(this, new LogEventArgs(message, level));
    }
}

public class LogEventArgs : EventArgs
{
    public string Message { get; }
    public LogLevel Level { get; }
    public DateTime Timestamp { get; }

    public LogEventArgs(string message, LogLevel level)
    {
        Message = message;
        Level = level;
        Timestamp = DateTime.Now;
    }
}

public enum LogLevel
{
    Info,
    Success,
    Warning,
    Error
}
