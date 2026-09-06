@echo off
chcp 65001 >nul
setlocal EnableExtensions DisableDelayedExpansion

REM 将已编译模组复制到本机 RimWorld Mods 目录。
set "DEPLOY_EXE=E:\aow\ModPack\xmlremove\bin\Release\net8.0-windows\xmlremove.exe"
set "SOURCE_MOD=%~dp0HiveLord"
set "TARGET_NAME=HiveLord"

if not exist "%DEPLOY_EXE%" (
    echo [Deploy] 部署工具不存在 / Deployment tool not found: %DEPLOY_EXE%
    exit /b 1
)

if not exist "%SOURCE_MOD%\About\About.xml" (
    echo [Deploy] 源目录缺少 About.xml / About.xml missing from source: %SOURCE_MOD%
    exit /b 1
)

"%DEPLOY_EXE%" deploy --source "%SOURCE_MOD%" --target-name "%TARGET_NAME%"
set "DEPLOY_ERROR=%ERRORLEVEL%"
if not "%DEPLOY_ERROR%"=="0" (
    echo [Deploy] 部署失败，退出码 / Failed, exit code: %DEPLOY_ERROR%
    exit /b %DEPLOY_ERROR%
)

echo [Deploy] 部署完成 / Deployment completed: %TARGET_NAME%
exit /b 0
