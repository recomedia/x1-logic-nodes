@echo off
setlocal enabledelayedexpansion

REM Create output package directory, if not yet present
if not exist out mkdir out

REM Check if SDK is installed in correct location
if NOT exist LogicNodesSDK\LogicNodeTool.exe (
    echo ERROR: Gira LogicNodesSDK not correctly installed.
    exit 1
)

REM Check if Release build output (DLL) for given package is present
set RELEASEOUT=%1Nodes\bin\Release
set RELEASEDLL=%RELEASEOUT%\Recomedia_de.Logic.%1.dll
if NOT exist %RELEASEDLL% (
    echo ERROR: Build output for %1 not present.
    exit 2
)

REM Check if Release build output is newer than Debug build output
set DEBUGOUT=%1Nodes\bin\Debug
set DEBUGDLL=%DEBUGOUT%\Recomedia_de.Logic.%1.dll
if exist %DEBUGDLL% ( REM Release DLL has already been checked above
    REM Both Release and Debug DLLs exists, check which one is newer
    REM The /L simulation mode of xcopy does not copy anything; we only
    REM parse its output to detect whether the Debug DLL would have been
    REM copied, which implies it is newer than the Release DLL.
    for /F "tokens=1" %%I in ('xcopy /D /Y /L "%DEBUGDLL%" "%RELEASEDLL%"') do (
        if "%%I" EQU "1" (
            echo WARNING: Release DLL for %1 is older than Debug DLL.
            echo WARNING: No shippable package created.
            exit 0
        )
    )
)

REM Create package from build output
LogicNodesSDK\LogicNodeTool.exe create %RELEASEOUT% out
if %errorlevel% NEQ 0 (
    echo ERROR: Failed to create package.
    exit 3
)

REM Determine latest package version
for /f %%i in ('dir /b/a-d/od/t:c out\Recomedia_de.Logic.%1-*.zip') do set LASTZIP=%%i
if "%LASTZIP%" EQU "" (
    echo ERROR: No package found to sign.
    exit 4
)

REM Check if developer password exists
if NOT exist Horst_Lehner.pw (
    echo WARNING: Could not sign package, because password file is missing.
    exit 0
)
set /p PASSWORD=<Horst_Lehner.pw
if "%PASSWORD%" EQU "" (
    echo ERROR: Password file Horst_Lehner.pw is empty.
    exit 5
)

REM Sign latest package version, if developer certificate exists
if exist Horst_Lehner.p12 (
    LogicNodesSDK\SignLogicNodes.exe Horst_Lehner.p12 %PASSWORD% out\%LASTZIP%
) else (
    echo WARNING: Could not sign package, because developer certificate is missing.
    exit 0
)
