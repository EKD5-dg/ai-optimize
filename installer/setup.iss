; AI 鐢佃剳浼樺寲鍔╂墜 瀹夎鍖呰剼鏈?
[Setup]
AppId={{B7E5D3A1-6C2F-4E8A-9D4B-1F2A3C4D5E6F}
AppName=AI 鐢佃剳浼樺寲鍔╂墜
AppVersion=1.3.7
AppPublisher=macan
DefaultDirName={autopf}\AiOptimize
DefaultGroupName=AI 鐢佃剳浼樺寲鍔╂墜
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=AI鐢佃剳浼樺寲鍔╂墜瀹夎绋嬪簭_v1.3.7
Compression=lzma2/max
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
SetupIconFile=..\AiOptimize\app.ico
UninstallDisplayIcon={app}\AiOptimize.exe
UninstallDisplayName=AI 鐢佃剳浼樺寲鍔╂墜

[Tasks]
Name: "desktopicon"; Description: "鍒涘缓妗岄潰蹇嵎鏂瑰紡 (Create desktop shortcut)"

[Files]
Source: "..\publish\AiOptimize.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\AI 鐢佃剳浼樺寲鍔╂墜"; Filename: "{app}\AiOptimize.exe"
Name: "{autodesktop}\AI 鐢佃剳浼樺寲鍔╂墜"; Filename: "{app}\AiOptimize.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\AiOptimize.exe"; Description: "绔嬪嵆杩愯 (Launch now)"; Flags: nowait postinstall skipifsilent shellexec
