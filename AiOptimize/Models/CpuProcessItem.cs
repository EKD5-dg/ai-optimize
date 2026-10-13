namespace AiOptimize.Models;

/// <summary>CPU 明细项：进程名（同名已合并标注 ×N）+ 占总处理器能力的百分比 + 全部实例 PID（供「结束」使用）。</summary>
public sealed record CpuProcessItem(string Name, double Usage, IReadOnlyList<int> Pids);
