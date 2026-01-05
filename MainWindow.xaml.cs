using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CommandFlow.NET.Services;
using Wpf.Ui.Controls;

namespace CommandFlow.NET;

public partial class MainWindow : FluentWindow
{
    private readonly WorkflowExecutor _executor = new();
    private readonly ObservableCollection<LogEntry> _logs = new();

    public MainWindow()
    {
        InitializeComponent();

        // 初始化节点分类列表
        NodeCategoriesControl.ItemsSource = NodeFactory.GetNodeCategories();

        // 初始化日志
        LogControl.ItemsSource = _logs;

        // 订阅执行器事件
        _executor.LogMessage += (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                var color = e.Level switch
                {
                    LogLevel.Success => Brushes.LimeGreen,
                    LogLevel.Warning => Brushes.Orange,
                    LogLevel.Error => Brushes.Red,
                    _ => (Brush)FindResource("TextFillColorPrimaryBrush")
                };
                _logs.Add(new LogEntry { Timestamp = e.Timestamp, Message = e.Message, Color = color });
                LogScrollViewer.ScrollToEnd();
            });
        };

        _executor.ExecutionStarted += (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                RunButton.IsEnabled = false;
                StopButton.IsEnabled = true;
            });
        };

        _executor.ExecutionStopped += (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                RunButton.IsEnabled = true;
                StopButton.IsEnabled = false;
            });
        };

        // 键盘快捷键
        KeyDown += MainWindow_KeyDown;
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            WorkflowCanvas.DeleteSelectedNodes();
        }
        else if (e.Key == Key.F5)
        {
            RunButton_Click(sender, e);
        }
        else if (e.Key == Key.Escape && _executor.IsRunning)
        {
            StopButton_Click(sender, e);
        }
    }

    private void NodeItem_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element)
        {
            if (element.DataContext is NodeTypeInfo nodeTypeInfo)
            {
                var data = new DataObject(typeof(Type), nodeTypeInfo.Type);
                DragDrop.DoDragDrop(element, data, DragDropEffects.Copy);
            }
        }
    }

    private async void RunButton_Click(object sender, RoutedEventArgs e)
    {
        await _executor.ExecuteAsync(WorkflowCanvas.Nodes, WorkflowCanvas.Connections);
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _executor.Stop();
    }

    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        WorkflowCanvas.ZoomIn();
    }

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        WorkflowCanvas.ZoomOut();
    }

    private void ResetViewButton_Click(object sender, RoutedEventArgs e)
    {
        WorkflowCanvas.ResetView();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        WorkflowCanvas.DeleteSelectedNodes();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "确定要清空所有节点和连接吗？",
            "确认",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            WorkflowCanvas.ClearAll();
        }
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        _logs.Clear();
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
    public Brush Color { get; set; } = Brushes.White;
}
