# Windows 电脑优化工具 设计文档

日期：2026-07-27
状态：已确认

## 1. 项目概述

一个 Windows 桌面电脑优化工具，提供：

- 实时监控：CPU 使用率、内存使用率、各磁盘分区使用率（1 秒刷新）
- 一键优化：临时文件清理、深度垃圾清理、内存释放
- 启动项管理：枚举并可启用/禁用开机自启项

## 2. 技术选型

| 项目 | 选择 |
|---|---|
| 框架 | .NET 8 + WPF |
| 架构模式 | MVVM（无第三方 MVVM 框架，手写 ViewModelBase + RelayCommand） |
| UI 风格 | 深色现代风格，纯手写 WPF 样式，不引入第三方 UI 库 |
| 权限 | 应用清单声明 `requireAdministrator`（清理系统缓存、释放其他进程内存需要） |
| 打包 | 单文件发布（self-contained 可选） |

## 3. 界面设计

单主窗口，深色主题（背景 #1E1E2E 系，强调色青/紫渐变），布局自上而下：

### 3.1 顶部监控区

三张监控卡片：

1. **CPU 卡片**：环形进度显示总使用率百分比
2. **内存卡片**：环形进度显示使用率 + 文字"已用 X GB / 共 Y GB"
3. **磁盘卡片**：每个固定分区一行，横向进度条 + "已用 X GB / 共 Y GB"，使用率超过 90% 进度条变红色警示

刷新频率 1 秒，数值变化平滑过渡。

### 3.2 中部优化项列表

四个可勾选项（默认全选前三项），每项显示扫描结果：

| 优化项 | 默认 | 扫描显示 |
|---|---|---|
| 临时文件清理 | ✔ | 可清理大小（MB/GB） |
| 深度垃圾清理 | ✔ | 可清理大小（MB/GB） |
| 内存释放 | ✔ | 当前内存使用率 |
| 启动项管理 | — | 自启项数量，点击打开管理弹窗 |

窗口打开后自动执行一次后台扫描，显示各项可清理大小。

### 3.3 底部操作区

- 「一键优化」大按钮（渐变强调色）
- 优化过程中显示进度条与当前步骤文字
- 完成后显示结果摘要，例如："已释放 2.3 GB 磁盘空间，内存使用率下降 18%，跳过 12 个被占用文件"

### 3.4 启动项管理弹窗

列表展示：名称、命令路径、来源（注册表/启动文件夹）、状态开关（ToggleSwitch 样式）。

## 4. 核心模块

项目结构：

```
AiOptimize/
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / .cs
├── app.manifest                    # requireAdministrator
├── ViewModels/
│   ├── ViewModelBase.cs
│   ├── RelayCommand.cs
│   ├── MainViewModel.cs
│   └── StartupManagerViewModel.cs
├── Views/
│   └── StartupManagerWindow.xaml
├── Services/
│   ├── SystemMonitorService.cs
│   ├── TempFileCleaner.cs
│   ├── DeepCleaner.cs
│   ├── MemoryOptimizer.cs
│   └── StartupManager.cs
├── Models/
│   └── (监控快照、清理结果、启动项等数据模型)
└── Native/
    └── NativeMethods.cs            # 全部 P/Invoke 声明
```

### 4.1 SystemMonitorService

- 职责：每秒采集一次系统指标，通过事件推送快照给 ViewModel
- CPU：`PerformanceCounter("Processor", "% Processor Time", "_Total")`
- 内存：P/Invoke `GlobalMemoryStatusEx`（总量、可用、使用率）
- 磁盘：`DriveInfo.GetDrives()` 过滤 `DriveType.Fixed && IsReady`
- 实现：`System.Threading.PeriodicTimer` 后台循环，UI 层经 Dispatcher 更新

### 4.2 TempFileCleaner（临时文件清理）

清理目标：

- `%TEMP%`（用户临时目录）
- `C:\Windows\Temp`
- 回收站：P/Invoke `SHEmptyRecycleBin`（静默、无确认音）

行为：先扫描统计可清理大小，清理时逐文件删除。

### 4.3 DeepCleaner（深度垃圾清理）

清理目标（仅已知安全路径）：

- 浏览器缓存：Chrome / Edge 的 `User Data\<Profile>\Cache`、`Code Cache`（仅缓存目录，不碰 Cookies、历史、密码）
- Windows 更新缓存：`C:\Windows\SoftwareDistribution\Download`
- 系统缩略图缓存：`%LocalAppData%\Microsoft\Windows\Explorer\thumbcache_*.db`
- Windows 错误报告：`%ProgramData%\Microsoft\Windows\WER\ReportQueue`、`ReportArchive`
- 预读文件：`C:\Windows\Prefetch\*.pf`

浏览器缓存清理前检测对应浏览器进程是否在运行，运行中则跳过该浏览器并在结果中说明。

### 4.4 MemoryOptimizer（内存释放）

两级释放：

1. 遍历所有可打开的进程，调用 `EmptyWorkingSet` 压缩工作集（跳过无权限进程与自身关键进程）
2. 清空系统待机内存列表：`NtSetSystemInformation(SystemMemoryListInformation, MemoryPurgeStandbyList)`，需先启用 `SeProfileSingleProcessPrivilege`

返回释放前后的内存使用率对比。

### 4.5 StartupManager（启动项管理）

枚举来源：

- 注册表 `HKCU\...\CurrentVersion\Run`、`HKLM\...\CurrentVersion\Run`（含 WOW6432Node）
- 启动文件夹：用户与公共 Startup 目录

启用/禁用方式：写 `HKCU/HKLM\...\Explorer\StartupApproved\Run` 的状态字节（与任务管理器一致的官方机制），**不删除**原始 Run 键值，可随时恢复。

## 5. 一键优化流程

1. 按勾选项顺序执行：临时文件 → 深度垃圾 → 内存释放
2. 每步开始时更新进度文字，步骤间进度条推进
3. 全部完成后重新扫描并显示结果摘要

## 6. 错误处理与安全策略

- **单文件级容错**：每个文件删除独立 try-catch，被占用/无权限的文件静默跳过并计数，最终汇总"跳过 N 个文件"
- **只删白名单路径**：所有清理路径硬编码为已知安全位置，不做任何模糊匹配式全盘扫描删除
- **回收站以外的删除均为永久删除**，但仅限缓存/临时文件性质的路径
- **降级运行**：若未取得管理员权限（清单失效等异常场景），系统级清理项禁用并提示，仅保留用户级功能
- **启动项可恢复**：禁用只改 StartupApproved 状态位，不删注册表值

## 7. 测试计划

- Services 层单元测试：路径扫描统计、启动项枚举解析、清理结果汇总逻辑（对文件系统操作使用临时目录模拟）
- 手动验收：
  1. 监控数值与任务管理器对比误差合理
  2. 一键优化全流程无崩溃，结果摘要正确
  3. 启动项禁用后任务管理器中状态同步为"已禁用"，可重新启用
  4. 非管理员场景降级提示正常

## 8. 明确不做（YAGNI）

- 不做注册表"深度清理/修复"（风险高、收益存疑）
- 不做驱动级/内核级优化
- 不做定时自动优化、开机自启
- 不做多语言，仅中文界面
