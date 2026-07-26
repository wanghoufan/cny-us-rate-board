using System.Threading;
using System.IO;
using System.Windows;

namespace RmbUsdWidget;

public partial class App : System.Windows.Application
{
    private Mutex? _singleInstance;
    private EventWaitHandle? _activationEvent;
    private Thread? _activationThread;
    private volatile bool _isExiting;
    private bool _ownsSingleInstance;

    private const string MutexName = "RmbUsdWidget.SingleInstance";
    private const string ActivationEventName = "RmbUsdWidget.ActivateExisting";

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            WriteCrashLog(args.Exception);
            args.Handled = true;
            System.Windows.MessageBox.Show(
                "汇率看板遇到问题，详细信息已写入本地日志。",
                "人民币兑美元",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            Shutdown(1);
        };

        _singleInstance = new Mutex(true, MutexName, out var isFirst);
        _ownsSingleInstance = isFirst;
        if (!isFirst)
        {
            SignalExistingInstance();
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        base.OnStartup(e);
        _activationEvent = new EventWaitHandle(
            false,
            EventResetMode.AutoReset,
            ActivationEventName);
        var window = new MainWindow();
        window.ShowInTaskbar = e.Args.Contains("--qa", StringComparer.OrdinalIgnoreCase);
        MainWindow = window;
        window.Show();
        StartActivationListener();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isExiting = true;
        _activationEvent?.Set();
        if (_ownsSingleInstance)
        {
            _singleInstance?.ReleaseMutex();
        }

        _activationEvent?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using var activationEvent = EventWaitHandle.OpenExisting(ActivationEventName);
            activationEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // The first instance is still starting. Exiting quietly is safer than
            // creating a second widget or showing an error.
        }
    }

    private void StartActivationListener()
    {
        _activationThread = new Thread(() =>
        {
            while (!_isExiting)
            {
                try
                {
                    _activationEvent?.WaitOne();
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                if (_isExiting)
                {
                    return;
                }

                Dispatcher.BeginInvoke(() =>
                {
                    if (MainWindow is MainWindow window)
                    {
                        window.RestoreFromExternalActivation();
                    }
                });
            }
        })
        {
            IsBackground = true,
            Name = "RmbUsdWidget.ActivationListener"
        };
        _activationThread.Start();
    }

    private static void WriteCrashLog(Exception exception)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RmbUsdWidget");
            Directory.CreateDirectory(folder);
            File.AppendAllText(
                Path.Combine(folder, "crash.log"),
                $"[{DateTime.Now:O}]{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Avoid masking the original startup error.
        }
    }
}
