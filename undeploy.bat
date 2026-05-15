@echo off
:: NoteWidget COM Add-In Uninstall Script
:: Run this script as Administrator

echo ============================================
echo  NoteWidget COM Add-In Uninstall Script
echo ============================================
echo.

:: Check for admin privileges
net session >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: This script must be run as Administrator!
    pause
    exit /b 1
)

set INSTALL_DIR=C:\Program Files (x86)\EKStudio\NoteWidget
set CLSID={EEE896F2-39B1-4D71-8A54-3EFDFB48BB06}

echo Step 1: Closing OneNote if running...
taskkill /im ONENOTE.EXE >nul 2>&1
echo Done.
echo.

echo Step 2: Unregistering COM component...
set REGASM32=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe
set REGASM64=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe
if exist "%REGASM64%" (
    "%REGASM64%" /unregister "%INSTALL_DIR%\NoteWidgetAddIn.dll"
) else (
    echo WARNING: 64-bit RegAsm not found
)
if exist "%REGASM32%" (
    "%REGASM32%" /unregister "%INSTALL_DIR%\NoteWidgetAddIn.dll"
) else (
    echo WARNING: 32-bit RegAsm not found
)
echo Done.
echo.

echo Step 3: Removing OneNote add-in registration...
reg delete "HKCU\SOFTWARE\Microsoft\Office\OneNote\AddIns\NoteWidget.AddIn" /f >nul 2>&1
reg delete "HKCU\SOFTWARE\Microsoft\Office\OneNote\AddIns\NoteWidgetAddIn" /f >nul 2>&1
echo Done.
echo.

echo Step 4: Removing DllSurrogate registration...
reg delete "HKCU\SOFTWARE\Classes\AppID\%CLSID%" /f >nul 2>&1
echo Done.
echo.

echo Step 5: Removing browser emulation setting...
reg delete "HKCU\SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION" /v dllhost.exe /f >nul 2>&1
echo Done.
echo.

echo Step 6: Removing installed files...
if exist "%INSTALL_DIR%" (
    rmdir /s /q "%INSTALL_DIR%"
    echo Removed %INSTALL_DIR%
) else (
    echo Install directory not found, skipping
)
echo Done.
echo.

echo ============================================
echo  Uninstall completed successfully!
echo ============================================
echo.
pause
