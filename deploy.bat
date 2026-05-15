@echo off
:: NoteWidget COM Add-In Deployment Script for OneNote 2021
:: Run this script as Administrator
:: Usage: Right-click -> Run as Administrator

echo ============================================
echo  NoteWidget COM Add-In Deployment Script
echo ============================================
echo.

:: Check for admin privileges
net session >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click this file and select "Run as administrator"
    pause
    exit /b 1
)

:: Configuration
set INSTALL_DIR=C:\Program Files (x86)\EKStudio\NoteWidget
set SOURCE_DIR=%~dp0NoteWidgetAddIn\bin\Release
set CLSID={EEE896F2-39B1-4D71-8A54-3EFDFB48BB06}

echo Step 1: Closing OneNote if running...
taskkill /im ONENOTE.EXE >nul 2>&1
echo Done.
echo.

echo Step 2: Creating install directory...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
if not exist "%INSTALL_DIR%\Resources\css" mkdir "%INSTALL_DIR%\Resources\css"
if not exist "%INSTALL_DIR%\Resources\js" mkdir "%INSTALL_DIR%\Resources\js"
if not exist "%INSTALL_DIR%\runtimes\win-x86\native" mkdir "%INSTALL_DIR%\runtimes\win-x86\native"
if not exist "%INSTALL_DIR%\runtimes\win-x64\native" mkdir "%INSTALL_DIR%\runtimes\win-x64\native"
if not exist "%INSTALL_DIR%\runtimes\win-arm64\native" mkdir "%INSTALL_DIR%\runtimes\win-arm64\native"
echo Done.
echo.

echo Step 3: Copying files...
:: Main DLL and dependencies
copy /Y "%SOURCE_DIR%\NoteWidgetAddIn.dll" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE_DIR%\NoteWidgetAddIn.dll.config" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE_DIR%\Markdig.dll" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE_DIR%\HtmlAgilityPack.dll" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE_DIR%\NLog.dll" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE_DIR%\NLog.config" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE_DIR%\Microsoft.Web.WebView2.Core.dll" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE_DIR%\Microsoft.Web.WebView2.WinForms.dll" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE_DIR%\Microsoft.Web.WebView2.Wpf.dll" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE_DIR%\System.Buffers.dll" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE_DIR%\System.Memory.dll" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE_DIR%\System.Numerics.Vectors.dll" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE_DIR%\System.Runtime.CompilerServices.Unsafe.dll" "%INSTALL_DIR%\" >nul

:: WebView2 native loaders
copy /Y "%SOURCE_DIR%\runtimes\win-x86\native\WebView2Loader.dll" "%INSTALL_DIR%\runtimes\win-x86\native\" >nul
copy /Y "%SOURCE_DIR%\runtimes\win-x64\native\WebView2Loader.dll" "%INSTALL_DIR%\runtimes\win-x64\native\" >nul
copy /Y "%SOURCE_DIR%\runtimes\win-arm64\native\WebView2Loader.dll" "%INSTALL_DIR%\runtimes\win-arm64\native\" >nul

:: Resource files (CSS, JS, HTML)
copy /Y "%~dp0NoteWidgetAddIn\Resources\css\*.*" "%INSTALL_DIR%\Resources\css\" >nul
copy /Y "%~dp0NoteWidgetAddIn\Resources\js\*.*" "%INSTALL_DIR%\Resources\js\" >nul
copy /Y "%~dp0NoteWidgetAddIn\Resources\MarkdownCheatSheet.html" "%INSTALL_DIR%\Resources\" >nul

:: Icon
copy /Y "%~dp0NoteWidgetAddIn\Properties\markdown_icon.ico" "%INSTALL_DIR%\" >nul 2>&1
echo Done.
echo.

echo Step 4: Registering COM component with RegAsm...
:: OneNote 2021 is 64-bit, so we need Framework64\RegAsm.exe
set REGASM32=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe
set REGASM64=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe

:: Unregister both 32-bit and 64-bit first to clean up any old registration
"%REGASM32%" /unregister "%INSTALL_DIR%\NoteWidgetAddIn.dll" >nul 2>&1
"%REGASM64%" /unregister "%INSTALL_DIR%\NoteWidgetAddIn.dll" >nul 2>&1

:: Register with 64-bit RegAsm for 64-bit OneNote
if exist "%REGASM64%" (
    echo Using 64-bit RegAsm for 64-bit OneNote...
    "%REGASM64%" /codebase "%INSTALL_DIR%\NoteWidgetAddIn.dll"
) else (
    echo WARNING: 64-bit RegAsm not found, falling back to 32-bit...
    "%REGASM32%" /codebase "%INSTALL_DIR%\NoteWidgetAddIn.dll"
)
if %ERRORLEVEL% neq 0 (
    echo ERROR: RegAsm registration failed!
    pause
    exit /b 1
)
echo Done.
echo.

echo Step 5: Registering OneNote add-in...
:: Remove old wrong ProgId key and create correct one matching RegAsm's ProgId
reg delete "HKCU\SOFTWARE\Microsoft\Office\OneNote\AddIns\NoteWidgetAddIn" /f >nul 2>&1
reg add "HKCU\SOFTWARE\Microsoft\Office\OneNote\AddIns\NoteWidget.AddIn" /v Description /t REG_SZ /d "Widget addin for OneNote, providing markdown and other features" /f >nul
reg add "HKCU\SOFTWARE\Microsoft\Office\OneNote\AddIns\NoteWidget.AddIn" /v FriendlyName /t REG_SZ /d "NoteWidgetAddIn" /f >nul
reg add "HKCU\SOFTWARE\Microsoft\Office\OneNote\AddIns\NoteWidget.AddIn" /v LoadBehavior /t REG_DWORD /d 3 /f >nul
echo Done.
echo.

echo Step 6: Setting up DllSurrogate for COM activation...
reg add "HKCU\SOFTWARE\Classes\AppID\%CLSID%" /v DllSurrogate /t REG_SZ /d "" /f >nul
reg add "HKCU\SOFTWARE\Classes\CLSID\%CLSID%" /v AppID /t REG_SZ /d "%CLSID%" /f >nul
echo Done.
echo.

echo Step 7: Setting browser emulation for WebView2...
reg add "HKCU\SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION" /v dllhost.exe /t REG_DWORD /d 11001 /f >nul
echo Done.
echo.

echo ============================================
echo  Deployment completed successfully!
echo ============================================
echo.
echo Installed to: %INSTALL_DIR%
echo.
echo Next steps:
echo   1. Open OneNote 2021
echo   2. Look for the "Markdown" group on the Home tab
echo   3. Click "Preview" to test the Markdown preview feature
echo.
echo To uninstall, run: undeploy.bat
echo.
pause
