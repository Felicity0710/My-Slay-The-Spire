@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_DIR=%%~fI"
set "BUILD_SCRIPT=%PROJECT_DIR%\build.ps1"
set "EXPORT_PRESETS=%PROJECT_DIR%\export_presets.cfg"
set "EXPORT_NAME=Windows Desktop"

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

if not exist "%GODOT_EXE%" (
    echo Godot executable does not exist: %GODOT_EXE%
    exit /b 1
)

if not exist "%BUILD_SCRIPT%" (
    echo Build script not found: %BUILD_SCRIPT%
    exit /b 1
)

if not exist "%EXPORT_PRESETS%" (
    echo Export preset file not found: %EXPORT_PRESETS%
    exit /b 1
)

findstr /c:"name=\"%EXPORT_NAME%\"" "%EXPORT_PRESETS%" >nul
if errorlevel 1 (
    echo Export preset "%EXPORT_NAME%" was not found in export_presets.cfg.
    exit /b 1
)

set "EXPORT_DIR=%PROJECT_DIR%\Exports\windows-current"
set "OUTPUT_EXE=%EXPORT_DIR%\slay-the-hs.exe"

if /i not "%SLAY_THE_HS_SKIP_BUILD%"=="1" (
    echo [1/4] Building project...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%"
    if errorlevel 1 (
        echo Build failed.
        exit /b 1
    )
) else (
    echo [1/4] Skipping build because SLAY_THE_HS_SKIP_BUILD=1.
)

echo [2/4] Cleaning export directory...
if exist "%EXPORT_DIR%" rmdir /s /q "%EXPORT_DIR%"
mkdir "%EXPORT_DIR%"
if errorlevel 1 (
    echo Failed to prepare export directory: %EXPORT_DIR%
    exit /b 1
)

echo [3/4] Exporting project...
"%GODOT_EXE%" --headless --path "%PROJECT_DIR%" --export-debug "%EXPORT_NAME%" "%OUTPUT_EXE%"

if errorlevel 1 (
    echo Export failed.
    exit /b 1
)

if not exist "%OUTPUT_EXE%" (
    echo Export reported success but output file is missing: %OUTPUT_EXE%
    exit /b 1
)

echo [4/4] Done.
echo Output: %OUTPUT_EXE%

endlocal
