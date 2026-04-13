@echo off
setlocal

echo ================================================
echo   FinanceFlow - Modo Debug (Visual Studio)
echo ================================================
echo.

set SCRIPT_DIR=%~dp0
cd /d "%SCRIPT_DIR%"

echo A restaurar pacotes e a iniciar em modo Debug...
echo (Para depuracao, abre o .csproj no Visual Studio)
echo.

dotnet run --project FinanceFlow.csproj -c Debug

pause
