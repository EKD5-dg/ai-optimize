namespace AiOptimize.Models;

/// <summary>单个进程的 CPU 采样结果：PID、进程名、占总处理器能力的百分比、是否有可见窗口。</summary>
public sealed record CpuProcessSample(int Pid, string Name, double Usage, bool HasWindow);
