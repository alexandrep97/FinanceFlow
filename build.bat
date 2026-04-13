@echo off
setlocal EnableDelayedExpansion

echo ================================================
echo   FinanceFlow - Script de Compilacao e Instalador
echo ================================================
echo.

:: Verificar se o .NET SDK esta instalado
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERRO] .NET SDK nao encontrado.
    echo        Instala o .NET 8.0 SDK em: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

for /f "tokens=*" %%v in ('dotnet --version') do set DOTNET_VER=%%v
echo [OK] .NET SDK versao: %DOTNET_VER%
echo.

:: Diretorio do script
set SCRIPT_DIR=%~dp0
cd /d "%SCRIPT_DIR%"

echo [1/5] A restaurar pacotes NuGet...
dotnet restore FinanceFlow.csproj
if errorlevel 1 (
    echo [ERRO] Falha ao restaurar pacotes NuGet.
    pause
    exit /b 1
)
echo [OK] Pacotes restaurados.
echo.

echo [2/5] A compilar em modo Release...
dotnet build FinanceFlow.csproj -c Release --no-restore
if errorlevel 1 (
    echo [ERRO] Falha na compilacao.
    pause
    exit /b 1
)
echo [OK] Compilacao concluida.
echo.

echo [3/5] A publicar (executavel unico auto-contido)...
if exist "publish\" rmdir /s /q "publish\"

dotnet publish FinanceFlow.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:PublishReadyToRun=true ^
    -o publish\
if errorlevel 1 (
    echo [ERRO] Falha na publicacao.
    pause
    exit /b 1
)
echo [OK] Publicacao concluida.
echo.

echo [4/5] A copiar recursos (wwwroot + icone)...
if not exist "publish\wwwroot\" mkdir "publish\wwwroot\"
xcopy /e /i /y "wwwroot\*" "publish\wwwroot\" >nul
copy /y "app.ico" "publish\app.ico" >nul
if errorlevel 1 (
    echo [AVISO] Falha a copiar recursos - verifique manualmente.
) else (
    echo [OK] Recursos copiados.
)
echo.

echo [5/5] A compilar o instalador InnoSetup...
echo.

:: Procura o InnoSetup em localizacoes comuns
set ISCC=""
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set ISCC="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files\Inno Setup 6\ISCC.exe"       set ISCC="C:\Program Files\Inno Setup 6\ISCC.exe"

if %ISCC%=="" (
    echo [AVISO] InnoSetup 6 nao encontrado.
    echo         O executavel foi publicado em publish\FinanceFlow.exe
    echo         Para criar o instalador:
    echo           1. Instala InnoSetup 6 em: https://jrsoftware.org/isdl.php
    echo           2. Volta a correr este script OU abre setup.iss no Inno Setup Compiler
    echo.
    goto :done
)

if not exist "installer\" mkdir "installer\"
%ISCC% setup.iss
if errorlevel 1 (
    echo [ERRO] Falha ao compilar o instalador.
    pause
    exit /b 1
)
echo [OK] Instalador criado em installer\

:done
echo.
echo ================================================
echo   BUILD CONCLUIDO COM SUCESSO!
echo ================================================
echo.
echo   Executavel:  publish\FinanceFlow.exe
echo   Instalador:  installer\FinanceFlow_Setup_1.0.0.exe  (se InnoSetup instalado)
echo.
echo   Para distribuir SEM instalador:
echo     Copia toda a pasta publish\ (exe + wwwroot\ + app.ico)
echo.
echo   Para distribuir COM instalador:
echo     Usa o ficheiro installer\FinanceFlow_Setup_1.0.0.exe
echo.
pause
