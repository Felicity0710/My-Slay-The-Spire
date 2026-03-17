@echo off
setlocal

@REM set GODOT_EXE=C:\Path\To\Godot_v4.5.1-stable_mono_win64.exe
set GODOT_EXE=C:\dev\game\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe
set PROJECT_DIR=C:\Users\Administrator\Documents\slay-the-hs
set EXPORT_DIR=%PROJECT_DIR%\Exports\windows-current
set OUTPUT_EXE=%EXPORT_DIR%\slay-the-hs.exe

echo [1/3] Cleaning export directory...
if exist "%EXPORT_DIR%" rmdir /s /q "%EXPORT_DIR%"
mkdir "%EXPORT_DIR%"

echo [2/3] Exporting project...
"%GODOT_EXE%" --headless --path "%PROJECT_DIR%" --export-debug "Windows Desktop" "%OUTPUT_EXE%"

if errorlevel 1 (
    echo Export failed.
    exit /b 1
)

echo [3/3] Done.
echo Output: %OUTPUT_EXE%

endlocal