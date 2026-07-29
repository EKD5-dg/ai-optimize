# 蓝屏分析功能 设计文档

日期：2026-07-28
状态：已确认（方案 A：事件日志 + 错误代码知识库）

## 1. 功能概述

在「AI 电脑优化助手」中新增蓝屏分析：读取 Windows 系统事件日志中的蓝屏记录
（Provider `Microsoft-Windows-WER-SystemErrorReporting`，EventID 1001），
将错误代码翻译为通俗中文解读与排查建议。

不解析 .dmp 转储文件（需集成调试引擎，收益低、复杂度高，明确不做）。

## 2. 界面设计

### 2.1 主窗口

优化项列表新增第五行卡片「蓝屏分析」（无勾选框，不参与一键优化）：

- 左侧：标题「蓝屏分析」+ 说明「检查系统蓝屏记录并给出原因解读」
- 右侧：扫描文案 `BlueScreenText`（"发现 N 次蓝屏" / "未发现蓝屏记录"）+「查看」按钮（AccentButton）
- 点「查看」打开 `BlueScreenWindow`（模态，Owner=主窗口）

### 2.2 蓝屏分析窗口

深色风格与启动项窗口一致，`ScrollViewer + ItemsControl`，每条蓝屏记录一张 Card：

- 第一行：发生时间（粗体） + 错误代码（红色 #EF5350，如 `0x00000034`）
- 第二行：错误名称（如 CACHE_MANAGER，强调青色）
- 第三行：原因解读（灰色小字）
- 第四行：排查建议（灰色小字，"建议：" 前缀）
- 底部（若有）：转储文件路径（灰色小字，TextTrimming）

无记录时窗口中央显示："未发现蓝屏记录，系统运行良好 ✔"

## 3. 模块设计

| 模块 | 职责 |
|---|---|
| `Models/BlueScreenEvent.cs` | 记录：时间、代码值、代码文本、名称、原因、建议、转储路径 |
| `Services/BlueScreenMessageParser.cs` | 纯逻辑：从事件消息文本解析停止代码与转储路径（正则），可单元测试 |
| `Services/StopCodeKnowledge.cs` | 纯逻辑：约 20 种常见停止代码 → (名称/原因/建议)，未知代码返回通用条目，可单元测试 |
| `Services/BlueScreenAnalyzer.cs` | 用 `EventLogQuery` 读取 System 日志 1001 事件，组装 `BlueScreenEvent` 列表（按时间倒序，最多 50 条） |
| `ViewModels/BlueScreenViewModel.cs` | 加载列表 + `HasEvents` 标志 |
| `Views/BlueScreenWindow.xaml(.cs)` | 展示窗口 |

`MainViewModel` 新增 `BlueScreenText`，在 `RefreshScanAsync` 中后台统计次数。

## 4. 知识库覆盖的停止代码

0x0A IRQL_NOT_LESS_OR_EQUAL、0x1A MEMORY_MANAGEMENT、0x1E KMODE_EXCEPTION_NOT_HANDLED、
0x24 NTFS_FILE_SYSTEM、0x34 CACHE_MANAGER、0x3B SYSTEM_SERVICE_EXCEPTION、
0x50 PAGE_FAULT_IN_NONPAGED_AREA、0x7A KERNEL_DATA_INPAGE_ERROR、0x7B INACCESSIBLE_BOOT_DEVICE、
0x7E SYSTEM_THREAD_EXCEPTION_NOT_HANDLED、0x7F UNEXPECTED_KERNEL_MODE_TRAP、
0x9F DRIVER_POWER_STATE_FAILURE、0xC2 BAD_POOL_CALLER、0xD1 DRIVER_IRQL_NOT_LESS_OR_EQUAL、
0xEF CRITICAL_PROCESS_DIED、0xF4 CRITICAL_OBJECT_TERMINATION、0x116 VIDEO_TDR_FAILURE、
0x124 WHEA_UNCORRECTABLE_ERROR、0x133 DPC_WATCHDOG_VIOLATION、0x139 KERNEL_SECURITY_CHECK_FAILURE。

每条含：中文原因（一句话）+ 针对性建议（驱动/内存/磁盘/显卡/硬件等方向）。
未知代码通用建议：更新驱动与系统补丁、运行内存诊断、检查磁盘健康。

## 5. 解析规则

事件 1001 消息示例（中文系统）：
`计算机已经从检测错误后重新启动。检测错误: 0x00000034 (0x…, 0x…, 0x…, 0x…)。已将转储保存在: C:\Windows\MEMORY.DMP。`

- 停止代码：消息中第一个 `0x` + 8 位十六进制（不区分大小写）
- 转储路径：正则匹配 `盘符:\…\*.dmp`（不区分大小写），可缺省
- 解析失败的事件跳过不展示

## 6. 错误处理

- 事件日志读取整体 try-catch，失败时返回空列表，主界面显示"蓝屏记录读取失败"
- 单条事件解析失败仅跳过该条

## 7. 测试计划

- 单元测试：MessageParser（真实中文消息样本 / 无代码文本 / 无转储路径）；StopCodeKnowledge（已知代码 0x34、未知代码回退通用条目）
- 手动验收：本机现有 2 条 0x00000034 记录应正确显示时间、名称 CACHE_MANAGER、原因与建议

## 8. 版本

随本功能升级至 v1.2.0（程序 + 安装包）。
