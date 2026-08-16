using System.Runtime.InteropServices;

namespace SynergyStrapper.Integrations;

public sealed class RuntimeMaintenance : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _worker;
    private readonly int _trackedProcessId;

    public RuntimeMaintenance(int trackedProcessId)
    {
        _trackedProcessId = trackedProcessId;
        _worker = Task.Run(RunAsync);
    }

    private async Task RunAsync()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            try
            {
                if (!Utilities.GetProcessesSafe().Any(x => x.Id == _trackedProcessId))
                    return;

                if (App.Settings.Prop.Features.DisableCrashHandler)
                    CloseCrashHandlers();

                var settings = App.Settings.Prop.Features;
                if (settings.EnableMemoryTrimmer)
                {
                    int threshold = Math.Clamp(settings.MemoryTrimThresholdMb, 256, 32768);
                    foreach (Process process in Utilities.GetProcessesSafe().Where(x => x.ProcessName.Equals(App.RobloxPlayerAppName, StringComparison.OrdinalIgnoreCase)))
                    {
                        try
                        {
                            if (process.WorkingSet64 >= threshold * 1024L * 1024L)
                            {
                                EmptyWorkingSet(process.Handle);
                                App.Logger.WriteLine("RuntimeMaintenance", $"Trimmed working set for PID {process.Id} at {process.WorkingSet64 / 1024 / 1024} MB.");
                            }
                        }
                        catch (Exception ex)
                        {
                            App.Logger.WriteException("RuntimeMaintenance::Trim", ex);
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                }

                int delay = Math.Clamp(settings.MemoryTrimIntervalSeconds, 10, 3600);
                await Task.Delay(TimeSpan.FromSeconds(delay), _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("RuntimeMaintenance", ex);
                await Task.Delay(TimeSpan.FromSeconds(10), _cancellation.Token);
            }
        }
    }

    private static void CloseCrashHandlers()
    {
        foreach (Process process in Utilities.GetProcessesSafe().Where(x => x.ProcessName.Equals("RobloxCrashHandler", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                if (!process.HasExited)
                {
                    process.CloseMainWindow();
                    if (!process.WaitForExit(500))
                        process.Kill();
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("RuntimeMaintenance::CloseCrashHandlers", ex);
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    public void Dispose()
    {
        _cancellation.Cancel();
        try { _worker.Wait(TimeSpan.FromSeconds(2)); } catch { }
        _cancellation.Dispose();
        GC.SuppressFinalize(this);
    }
}

public sealed class MultiInstanceWatcher : IDisposable
{
    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _worker;

    private MultiInstanceWatcher()
    {
        _mutex = new Mutex(true, "SynergyStrapper-MultiInstanceWatcher", out bool createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            throw new InvalidOperationException("A multi-instance watcher is already active.");
        }
        _worker = Task.Run(WatchAsync);
    }

    public static MultiInstanceWatcher? Start()
    {
        if (!App.Settings.Prop.Features.AllowMultipleInstances)
            return null;
        try
        {
            App.Logger.WriteLine("MultiInstanceWatcher", "Controlled multi-instance policy enabled.");
            return new MultiInstanceWatcher();
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("MultiInstanceWatcher::Start", ex);
            return null;
        }
    }

    private async Task WatchAsync()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            try
            {
                bool anyRoblox = Utilities.GetProcessesSafe().Any(x =>
                    x.ProcessName.Equals(App.RobloxPlayerAppName, StringComparison.OrdinalIgnoreCase) ||
                    x.ProcessName.Equals(App.RobloxStudioAppName, StringComparison.OrdinalIgnoreCase));
                if (!anyRoblox && !App.Settings.Prop.Features.KeepMultiInstanceWatcherAlive)
                    return;
                await Task.Delay(1000, _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("MultiInstanceWatcher", ex);
                return;
            }
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        try { _worker.Wait(TimeSpan.FromSeconds(2)); } catch { }
        _cancellation.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
        GC.SuppressFinalize(this);
    }
}
