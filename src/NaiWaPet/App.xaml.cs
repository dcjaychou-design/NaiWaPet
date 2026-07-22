using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace NaiWaPet;

internal sealed partial class App : System.Windows.Application, IDisposable
{
    private const string MutexName = "Local\\NaiWaPet.SingleInstance.v1";
    private const string ActivationEventName = "Local\\NaiWaPet.Activate.v1";
    private Mutex? instanceMutex;
    private EventWaitHandle? activationEvent;
    private CancellationTokenSource? activationCancellation;
    private Task? activationListenerTask;
    private AppController? controller;
    private bool disposed;

    protected override void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnStartup(e);
        if (e.Args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                using var player = new Services.SpriteAnimationPlayer();
                player.ValidateAssets();
                _ = Services.OpenSourceNotices.LoadAll();
                Shutdown(0);
            }
#pragma warning disable CA1031 // The smoke test must convert every startup failure into a nonzero exit code.
            catch (Exception exception)
            {
                Services.ErrorLog.Write(exception, "Windows smoke test");
                Shutdown(1);
            }
#pragma warning restore CA1031

            return;
        }

        instanceMutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            SignalExistingInstance();
            Shutdown();
            return;
        }

        activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
        activationCancellation = new CancellationTokenSource();
        StartActivationListener(activationCancellation.Token);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        controller = new AppController();
        controller.Start();
    }

    private static void SignalExistingInstance()
    {
        const int attempts = 20;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                using var existingEvent = EventWaitHandle.OpenExisting(ActivationEventName);
                existingEvent.Set();
                return;
            }
            catch (WaitHandleCannotBeOpenedException) when (attempt + 1 < attempts)
            {
                // The first process owns the mutex but may not have created the
                // activation event yet. Give it a short, bounded startup window.
                Thread.Sleep(50);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return;
            }
        }
    }

    private void StartActivationListener(CancellationToken cancellationToken)
    {
        var waitHandle = activationEvent ?? throw new InvalidOperationException("Activation event is unavailable.");
        activationListenerTask = Task.Run(() =>
        {
            var handles = new[] { waitHandle, cancellationToken.WaitHandle };
            while (WaitHandle.WaitAny(handles) == 0)
            {
                Dispatcher.BeginInvoke(() => controller?.ShowPet(), DispatcherPriority.Normal);
            }
        }, cancellationToken);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var logPath = Services.ErrorLog.Write(e.Exception, "WPF dispatcher");
        var logHint = logPath is null ? string.Empty : $"\n\n错误日志：\n{logPath}";
        System.Windows.MessageBox.Show(
            $"奶蛙遇到了错误，将安全退出。\n\n{e.Exception.Message}{logHint}",
            "奶蛙桌宠",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
        controller?.Exit();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Dispose();
        base.OnExit(e);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        activationCancellation?.Cancel();
        try
        {
            activationListenerTask?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            // Cancellation is the normal single-instance listener shutdown.
        }

        activationCancellation?.Dispose();
        activationEvent?.Dispose();
        controller?.Dispose();
        if (instanceMutex is not null)
        {
            try
            {
                instanceMutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // The mutex was already released during an early shutdown.
            }

            instanceMutex.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
