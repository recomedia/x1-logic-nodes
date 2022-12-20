@echo off

REM Create output package directory, if not yet present
if not exist out mkdir out

REM Check if SDK is installed in correct location
if NOT exist LogicNodesSDK\LogicNodeTool.exe (
    echo ERROR: Gira LogicNodesSDK not correctly installed.
    exit 1
)

REM Check if build output for given package is present
set BUILDOUT=%1Nodes\bin\Debug
if NOT exist %BUILDOUT% (
    echo ERROR: Buid output for %1 not present.
    exit 2
)

REM Create package from build output
LogicNodesSDK\LogicNodeTool.exe create %BUILDOUT% out
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
