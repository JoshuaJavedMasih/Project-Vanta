using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Vanta.Models;

namespace Vanta.Services;

public sealed class WindowsTelemetryProvider : ITelemetryProvider
{
    private readonly object _sync = new();
    private readonly string _cpuName;
    private readonly int? _cpuClockMhz;
    private readonly (string Name, long? MemoryBytes) _gpu;
    private ulong _previousIdle;
    private ulong _previousKernel;
    private ulong _previousUser;
    private DateTimeOffset _previousNetworkSample = DateTimeOffset.Now;
    private readonly Dictionary<string, (long Received, long Sent)> _networkCounters = new(StringComparer.Ordinal);
    private DateTimeOffset _previousProcessSample = DateTimeOffset.Now;
    private readonly Dictionary<int, TimeSpan> _processCpuTimes = new();

    public WindowsTelemetryProvider()
    {
        (_cpuName, _cpuClockMhz) = ReadCpuIdentity();
        _gpu = ReadGpuIdentity();
        ReadCpuUsage();
    }

    public TelemetrySnapshot Capture()
    {
        lock (_sync)
        {
            var now = DateTimeOffset.Now;
            var cpuUsage = ReadCpuUsage();
            var (totalMemory, usedMemory) = ReadMemory();
            var drives = ReadDrives();
            var network = ReadNetwork(now);
            var processes = ReadProcesses(now);
            var health = CalculateHealth(usedMemory, totalMemory, drives);
            var sensorCount = 3 + drives.Count + (network.IsConnected ? 2 : 0);

            return new TelemetrySnapshot(
                now,
                _cpuName,
                cpuUsage,
                Environment.ProcessorCount,
                _cpuClockMhz,
                null,
                _gpu.Name,
                null,
                null,
                _gpu.MemoryBytes,
                totalMemory,
                usedMemory,
                drives,
                network,
                processes,
                TimeSpan.FromMilliseconds(Environment.TickCount64),
                sensorCount,
                health.Status,
                health.Detail);
        }
    }

    private double ReadCpuUsage()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
        {
            return 0;
        }

        var idleTicks = ToUInt64(idle);
        var kernelTicks = ToUInt64(kernel);
        var userTicks = ToUInt64(user);
        var idleDelta = idleTicks - _previousIdle;
        var kernelDelta = kernelTicks - _previousKernel;
        var userDelta = userTicks - _previousUser;
        var total = kernelDelta + userDelta;

        _previousIdle = idleTicks;
        _previousKernel = kernelTicks;
        _previousUser = userTicks;

        return total == 0 ? 0 : Math.Clamp((total - idleDelta) * 100d / total, 0, 100);
    }

    private static (long Total, long Used) ReadMemory()
    {
        var status = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(ref status))
        {
            return (0, 0);
        }

        var total = checked((long)status.TotalPhysical);
        var available = checked((long)status.AvailablePhysical);
        return (total, Math.Max(0, total - available));
    }

    private static IReadOnlyList<DriveMetric> ReadDrives()
    {
        var result = new List<DriveMetric>();
        foreach (var drive in DriveInfo.GetDrives().Where(item => item.DriveType == DriveType.Fixed))
        {
            try
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                result.Add(new DriveMetric(
                    drive.Name.TrimEnd('\\'),
                    string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Local disk" : drive.VolumeLabel,
                    drive.DriveFormat,
                    drive.TotalSize,
                    drive.TotalSize - drive.AvailableFreeSpace));
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return result.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private NetworkMetric ReadNetwork(DateTimeOffset now)
    {
        var elapsed = Math.Max(0.001, (now - _previousNetworkSample).TotalSeconds);
        NetworkInterface? selected = null;

        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.OperationalStatus != OperationalStatus.Up ||
                adapter.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {
                continue;
            }

            if (selected is null || adapter.Speed > selected.Speed)
            {
                selected = adapter;
            }
        }

        if (selected is null)
        {
            _previousNetworkSample = now;
            return new NetworkMetric("No active adapter", "Offline", 0, 0, 0, false);
        }

        try
        {
            var stats = selected.GetIPv4Statistics();
            var key = selected.Id;
            var download = 0d;
            var upload = 0d;

            if (_networkCounters.TryGetValue(key, out var previous))
            {
                download = Math.Max(0, stats.BytesReceived - previous.Received) * 8d / elapsed / 1_000_000d;
                upload = Math.Max(0, stats.BytesSent - previous.Sent) * 8d / elapsed / 1_000_000d;
            }

            _networkCounters[key] = (stats.BytesReceived, stats.BytesSent);
            _previousNetworkSample = now;
            return new NetworkMetric(
                selected.Name,
                selected.NetworkInterfaceType.ToString(),
                download,
                upload,
                selected.Speed,
                true);
        }
        catch (NetworkInformationException)
        {
            _previousNetworkSample = now;
            return new NetworkMetric(selected.Name, selected.NetworkInterfaceType.ToString(), 0, 0, selected.Speed, true);
        }
    }

    private IReadOnlyList<ProcessMetric> ReadProcesses(DateTimeOffset now)
    {
        var elapsed = Math.Max(0.001, (now - _previousProcessSample).TotalSeconds);
        var nextCpuTimes = new Dictionary<int, TimeSpan>();
        var result = new List<ProcessMetric>();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    var cpuTime = process.TotalProcessorTime;
                    nextCpuTimes[process.Id] = cpuTime;
                    var cpu = 0d;
                    if (_processCpuTimes.TryGetValue(process.Id, out var previous))
                    {
                        cpu = Math.Clamp((cpuTime - previous).TotalSeconds / elapsed / Environment.ProcessorCount * 100d, 0, 100);
                    }

                    result.Add(new ProcessMetric(
                        process.ProcessName,
                        process.Id,
                        cpu,
                        process.WorkingSet64,
                        process.Threads.Count));
                }
                catch (InvalidOperationException)
                {
                }
                catch (System.ComponentModel.Win32Exception)
                {
                }
                catch (NotSupportedException)
                {
                }
            }
        }

        _processCpuTimes.Clear();
        foreach (var item in nextCpuTimes)
        {
            _processCpuTimes[item.Key] = item.Value;
        }

        _previousProcessSample = now;
        return result
            .OrderByDescending(item => item.CpuPercent)
            .ThenByDescending(item => item.WorkingSetBytes)
            .Take(8)
            .ToArray();
    }

    private static (string Name, int? ClockMhz) ReadCpuIdentity()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            var name = (key?.GetValue("ProcessorNameString") as string)?.Trim();
            var clock = key?.GetValue("~MHz") as int?;
            return (string.IsNullOrWhiteSpace(name) ? "Windows processor" : name, clock);
        }
        catch
        {
            return ("Windows processor", null);
        }
    }

    private static (string Name, long? MemoryBytes) ReadGpuIdentity()
    {
        try
        {
            using var video = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Video");
            if (video is null)
            {
                return ("Graphics adapter unavailable", null);
            }

            foreach (var adapterId in video.GetSubKeyNames())
            {
                using var adapter = video.OpenSubKey($@"{adapterId}\0000");
                var name = adapter?.GetValue("DriverDesc") as string;
                if (string.IsNullOrWhiteSpace(name) || name.Contains("Remote", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var rawMemory = adapter?.GetValue("HardwareInformation.qwMemorySize");
                long? memory = rawMemory switch
                {
                    long value => value,
                    byte[] bytes when bytes.Length >= sizeof(long) => BitConverter.ToInt64(bytes),
                    _ => null
                };
                return (name.Trim(), memory);
            }
        }
        catch
        {
        }

        return ("Graphics adapter unavailable", null);
    }

    private static (string Status, string Detail) CalculateHealth(long usedMemory, long totalMemory, IReadOnlyList<DriveMetric> drives)
    {
        var memoryPercent = totalMemory <= 0 ? 0 : usedMemory * 100d / totalMemory;
        var maximumDrivePercent = drives.Count == 0 ? 0 : drives.Max(item => item.UsedPercent);

        if (memoryPercent >= 95 || maximumDrivePercent >= 98)
        {
            return ("Critical", "Current resource pressure has crossed a critical measurable threshold.");
        }

        if (memoryPercent >= 85 || maximumDrivePercent >= 90)
        {
            return ("Attention", "Current memory or disk capacity is approaching its configured limit.");
        }

        return ("Healthy", "No critical resource-pressure condition is currently visible.");
    }

    private static ulong ToUInt64(FileTime value) => ((ulong)value.HighDateTime << 32) | value.LowDateTime;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;

        public MemoryStatusEx()
        {
        }
    }
}
