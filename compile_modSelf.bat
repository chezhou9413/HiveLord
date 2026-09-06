@echo off
chcp 65001 >nul
setlocal EnableExtensions DisableDelayedExpansion

REM 编译霸王虫运行程序集。
set "CONFIGURATION=%~1"
if "%CONFIGURATION%"=="" set "CONFIGURATION=Debug"
set "PROJECT=%~dp0HiveLord\1.6\Source\HiveLordLib\HiveLordLib.csproj"

echo [Build] 编译 / Building: %PROJECT%
msbuild "%PROJECT%" /p:Configuration=%CONFIGURATION% /p:Platform="AnyCPU"
set "BUILD_ERROR=%ERRORLEVEL%"
if not "%BUILD_ERROR%"=="0" (
    echo [Build] 编译失败，退出码 / Failed, exit code: %BUILD_ERROR%
    exit /b %BUILD_ERROR%
)

echo [Build] 编译完成 / Build completed.
exit /b 0
