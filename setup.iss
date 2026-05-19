; NoteWidget Inno Setup Installer Script
; Build with: iscc setup.iss

#define MyAppName "NoteWidget"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "EKStudio"
#define MyAppExeName "NoteWidgetAddIn.dll"
#define CLSID "{{EEE896F2-39B1-4D71-8A54-3EFDFB48BB06}"

[Setup]
AppId={#CLSID}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppPublisher}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=NoteWidget-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "default"; MessagesFile: "compiler:Default.isl"

[Messages]
; 安装界面中文化
SetupAppTitle=安装 - {#MyAppName}
SetupWindowTitle=安装 - {#MyAppName} {#MyAppVersion}
WelcomeLabel2=这将安装 {#MyAppName} {#MyAppVersion} 到您的计算机。%n%nOneNote Markdown 增强插件，支持编辑、预览、语法高亮和导出。%n%n建议在继续之前关闭 OneNote。
SelectDirLabel3=安装程序将把 {#MyAppName} 安装到以下文件夹。
SelectDirBrowseLabel=如需安装到其他位置，请点击"浏览"。
ReadyLabel2a=安装程序已准备好将 {#MyAppName} 安装到您的计算机。
InstallingLabel=正在安装，请稍候...
FinishedHeadingLabel=安装完成
FinishedLabelNoIcons={#MyAppName} 已成功安装到您的计算机。%n%n请打开 OneNote，在"开始"选项卡中找到 Markdown 组，即可使用预览和编辑功能。
ExitSetupTitle=退出安装
ExitSetupMessage=安装尚未完成。如果现在退出，程序将不会被安装。%n%n确定要退出安装吗？

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main DLL
Source: "NoteWidgetAddIn\bin\Release\NoteWidgetAddIn.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "NoteWidgetAddIn\bin\Release\NoteWidgetAddIn.dll.config"; DestDir: "{app}"; Flags: ignoreversion

; NuGet dependencies
Source: "NoteWidgetAddIn\bin\Release\Markdig.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "NoteWidgetAddIn\bin\Release\HtmlAgilityPack.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "NoteWidgetAddIn\bin\Release\NLog.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "NoteWidgetAddIn\bin\Release\NLog.config"; DestDir: "{app}"; Flags: ignoreversion

; WebView2
Source: "NoteWidgetAddIn\bin\Release\Microsoft.Web.WebView2.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "NoteWidgetAddIn\bin\Release\Microsoft.Web.WebView2.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "NoteWidgetAddIn\bin\Release\Microsoft.Web.WebView2.Wpf.dll"; DestDir: "{app}"; Flags: ignoreversion

; System dependencies
Source: "NoteWidgetAddIn\bin\Release\System.Buffers.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "NoteWidgetAddIn\bin\Release\System.Memory.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "NoteWidgetAddIn\bin\Release\System.Numerics.Vectors.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "NoteWidgetAddIn\bin\Release\System.Runtime.CompilerServices.Unsafe.dll"; DestDir: "{app}"; Flags: ignoreversion

; WebView2 native loaders
Source: "NoteWidgetAddIn\bin\Release\runtimes\win-x86\native\WebView2Loader.dll"; DestDir: "{app}\runtimes\win-x86\native"; Flags: ignoreversion
Source: "NoteWidgetAddIn\bin\Release\runtimes\win-x64\native\WebView2Loader.dll"; DestDir: "{app}\runtimes\win-x64\native"; Flags: ignoreversion
Source: "NoteWidgetAddIn\bin\Release\runtimes\win-arm64\native\WebView2Loader.dll"; DestDir: "{app}\runtimes\win-arm64\native"; Flags: ignoreversion

; Resource files - CSS
Source: "NoteWidgetAddIn\Resources\css\*"; DestDir: "{app}\Resources\css"; Flags: ignoreversion recursesubdirs

; Resource files - JS
Source: "NoteWidgetAddIn\Resources\js\*.js"; DestDir: "{app}\Resources\js"; Flags: ignoreversion
Source: "NoteWidgetAddIn\Resources\js\*.html"; DestDir: "{app}\Resources\js"; Flags: ignoreversion
Source: "NoteWidgetAddIn\Resources\js\monaco\**"; DestDir: "{app}\Resources\js\monaco"; Flags: ignoreversion recursesubdirs

; Resource files - HTML
Source: "NoteWidgetAddIn\Resources\MarkdownCheatSheet.html"; DestDir: "{app}\Resources"; Flags: ignoreversion

; Icon
Source: "NoteWidgetAddIn\Properties\markdown_icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
; OneNote Add-in registration
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Office\OneNote\AddIns\NoteWidget.AddIn"; ValueType: string; ValueName: "Description"; ValueData: "Markdown enhanced addin for OneNote"; Flags: uninsdeletekey
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Office\OneNote\AddIns\NoteWidget.AddIn"; ValueType: string; ValueName: "FriendlyName"; ValueData: "NoteWidget"; Flags: uninsdeletekey
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Office\OneNote\AddIns\NoteWidget.AddIn"; ValueType: dword; ValueName: "LoadBehavior"; ValueData: "3"; Flags: uninsdeletekey

; COM DllSurrogate
Root: HKCU; Subkey: "SOFTWARE\Classes\AppID\{#CLSID}"; ValueType: string; ValueName: "DllSurrogate"; ValueData: ""; Flags: uninsdeletekey
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{#CLSID}"; ValueType: string; ValueName: "AppID"; ValueData: "{#CLSID}"; Flags: uninsdeletekey

; WebView2 browser emulation
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"; ValueType: dword; ValueName: "dllhost.exe"; ValueData: "11001"; Flags: uninsdeletevalue

[Run]
; Register COM with RegAsm (64-bit for 64-bit OneNote)
Filename: "{win}\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"; Parameters: "/codebase ""{app}\NoteWidgetAddIn.dll"""; StatusMsg: "Registering COM component..."; Flags: runhidden; Check: Is64BitInstallMode
Filename: "{win}\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"; Parameters: "/codebase ""{app}\NoteWidgetAddIn.dll"""; StatusMsg: "Registering COM component..."; Flags: runhidden; Check: "not Is64BitInstallMode"

[UninstallRun]
; Unregister COM
Filename: "{win}\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"; Parameters: "/unregister ""{app}\NoteWidgetAddIn.dll"""; Flags: runhidden; Check: Is64BitInstallMode
Filename: "{win}\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"; Parameters: "/unregister ""{app}\NoteWidgetAddIn.dll"""; Flags: runhidden; Check: "not Is64BitInstallMode"

[Code]
// Kill OneNote before install/uninstall
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  if Exec('taskkill', '/im ONENOTE.EXE', '', SW_HIDE, ewNoWait, ResultCode) then
    Sleep(2000);
end;

function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  if Exec('taskkill', '/im ONENOTE.EXE', '', SW_HIDE, ewNoWait, ResultCode) then
    Sleep(2000);
end;
