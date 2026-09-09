@echo off
rem 步骤记录器 · 一键构建（Windows）
rem 需要：.NET 10 SDK (https://dotnet.microsoft.com/download/dotnet/10.0)

setlocal
set ROOT=%~dp0
pushd "%ROOT%src\BetterStepsRecorder"

dotnet restore
if errorlevel 1 goto :err

dotnet build -c Release
if errorlevel 1 goto :err

rem 单文件发布
if not exist "%ROOT%dist" mkdir "%ROOT%dist"
dotnet publish -c Release -r win-x64 --self-contained false ^
   -p:PublishSingleFile=true ^
   -p:IncludeNativeLibrariesForSelfExtract=true ^
   -o "%ROOT%dist"
if errorlevel 1 goto :err

echo.
echo === 构建完成 ===
echo 可执行文件: %ROOT%dist\StepRecorder.exe
popd
endlocal
exit /b 0

:err
echo 构建失败，请检查上方日志。
popd
endlocal
exit /b 1
