namespace AiOptimize.Models;

/// <summary>有问题的硬件设备信息。</summary>
public sealed record ProblemDevice(
    string Name,
    string DeviceId,
    string Status,
    string ProblemDescription);
