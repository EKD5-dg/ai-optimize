namespace AiOptimize.Models;

/// <summary>CPU 明细项：进程名（同名已合并标注 ×N）+ 占总处理器能力的百分比。</summary>
public sealed record CpuProcessItem(string Name, double Usage);
