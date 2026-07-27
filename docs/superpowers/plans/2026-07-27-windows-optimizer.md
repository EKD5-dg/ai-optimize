# Windows 电脑优化工具 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个 WPF 深色主题的 Windows 优化工具：实时监控 CPU/内存/磁盘，一键执行临时文件清理、深度垃圾清理、内存释放，并提供启动项管理。

**Architecture:** .NET 8 + WPF + 手写 MVVM。Services 层封装全部系统操作（监控/清理/内存/启动项），Native 层集中 P/Invoke，ViewModel 只做编排与状态，View 纯 XAML 绑定。纯逻辑（字节格式化、清理统计、启动项状态位）用 xunit TDD 覆盖。

**Tech Stack:** .NET 8, WPF, xunit, System.Diagnostics.PerformanceCounter, Win32 P/Invoke (psapi/ntdll/shell32/advapi32), Microsoft.Win32 Registry

**规格文档:** `docs/superpowers/specs/2026-07-27-windows-optimizer-design.md`

---

### Task 1: 解决方案与项目骨架

**Files:**
- Create: `.gitignore`, `AiOptimize.sln`
- Create: `AiOptimize/AiOptimize.csproj`, `AiOptimize/app.manifest`
- Create: `AiOptimize.Tests/AiOptimize.Tests.csproj`

- [ ] **Step 1: 创建解决方案与项目**

```powershell
dotnet new gitignore
dotnet new sln -n AiOptimize
dotnet new wpf -n AiOptimize -f net8.0
dotnet new xunit -n AiOptimize.Tests -f net8.0
dotnet sln add AiOptimize AiOptimize.Tests
dotnet add AiOptimize.Tests reference AiOptimize
dotnet add AiOptimize package System.Diagnostics.PerformanceCounter
```

- [ ] **Step 2: 覆写 `AiOptimize/AiOptimize.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AssemblyTitle>AI 电脑优化助手</AssemblyTitle>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="System.Diagnostics.PerformanceCounter" Version="8.0.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: 创建 `AiOptimize/app.manifest`（管理员权限）**

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>
```

- [ ] **Step 4: 测试项目 csproj 改为 `net8.0-windows` + `UseWPF`（否则无法引用 WPF 工程）**

- [ ] **Step 5: 验证构建**

Run: `dotnet build`
Expected: Build succeeded, 0 Errors

- [ ] **Step 6: Commit** `chore: 初始化解决方案骨架`

---

### Task 2: 工具与模型层（TDD）

**Files:**
- Create: `AiOptimize/Utils/ByteFormatter.cs`
- Create: `AiOptimize/Models/CleanResult.cs`
- Create: `AiOptimize/Models/SystemSnapshot.cs`
- Create: `AiOptimize/Models/StartupItem.cs`
- Test: `AiOptimize.Tests/ByteFormatterTests.cs`, `AiOptimize.Tests/CleanResultTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
// ByteFormatterTests.cs
using AiOptimize.Utils;

namespace AiOptimize.Tests;

public class ByteFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1073741824, "1 GB")]
    public void Format_ReturnsHumanReadable(long bytes, string expected)
        => Assert.Equal(expected, ByteFormatter.Format(bytes));
}
```

```csharp
// CleanResultTests.cs
using AiOptimize.Models;

namespace AiOptimize.Tests;

public class CleanResultTests
{
    [Fact]
    public void Merge_SumsCountsAndConcatsNotes()
    {
        var a = new CleanResult { BytesFreed = 100, FilesDeleted = 2, FilesSkipped = 1 };
        a.Notes.Add("n1");
        var b = new CleanResult { BytesFreed = 50, FilesDeleted = 1, FilesSkipped = 3 };
        b.Notes.Add("n2");

        a.Merge(b);

        Assert.Equal(150, a.BytesFreed);
        Assert.Equal(3, a.FilesDeleted);
        Assert.Equal(4, a.FilesSkipped);
        Assert.Equal(new[] { "n1", "n2" }, a.Notes);
    }
}
```

- [ ] **Step 2: 运行确认编译失败**（类型不存在）

- [ ] **Step 3: 实现**

```csharp
// Utils/ByteFormatter.cs
namespace AiOptimize.Utils;

public static class ByteFormatter
{
    public static string Format(long bytes) => Format((double)bytes);

    public static string Format(double bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        while (bytes >= 1024 && i < units.Length - 1) { bytes /= 1024; i++; }
        return $"{bytes:0.#} {units[i]}";
    }
}
```

```csharp
// Models/CleanResult.cs
namespace AiOptimize.Models;

public sealed class CleanResult
{
    public long BytesFreed { get; set; }
    public int FilesDeleted { get; set; }
    public int FilesSkipped { get; set; }
    public List<string> Notes { get; } = new();

    public void Merge(CleanResult other)
    {
        BytesFreed += other.BytesFreed;
        FilesDeleted += other.FilesDeleted;
        FilesSkipped += other.FilesSkipped;
        Notes.AddRange(other.Notes);
    }
}
```

```csharp
// Models/SystemSnapshot.cs
namespace AiOptimize.Models;

public sealed record DiskUsageInfo(string Name, long UsedBytes, long TotalBytes)
{
    public double Usage => TotalBytes == 0 ? 0 : UsedBytes * 100.0 / TotalBytes;
}

public sealed record SystemSnapshot(
    double CpuUsage,
    ulong MemoryUsedBytes,
    ulong MemoryTotalBytes,
    IReadOnlyList<DiskUsageInfo> Disks)
{
    public double MemoryUsage => MemoryTotalBytes == 0 ? 0 : MemoryUsedBytes * 100.0 / MemoryTotalBytes;
}
```

```csharp
// Models/StartupItem.cs
namespace AiOptimize.Models;

public enum StartupSource
{
    HkcuRun,
    HklmRun,
    HklmRunWow64,
    UserStartupFolder,
    CommonStartupFolder,
}

public sealed class StartupItem
{
    public required string Name { get; init; }
    public required string Command { get; init; }
    public required StartupSource Source { get; init; }
    public bool IsEnabled { get; set; }

    public string SourceDisplay => Source switch
    {
        StartupSource.HkcuRun => "注册表（当前用户）",
        StartupSource.HklmRun => "注册表（所有用户）",
        StartupSource.HklmRunWow64 => "注册表（所有用户 32 位）",
        StartupSource.UserStartupFolder => "启动文件夹（当前用户）",
        StartupSource.CommonStartupFolder => "启动文件夹（所有用户）",
        _ => "未知",
    };
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test`
Expected: 全部 PASS

- [ ] **Step 5: Commit** `feat: 工具与模型层`

---

### Task 3: Native P/Invoke 层

**Files:**
- Create: `AiOptimize/Native/NativeMethods.cs`

- [ ] **Step 1: 实现全部 P/Invoke 声明**

```csharp
using System.Runtime.InteropServices;

namespace AiOptimize.Native;

internal static class NativeMethods
{
    // ---- 内存状态 ----
    [StructLayout(LayoutKind.Sequential)]
    internal struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public static MEMORYSTATUSEX Create() => new() { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    // ---- 工作集压缩 ----
    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EmptyWorkingSet(IntPtr hProcess);

    // ---- 回收站 ----
    internal const uint SHERB_NOCONFIRMATION = 0x1;
    internal const uint SHERB_NOPROGRESSUI = 0x2;
    internal const uint SHERB_NOSOUND = 0x4;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    internal static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    internal static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    // ---- 待机内存列表清理 ----
    internal const int SystemMemoryListInformation = 80;
    internal const int MemoryPurgeStandbyList = 4;

    [DllImport("ntdll.dll")]
    internal static extern int NtSetSystemInformation(int infoClass, ref int info, int length);

    // ---- 权限提升 ----
    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID { public uint LowPart; public int HighPart; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    internal const uint SE_PRIVILEGE_ENABLED = 0x2;
    internal const uint TOKEN_ADJUST_PRIVILEGES = 0x20;
    internal const uint TOKEN_QUERY = 0x8;
    internal const string SE_PROFILE_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege";

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool LookupPrivilegeValue(string? systemName, string name, out LUID luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AdjustTokenPrivileges(IntPtr tokenHandle,
        [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
        ref TOKEN_PRIVILEGES newState, uint bufferLength, IntPtr previousState, IntPtr returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr handle);
}
```

- [ ] **Step 2: 构建通过后 Commit** `feat: Native P/Invoke 层`

---

### Task 4: FileCleanupHelper 文件清理基础库（TDD）

**Files:**
- Create: `AiOptimize/Services/FileCleanupHelper.cs`
- Test: `AiOptimize.Tests/FileCleanupHelperTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
using AiOptimize.Models;
using AiOptimize.Services;

namespace AiOptimize.Tests;

public class FileCleanupHelperTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "AiOptimizeTests_" + Guid.NewGuid().ToString("N"));

    public FileCleanupHelperTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void GetDirectorySize_SumsAllFilesRecursively()
    {
        File.WriteAllBytes(Path.Combine(_dir, "a.bin"), new byte[100]);
        var sub = Directory.CreateDirectory(Path.Combine(_dir, "sub")).FullName;
        File.WriteAllBytes(Path.Combine(sub, "b.bin"), new byte[50]);

        Assert.Equal(150, FileCleanupHelper.GetDirectorySize(_dir));
    }

    [Fact]
    public void GetDirectorySize_MissingDirectory_ReturnsZero()
        => Assert.Equal(0, FileCleanupHelper.GetDirectorySize(Path.Combine(_dir, "nope")));

    [Fact]
    public void GetFilesSize_OnlyMatchesPattern()
    {
        File.WriteAllBytes(Path.Combine(_dir, "a.pf"), new byte[30]);
        File.WriteAllBytes(Path.Combine(_dir, "b.txt"), new byte[99]);

        Assert.Equal(30, FileCleanupHelper.GetFilesSize(_dir, "*.pf"));
    }

    [Fact]
    public void DeleteDirectoryContents_DeletesFilesKeepsRoot()
    {
        File.WriteAllBytes(Path.Combine(_dir, "a.bin"), new byte[100]);
        var sub = Directory.CreateDirectory(Path.Combine(_dir, "sub")).FullName;
        File.WriteAllBytes(Path.Combine(sub, "b.bin"), new byte[50]);

        var result = new CleanResult();
        FileCleanupHelper.DeleteDirectoryContents(_dir, result);

        Assert.True(Directory.Exists(_dir));
        Assert.Empty(Directory.EnumerateFileSystemEntries(_dir));
        Assert.Equal(150, result.BytesFreed);
        Assert.Equal(2, result.FilesDeleted);
        Assert.Equal(0, result.FilesSkipped);
    }

    [Fact]
    public void DeleteDirectoryContents_LockedFile_SkippedAndCounted()
    {
        var locked = Path.Combine(_dir, "locked.bin");
        File.WriteAllBytes(locked, new byte[10]);
        using var stream = new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None);

        var result = new CleanResult();
        FileCleanupHelper.DeleteDirectoryContents(_dir, result);

        Assert.Equal(1, result.FilesSkipped);
        Assert.True(File.Exists(locked));
    }

    [Fact]
    public void DeleteFiles_OnlyDeletesMatchingPattern()
    {
        File.WriteAllBytes(Path.Combine(_dir, "a.pf"), new byte[30]);
        File.WriteAllBytes(Path.Combine(_dir, "b.txt"), new byte[99]);

        var result = new CleanResult();
        FileCleanupHelper.DeleteFiles(_dir, "*.pf", result);

        Assert.False(File.Exists(Path.Combine(_dir, "a.pf")));
        Assert.True(File.Exists(Path.Combine(_dir, "b.txt")));
        Assert.Equal(30, result.BytesFreed);
    }
}
```

- [ ] **Step 2: 运行确认失败**

- [ ] **Step 3: 实现**

```csharp
// Services/FileCleanupHelper.cs
using System.IO;
using AiOptimize.Models;

namespace AiOptimize.Services;

/// <summary>文件扫描与逐文件容错删除的基础库，所有清理服务共用。</summary>
public static class FileCleanupHelper
{
    public static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        long size = 0;
        foreach (var file in EnumerateFilesSafe(path))
        {
            try { size += new FileInfo(file).Length; } catch { }
        }
        return size;
    }

    public static long GetFilesSize(string directory, string pattern)
    {
        if (!Directory.Exists(directory)) return 0;
        long size = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, pattern))
            {
                try { size += new FileInfo(file).Length; } catch { }
            }
        }
        catch { }
        return size;
    }

    /// <summary>删除目录下全部内容（保留目录本身），逐文件容错。</summary>
    public static void DeleteDirectoryContents(string path, CleanResult result)
    {
        if (!Directory.Exists(path)) return;
        foreach (var file in EnumerateFilesSafe(path))
        {
            TryDeleteFile(file, result);
        }
        try
        {
            // 由深到浅删除已清空的子目录
            foreach (var dir in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)
                         .OrderByDescending(d => d.Length))
            {
                try { Directory.Delete(dir, false); } catch { }
            }
        }
        catch { }
    }

    public static void DeleteFiles(string directory, string pattern, CleanResult result)
    {
        if (!Directory.Exists(directory)) return;
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, pattern))
            {
                TryDeleteFile(file, result);
            }
        }
        catch { }
    }

    private static void TryDeleteFile(string file, CleanResult result)
    {
        try
        {
            var info = new FileInfo(file);
            long length = info.Length;
            info.Attributes = FileAttributes.Normal;
            info.Delete();
            result.BytesFreed += length;
            result.FilesDeleted++;
        }
        catch
        {
            result.FilesSkipped++;
        }
    }

    private static IEnumerable<string> EnumerateFilesSafe(string path)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };
        IEnumerable<string> files;
        try { files = Directory.EnumerateFiles(path, "*", options); }
        catch { yield break; }
        foreach (var f in files) yield return f;
    }
}
```

- [ ] **Step 4: 运行测试确认通过** `dotnet test`

- [ ] **Step 5: Commit** `feat: 文件清理基础库`

---

### Task 5: TempFileCleaner 与 DeepCleaner

**Files:**
- Create: `AiOptimize/Services/TempFileCleaner.cs`
- Create: `AiOptimize/Services/DeepCleaner.cs`

- [ ] **Step 1: 实现 TempFileCleaner**

```csharp
// Services/TempFileCleaner.cs
using System.IO;
using System.Runtime.InteropServices;
using AiOptimize.Models;
using AiOptimize.Native;

namespace AiOptimize.Services;

/// <summary>临时文件清理：用户临时目录、Windows\Temp、回收站。</summary>
public sealed class TempFileCleaner
{
    private static readonly string[] TargetDirectories =
    {
        Path.GetTempPath(),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
    };

    public Task<long> ScanAsync() => Task.Run(() =>
        TargetDirectories.Sum(FileCleanupHelper.GetDirectorySize) + GetRecycleBinSize());

    public Task<CleanResult> CleanAsync() => Task.Run(() =>
    {
        var result = new CleanResult();
        foreach (var dir in TargetDirectories)
        {
            FileCleanupHelper.DeleteDirectoryContents(dir, result);
        }

        long recycleSize = GetRecycleBinSize();
        int hr = NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null,
            NativeMethods.SHERB_NOCONFIRMATION | NativeMethods.SHERB_NOPROGRESSUI | NativeMethods.SHERB_NOSOUND);
        if (hr == 0 && recycleSize > 0)
        {
            result.BytesFreed += recycleSize;
            result.Notes.Add("回收站已清空");
        }
        return result;
    });

    public static long GetRecycleBinSize()
    {
        var info = new NativeMethods.SHQUERYRBINFO
        {
            cbSize = Marshal.SizeOf<NativeMethods.SHQUERYRBINFO>(),
        };
        return NativeMethods.SHQueryRecycleBin(null, ref info) == 0 ? info.i64Size : 0;
    }
}
```

- [ ] **Step 2: 实现 DeepCleaner**

```csharp
// Services/DeepCleaner.cs
using System.Diagnostics;
using System.IO;
using AiOptimize.Models;

namespace AiOptimize.Services;

/// <summary>深度垃圾清理：浏览器缓存、更新缓存、缩略图、错误报告、预读文件。</summary>
public sealed class DeepCleaner
{
    private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static string WindowsDir => Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    private static string ProgramData => Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

    private sealed record BrowserTarget(string DisplayName, string ProcessName, string UserDataDir);

    private static readonly BrowserTarget[] Browsers =
    {
        new("Chrome", "chrome", Path.Combine(LocalAppData, "Google", "Chrome", "User Data")),
        new("Edge", "msedge", Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data")),
    };

    public Task<long> ScanAsync() => Task.Run(() =>
    {
        long size = 0;
        foreach (var browser in Browsers)
        {
            foreach (var cacheDir in GetBrowserCacheDirs(browser))
                size += FileCleanupHelper.GetDirectorySize(cacheDir);
        }
        size += FileCleanupHelper.GetDirectorySize(Path.Combine(WindowsDir, "SoftwareDistribution", "Download"));
        size += FileCleanupHelper.GetFilesSize(Path.Combine(LocalAppData, @"Microsoft\Windows\Explorer"), "thumbcache_*.db");
        size += FileCleanupHelper.GetDirectorySize(Path.Combine(ProgramData, @"Microsoft\Windows\WER\ReportQueue"));
        size += FileCleanupHelper.GetDirectorySize(Path.Combine(ProgramData, @"Microsoft\Windows\WER\ReportArchive"));
        size += FileCleanupHelper.GetFilesSize(Path.Combine(WindowsDir, "Prefetch"), "*.pf");
        return size;
    });

    public Task<CleanResult> CleanAsync() => Task.Run(() =>
    {
        var result = new CleanResult();
        foreach (var browser in Browsers)
        {
            if (Process.GetProcessesByName(browser.ProcessName).Length > 0)
            {
                result.Notes.Add($"{browser.DisplayName} 正在运行，已跳过其缓存");
                continue;
            }
            foreach (var cacheDir in GetBrowserCacheDirs(browser))
                FileCleanupHelper.DeleteDirectoryContents(cacheDir, result);
        }

        FileCleanupHelper.DeleteDirectoryContents(Path.Combine(WindowsDir, "SoftwareDistribution", "Download"), result);
        FileCleanupHelper.DeleteFiles(Path.Combine(LocalAppData, @"Microsoft\Windows\Explorer"), "thumbcache_*.db", result);
        FileCleanupHelper.DeleteDirectoryContents(Path.Combine(ProgramData, @"Microsoft\Windows\WER\ReportQueue"), result);
        FileCleanupHelper.DeleteDirectoryContents(Path.Combine(ProgramData, @"Microsoft\Windows\WER\ReportArchive"), result);
        FileCleanupHelper.DeleteFiles(Path.Combine(WindowsDir, "Prefetch"), "*.pf", result);
        return result;
    });

    private static IEnumerable<string> GetBrowserCacheDirs(BrowserTarget browser)
    {
        if (!Directory.Exists(browser.UserDataDir)) yield break;
        List<string> profiles = new();
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(browser.UserDataDir))
            {
                var name = Path.GetFileName(dir);
                if (name == "Default" || name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase))
                    profiles.Add(dir);
            }
        }
        catch { yield break; }

        foreach (var profile in profiles)
        {
            yield return Path.Combine(profile, "Cache");
            yield return Path.Combine(profile, "Code Cache");
        }
    }
}
```

- [ ] **Step 3: 构建通过后 Commit** `feat: 临时文件与深度垃圾清理服务`

---

### Task 6: MemoryOptimizer 内存释放

**Files:**
- Create: `AiOptimize/Services/MemoryOptimizer.cs`

- [ ] **Step 1: 实现**

```csharp
// Services/MemoryOptimizer.cs
using System.Diagnostics;
using AiOptimize.Native;

namespace AiOptimize.Services;

public sealed record MemoryOptimizeResult(double BeforeUsage, double AfterUsage, long FreedBytes, int ProcessesTrimmed);

/// <summary>内存释放：压缩各进程工作集 + 清空系统待机内存列表。</summary>
public sealed class MemoryOptimizer
{
    public Task<MemoryOptimizeResult> OptimizeAsync() => Task.Run(() =>
    {
        var before = ReadMemory();
        int trimmed = 0;
        int selfId = Environment.ProcessId;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id == selfId) continue;
                if (NativeMethods.EmptyWorkingSet(process.Handle)) trimmed++;
            }
            catch { /* 无权限或已退出的进程跳过 */ }
            finally { process.Dispose(); }
        }

        PurgeStandbyList();

        Thread.Sleep(500); // 等系统内存统计刷新
        var after = ReadMemory();
        long freed = Math.Max(0, (long)before.Used - (long)after.Used);
        return new MemoryOptimizeResult(before.Usage, after.Usage, freed, trimmed);
    });

    private static (ulong Used, double Usage) ReadMemory()
    {
        var mem = NativeMethods.MEMORYSTATUSEX.Create();
        if (!NativeMethods.GlobalMemoryStatusEx(ref mem)) return (0, 0);
        ulong used = mem.ullTotalPhys - mem.ullAvailPhys;
        return (used, used * 100.0 / mem.ullTotalPhys);
    }

    private static void PurgeStandbyList()
    {
        try
        {
            EnablePrivilege(NativeMethods.SE_PROFILE_SINGLE_PROCESS_NAME);
            int command = NativeMethods.MemoryPurgeStandbyList;
            NativeMethods.NtSetSystemInformation(NativeMethods.SystemMemoryListInformation, ref command, sizeof(int));
        }
        catch { /* 权限不足时静默降级 */ }
    }

    private static void EnablePrivilege(string privilege)
    {
        using var current = Process.GetCurrentProcess();
        if (!NativeMethods.OpenProcessToken(current.Handle,
                NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_QUERY, out var token))
            return;
        try
        {
            if (!NativeMethods.LookupPrivilegeValue(null, privilege, out var luid)) return;
            var tp = new NativeMethods.TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = NativeMethods.SE_PRIVILEGE_ENABLED,
            };
            NativeMethods.AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            NativeMethods.CloseHandle(token);
        }
    }
}
```

- [ ] **Step 2: 构建通过后 Commit** `feat: 内存释放服务`

---

### Task 7: 启动项管理（TDD 状态位逻辑）

**Files:**
- Create: `AiOptimize/Services/StartupApprovedState.cs`
- Create: `AiOptimize/Services/StartupManager.cs`
- Test: `AiOptimize.Tests/StartupApprovedStateTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
using AiOptimize.Services;

namespace AiOptimize.Tests;

public class StartupApprovedStateTests
{
    [Fact]
    public void IsEnabled_NullOrEmpty_ReturnsTrue()
    {
        Assert.True(StartupApprovedState.IsEnabled(null));
        Assert.True(StartupApprovedState.IsEnabled(Array.Empty<byte>()));
    }

    [Fact]
    public void IsEnabled_EnabledValue_ReturnsTrue()
        => Assert.True(StartupApprovedState.IsEnabled(StartupApprovedState.CreateEnabledValue()));

    [Fact]
    public void IsEnabled_DisabledValue_ReturnsFalse()
        => Assert.False(StartupApprovedState.IsEnabled(StartupApprovedState.CreateDisabledValue()));

    [Fact]
    public void CreateDisabledValue_Is12BytesWithTimestamp()
    {
        var data = StartupApprovedState.CreateDisabledValue();
        Assert.Equal(12, data.Length);
        Assert.Equal(0x03, data[0]);
        Assert.NotEqual(0L, BitConverter.ToInt64(data, 4));
    }
}
```

- [ ] **Step 2: 运行确认失败，然后实现**

```csharp
// Services/StartupApprovedState.cs
namespace AiOptimize.Services;

/// <summary>任务管理器 StartupApproved 二进制值约定：首字节偶数=启用，奇数=禁用；无数据视为启用。</summary>
public static class StartupApprovedState
{
    public static bool IsEnabled(byte[]? data)
        => data is null || data.Length == 0 || (data[0] & 0x01) == 0;

    public static byte[] CreateEnabledValue()
    {
        var data = new byte[12];
        data[0] = 0x02;
        return data;
    }

    public static byte[] CreateDisabledValue()
    {
        var data = new byte[12];
        data[0] = 0x03;
        BitConverter.GetBytes(DateTime.Now.ToFileTime()).CopyTo(data, 4);
        return data;
    }
}
```

```csharp
// Services/StartupManager.cs
using System.IO;
using AiOptimize.Models;
using Microsoft.Win32;

namespace AiOptimize.Services;

/// <summary>启动项枚举与启用/禁用（只写 StartupApproved 状态位，不删除原始项）。</summary>
public sealed class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunKeyWow64 = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedRun = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ApprovedRun32 = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32";
    private const string ApprovedFolder = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

    public List<StartupItem> GetItems()
    {
        var items = new List<StartupItem>();
        CollectRegistryItems(Registry.CurrentUser, RunKey, ApprovedRun, StartupSource.HkcuRun, items);
        CollectRegistryItems(Registry.LocalMachine, RunKey, ApprovedRun, StartupSource.HklmRun, items);
        CollectRegistryItems(Registry.LocalMachine, RunKeyWow64, ApprovedRun32, StartupSource.HklmRunWow64, items);
        CollectFolderItems(Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            Registry.CurrentUser, StartupSource.UserStartupFolder, items);
        CollectFolderItems(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
            Registry.LocalMachine, StartupSource.CommonStartupFolder, items);
        return items;
    }

    public void SetEnabled(StartupItem item, bool enabled)
    {
        var (root, approvedPath) = item.Source switch
        {
            StartupSource.HkcuRun => (Registry.CurrentUser, ApprovedRun),
            StartupSource.HklmRun => (Registry.LocalMachine, ApprovedRun),
            StartupSource.HklmRunWow64 => (Registry.LocalMachine, ApprovedRun32),
            StartupSource.UserStartupFolder => (Registry.CurrentUser, ApprovedFolder),
            StartupSource.CommonStartupFolder => (Registry.LocalMachine, ApprovedFolder),
            _ => throw new InvalidOperationException($"未知来源: {item.Source}"),
        };
        using var key = root.CreateSubKey(approvedPath, writable: true)
            ?? throw new InvalidOperationException("无法打开 StartupApproved 注册表键");
        key.SetValue(item.Name,
            enabled ? StartupApprovedState.CreateEnabledValue() : StartupApprovedState.CreateDisabledValue(),
            RegistryValueKind.Binary);
        item.IsEnabled = enabled;
    }

    private static void CollectRegistryItems(RegistryKey root, string runPath, string approvedPath,
        StartupSource source, List<StartupItem> items)
    {
        using var runKey = root.OpenSubKey(runPath);
        if (runKey is null) return;
        using var approvedKey = root.OpenSubKey(approvedPath);
        foreach (var name in runKey.GetValueNames())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            items.Add(new StartupItem
            {
                Name = name,
                Command = runKey.GetValue(name)?.ToString() ?? "",
                Source = source,
                IsEnabled = StartupApprovedState.IsEnabled(approvedKey?.GetValue(name) as byte[]),
            });
        }
    }

    private static void CollectFolderItems(string folder, RegistryKey approvedRoot,
        StartupSource source, List<StartupItem> items)
    {
        if (!Directory.Exists(folder)) return;
        using var approvedKey = approvedRoot.OpenSubKey(ApprovedFolder);
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var fileName = Path.GetFileName(file);
            if (fileName.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
            items.Add(new StartupItem
            {
                Name = fileName,
                Command = file,
                Source = source,
                IsEnabled = StartupApprovedState.IsEnabled(approvedKey?.GetValue(fileName) as byte[]),
            });
        }
    }
}
```

- [ ] **Step 3: `dotnet test` 全部通过后 Commit** `feat: 启动项管理服务`

---

### Task 8: SystemMonitorService 实时监控

**Files:**
- Create: `AiOptimize/Services/SystemMonitorService.cs`

- [ ] **Step 1: 实现**

```csharp
// Services/SystemMonitorService.cs
using System.Diagnostics;
using System.IO;
using AiOptimize.Models;
using AiOptimize.Native;

namespace AiOptimize.Services;

/// <summary>每秒采集 CPU/内存/磁盘指标并通过事件推送。</summary>
public sealed class SystemMonitorService : IDisposable
{
    private readonly PerformanceCounter _cpuCounter = new("Processor", "% Processor Time", "_Total");
    private readonly CancellationTokenSource _cts = new();

    public event Action<SystemSnapshot>? SnapshotUpdated;

    public void Start() => _ = RunAsync(_cts.Token);

    private async Task RunAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                SnapshotUpdated?.Invoke(Capture());
            }
        }
        catch (OperationCanceledException) { }
    }

    public SystemSnapshot Capture()
    {
        double cpu = 0;
        try { cpu = Math.Clamp(_cpuCounter.NextValue(), 0, 100); } catch { }

        var mem = NativeMethods.MEMORYSTATUSEX.Create();
        ulong total = 0, used = 0;
        if (NativeMethods.GlobalMemoryStatusEx(ref mem))
        {
            total = mem.ullTotalPhys;
            used = mem.ullTotalPhys - mem.ullAvailPhys;
        }

        var disks = new List<DiskUsageInfo>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;
                disks.Add(new DiskUsageInfo(
                    drive.Name.TrimEnd('\\'),
                    drive.TotalSize - drive.TotalFreeSpace,
                    drive.TotalSize));
            }
            catch { }
        }

        return new SystemSnapshot(cpu, used, total, disks);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _cpuCounter.Dispose();
    }
}
```

- [ ] **Step 2: 构建通过后 Commit** `feat: 系统监控服务`

---

### Task 9: MVVM 基础、转换器与 MainViewModel

**Files:**
- Create: `AiOptimize/ViewModels/ViewModelBase.cs`
- Create: `AiOptimize/ViewModels/RelayCommand.cs`
- Create: `AiOptimize/ViewModels/MainViewModel.cs`
- Create: `AiOptimize/ViewModels/StartupManagerViewModel.cs`
- Create: `AiOptimize/Converters/BytesToTextConverter.cs`
- Create: `AiOptimize/Converters/UsageToBrushConverter.cs`

实现时保持以下签名与行为一致：

- `ViewModelBase`：`INotifyPropertyChanged` + `SetProperty<T>(ref T, T, [CallerMemberName])`
- `RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)`：`CanExecuteChanged` 挂接 `CommandManager.RequerySuggested`
- `BytesToTextConverter`：long/ulong/double → `ByteFormatter.Format`
- `UsageToBrushConverter`：`double >= 90` 返回红色 `#EF5350`，否则青色 `#4FC3F7`
- `MainViewModel` 公开成员：
  - 监控：`CpuUsage`、`MemoryUsage`、`MemoryText`、`Disks (IReadOnlyList<DiskUsageInfo>)`
  - 勾选：`IsTempSelected=true`、`IsDeepSelected=true`、`IsMemorySelected=true`
  - 扫描文案：`TempScanText`、`DeepScanText`、`MemoryInfoText`、`StartupCountText`
  - 优化状态：`IsOptimizing`、`ProgressValue`、`ProgressText`、`ResultText`
  - 命令：`OptimizeCommand`（执行中或全不勾选时禁用）
  - 方法：`RefreshStartupCount()`、`Shutdown()`（dispose 监控）
- 构造函数：订阅 `SystemMonitorService.SnapshotUpdated`（经 `Application.Current.Dispatcher` 回 UI 线程）、`Start()`、后台执行初始扫描 `RefreshScanAsync()`
- `OptimizeAsync()` 流程：按勾选依次 临时清理 → 深度清理 → 内存释放，每步更新 `ProgressText/ProgressValue`，`CleanResult.Merge` 汇总，结束生成摘要（释放空间、内存前后使用率、跳过文件数、Notes），finally 中 `IsOptimizing=false` 并重新扫描
- `StartupManagerViewModel`：`ObservableCollection<StartupItemViewModel>`；`StartupItemViewModel.IsEnabled` setter 调用 `StartupManager.SetEnabled`，异常时 `MessageBox` 提示并还原

- [ ] **Step 1: 实现全部六个文件，构建通过**
- [ ] **Step 2: Commit** `feat: MVVM 层与主视图模型`

---

### Task 10: UI —— 样式、环形进度控件、主窗口、启动项窗口

**Files:**
- Create: `AiOptimize/Controls/RingProgress.cs`
- Modify: `AiOptimize/App.xaml`（全局样式资源）
- Modify: `AiOptimize/MainWindow.xaml` / `.xaml.cs`
- Create: `AiOptimize/Views/StartupManagerWindow.xaml` / `.xaml.cs`

**RingProgress**：继承 `FrameworkElement`，依赖属性 `Value(0-100)`、`Thickness(默认12)`、`RingBrush`、`TrackBrush`，全部 `AffectsRender`；`OnRender` 先画轨道圆，再按 `Value/100*360` 度画圆弧（`StreamGeometry.ArcTo` + 圆头 `PenLineCap.Round`，起点 12 点方向顺时针，>=360 度退化为整圆）。

**App.xaml 资源**（深色主题）：
- 颜色：背景 `#1E1E2E`、卡片 `#27273A`、次要文字 `#9A9AB0`、主文字 `#E6E6F0`、强调青 `#4FC3F7`、强调紫 `#7C6CF0`、成功绿 `#7CE38B`、警示红 `#EF5350`
- `Card`：Border 样式，圆角 12、内边距 16、卡片底色
- `SlimProgressBar`：无边框圆角模板，`PART_Track` + `PART_Indicator`
- `AccentButton`：青→紫横向渐变、圆角 10、按下变暗、禁用半透明
- `ToggleSwitch`：基于 ToggleButton 的开关模板（圆角轨道 + 圆形滑块，选中态紫色、滑块右移）

**MainWindow.xaml** 结构：
1. 标题行
2. 三张监控卡片（Grid 三列，磁盘列 1.4 倍宽）：CPU/内存卡片 = RingProgress + 居中百分比文字，内存卡片下方 `MemoryText`；磁盘卡片 = `ItemsControl` 绑定 `Disks`，行模板 = 盘符 + `SlimProgressBar`（Foreground 走 `UsageToBrushConverter`）+ 已用/总量（`BytesToTextConverter`）
3. 四行优化项卡片：CheckBox + 标题/说明 + 右侧扫描文案；第四行启动项无 CheckBox，右侧「管理」按钮（code-behind 打开 `StartupManagerWindow`，关闭后调用 `RefreshStartupCount()`）
4. 底部：优化进度条（`IsOptimizing` 时可见，`BooleanToVisibilityConverter`）+ `ProgressText` + `ResultText`（绿色）+「一键优化」`AccentButton`（高 52，绑定 `OptimizeCommand`）

`MainWindow.xaml.cs`：`DataContext = new MainViewModel()`；`OnClosed` 调用 `viewModel.Shutdown()`。

**StartupManagerWindow.xaml**：深色窗口，`ScrollViewer + ItemsControl` 绑定 `Items`，行模板 = 名称（粗体）+ 命令路径（灰色小字、`TextTrimming`）+ 来源标签 + `ToggleSwitch`（双向绑定 `IsEnabled`）；底部灰字提示“禁用不会删除程序，仅阻止其开机自动启动，可随时重新开启”。

- [ ] **Step 1: 实现 RingProgress 与 App.xaml 样式，构建通过**
- [ ] **Step 2: 实现 MainWindow 与 StartupManagerWindow，构建通过**
- [ ] **Step 3: Commit** `feat: 深色主题 UI 与主窗口`

---

### Task 11: 集成验证与发布

- [ ] **Step 1: 全量测试** — Run: `dotnet test`，Expected: 全部 PASS
- [ ] **Step 2: Release 构建** — Run: `dotnet build -c Release`，Expected: 0 Error
- [ ] **Step 3: 启动程序手动验收**（需要 UAC 确认）
  1. 监控数值与任务管理器对比合理，1 秒刷新
  2. 打开后自动显示各项可清理大小
  3. 一键优化全流程无崩溃、显示结果摘要
  4. 启动项弹窗可开关，任务管理器“启动应用”中状态同步
  5. 使用率超过 90% 的磁盘进度条为红色（如无法构造可跳过）
- [ ] **Step 4: 发布单文件版本** — Run: `dotnet publish AiOptimize -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish`，Expected: 生成 `publish/AiOptimize.exe`
- [ ] **Step 5: Commit** `chore: 发布配置与验收`

---

## Self-Review 结论

- **规格覆盖**：设计文档第 3-6 节全部映射到 Task 2-10；测试计划（第 7 节）映射到 Task 2/4/7 单元测试与 Task 11 手动验收 ✔
- **占位符扫描**：Task 9/10 以精确接口签名 + 结构说明表达，无 TBD/TODO ✔
- **类型一致性**：`CleanResult`/`SystemSnapshot`/`DiskUsageInfo`/`StartupItem`/`ByteFormatter` 等类型在各任务间签名一致 ✔
