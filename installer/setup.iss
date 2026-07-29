; AI 电脑优化助手 安装包脚本
[Setup]
AppId={{B7E5D3A1-6C2F-4E8A-9D4B-1F2A3C4D5E6F}
AppName=AI 电脑优化助手
AppVersion=1.3.1
AppPublisher=macan
DefaultDirName={autopf}\AiOptimize
DefaultGroupName=AI 电脑优化助手
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=AI电脑优化助手安装程序_v1.3.1
Compression=lzma2/max
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
SetupIconFile=..\AiOptimize\app.ico
UninstallDisplayIcon={app}\AiOptimize.exe
UninstallDisplayName=AI 电脑优化助手

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式 (Create desktop shortcut)"

[Files]
Source: "..\publish-sc\AiOptimize.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\AI 电脑优化助手"; Filename: "{app}\AiOptimize.exe"
Name: "{autodesktop}\AI 电脑优化助手"; Filename: "{app}\AiOptimize.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\AiOptimize.exe"; Description: "立即运行 (Launch now)"; Flags: nowait postinstall skipifsilent shellexec
