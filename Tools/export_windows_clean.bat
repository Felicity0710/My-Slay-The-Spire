@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_DIR=%%~fI"

if not "%~1"=="" set "GODOT_EXE=%~1"
if not defined GODOT_EXE set "GODOT_EXE=%SLAY_THE_HS_GODOT_EXE%"

if not defined GODOT_EXE (
    for %%N in (godot4-mono.exe godot4.exe godot.exe) do (
        for /f "delims=" %%I in ('where %%N 2^>nul') do (
            if not defined GODOT_EXE set "GODOT_EXE=%%I"
        )
    )
)

if not defined GODOT_EXE (
    echo Could not find Godot in PATH.
    echo Pass the executable as the first argument or set SLAY_THE_HS_GODOT_EXE.
    exit /b 1
)

set "EXPORT_DIR=%PROJECT_DIR%\Exports\windows-current"
set "OUTPUT_EXE=%EXPORT_DIR%\slay-the-hs.exe"

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
