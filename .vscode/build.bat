@echo off
setlocal

set "PROJECT=.vscode\mod.csproj"

for %%I in ("%~dp0..") do set "REPO_ROOT=%%~fI"
set "MOD_DIR=%REPO_ROOT%\RimworldMilkingMachine"
set "OUTPUT_DIR=%MOD_DIR%\1.6\Assemblies"
for %%I in ("C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods") do set "RIMWORLD_MODS_DIR=%%~fI"
set "TARGET_DIR=%RIMWORLD_MODS_DIR%\RimworldMilkingMachine"
set "LOCK_PATH=%TARGET_DIR%\.rmm_lock"

if not exist "%OUTPUT_DIR%" (
    mkdir "%OUTPUT_DIR%"
)

REM remove previously built assemblies so stale files do not linger
del /q "%OUTPUT_DIR%\*" >nul 2>&1

dotnet build "%PROJECT%" -c Release
if errorlevel 1 (
    exit /b %errorlevel%
)

if not exist "%RIMWORLD_MODS_DIR%" goto :mods_missing

if exist "%LOCK_PATH%" goto :lock_found

robocopy "%MOD_DIR%" "%TARGET_DIR%" /MIR /NFL /NDL /NJH /NJS /NP >nul
set "RC=%ERRORLEVEL%"
if %RC% GEQ 8 (
    echo Robocopy failed with exit code %RC%.
    exit /b %RC%
)

echo Copied mod contents to %TARGET_DIR%
exit /b 0

:mods_missing
echo RimWorld Mods directory not found: %RIMWORLD_MODS_DIR%
exit /b 1

:lock_found
echo RimWorld mod folder appears in use (lock file found: %LOCK_PATH%).
echo Please exit RimWorld and remove the lock file if the game is closed.
exit /b 2
