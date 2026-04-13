; ============================================================================
;  FinanceFlow — Script de Instalação (InnoSetup 6.x)
;  Para compilar:
;    1. Instala InnoSetup 6: https://jrsoftware.org/isdl.php
;    2. Corre primeiro: build.bat  (gera a pasta publish\)
;    3. Abre este ficheiro no Inno Setup Compiler e clica Build
;       OU executa: "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup.iss
; ============================================================================

#define AppName        "FinanceFlow"
#define AppVersion     "1.0.0"
#define AppPublisher   "FinanceFlow"
#define AppURL         "https://github.com/alexandrep97/financeflow"
#define AppExeName     "FinanceFlow.exe"
#define AppDescription "Gestão Financeira Pessoal"
#define AppCopyright   "Copyright © 2026 Alexandre"

; Pasta de origem: resultado do dotnet publish (gerado pelo build.bat)
#define SourceDir      "publish"

[Setup]
; ── Identificadores únicos ──────────────────────────────────────────────────
; NOTA: Gera um novo GUID para cada aplicação diferente.
; Para regenerar: Tools > Generate GUID no Inno Setup Compiler
AppId={{F3A2B1C4-9D8E-4F7A-B6C5-2A1D3E4F5B6C}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
AppPublisher={#AppPublisher}
AppCopyright={#AppCopyright}
AppComments={#AppDescription}

; ── Instalação ──────────────────────────────────────────────────────────────
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
; Não requer administrador (instala em %LOCALAPPDATA% se não for admin)
PrivilegesRequiredOverridesAllowed=dialog commandline
PrivilegesRequired=lowest

; ── Desinstalação ───────────────────────────────────────────────────────────
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
; Pergunta se quer manter os dados ao desinstalar
; (tratado na secção [UninstallRun] abaixo)

; ── Output ──────────────────────────────────────────────────────────────────
OutputDir=installer
OutputBaseFilename=FinanceFlow_Setup_{#AppVersion}
SetupIconFile=app.ico
; Compressão máxima
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; ── Aspeto ──────────────────────────────────────────────────────────────────
WizardStyle=modern
WizardResizable=no
; Imagem lateral do wizard (164x314 px — opcional, remove a linha se não tiveres)
; WizardImageFile=assets\wizard_sidebar.bmp
; WizardSmallImageFile=assets\wizard_icon.bmp

; ── Plataforma ──────────────────────────────────────────────────────────────
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

; ── Versão mínima do Windows ────────────────────────────────────────────────
; Windows 10 1903 (build 18362) — requisito do WebView2
MinVersion=10.0.18362

; ── Ficheiro de licença (opcional) ─────────────────────────────────────────
; LicenseFile=LICENSE.txt

; ── Restart ─────────────────────────────────────────────────────────────────
RestartIfNeededByRun=no

[Languages]
Name: "pt"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
pt.WelcomeLabel2=Este assistente vai instalar o [name/ver] no teu computador.%n%nO {#AppName} é uma aplicação de gestão financeira pessoal.%n%nFecha outras aplicações antes de continuar.
en.WelcomeLabel2=This wizard will install [name/ver] on your computer.%n%n{#AppName} is a personal finance management application.%n%nClose other applications before continuing.

pt.FinishLabel=O {#AppName} foi instalado com sucesso!%n%nClica em Concluir para fechar o instalador.
en.FinishLabel={#AppName} has been installed successfully!%n%nClick Finish to close the installer.

pt.WebView2Missing=O Microsoft Edge WebView2 Runtime não foi encontrado.%n%nO {#AppName} requer o WebView2 para funcionar.%n%nDeseja instalar o WebView2 agora? (requer ligação à Internet)
en.WebView2Missing=Microsoft Edge WebView2 Runtime was not found.%n%n{#AppName} requires WebView2 to run.%n%nDo you want to install WebView2 now? (requires Internet connection)

[Tasks]
Name: "desktopicon";    Description: "{cm:CreateDesktopIcon}";         GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunch";   Description: "{cm:CreateQuickLaunchIcon}";     GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1
Name: "startupicon";   Description: "Iniciar automaticamente com o Windows"; GroupDescription: "Arranque:"; Flags: unchecked

[Dirs]
; Pasta de dados do utilizador — persiste entre actualizações
Name: "{userappdata}\{#AppName}"; Flags: uninsneveruninstall

[Files]
; ── Executável principal ─────────────────────────────────────────────────────
Source: "{#SourceDir}\{#AppExeName}";   DestDir: "{app}";             Flags: ignoreversion

; ── Ícone (para usar nos atalhos e no "Remover programas") ──────────────────
Source: "{#SourceDir}\app.ico";          DestDir: "{app}";             Flags: ignoreversion

; ── Interface web ────────────────────────────────────────────────────────────
Source: "{#SourceDir}\wwwroot\*";        DestDir: "{app}\wwwroot";     Flags: ignoreversion recursesubdirs createallsubdirs

; ── WebView2 Runtime bootstrapper (download & install silenciosamente) ───────
; Descomenta se quiseres incluir o bootstrapper (~2 MB) diretamente no instalador:
; Source: "assets\MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
; ── Menu Iniciar ────────────────────────────────────────────────────────────
Name: "{group}\{#AppName}";              Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\app.ico"; Comment: "{#AppDescription}"
Name: "{group}\Desinstalar {#AppName}";  Filename: "{uninstallexe}";      IconFilename: "{app}\app.ico"

; ── Ambiente de trabalho ────────────────────────────────────────────────────
Name: "{autodesktop}\{#AppName}";        Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\app.ico"; Comment: "{#AppDescription}"; Tasks: desktopicon

; ── Quick Launch (Windows XP/Vista/7) ───────────────────────────────────────
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: quicklaunch; IconFilename: "{app}\app.ico"

[Registry]
; ── Arranque automático com o Windows ────────────────────────────────────────
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

; ── Registo da aplicação (para "Abrir com" e associações) ────────────────────
Root: HKCU; Subkey: "Software\{#AppPublisher}\{#AppName}";              Flags: uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\{#AppPublisher}\{#AppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\{#AppPublisher}\{#AppName}"; ValueType: string; ValueName: "Version";     ValueData: "{#AppVersion}"; Flags: uninsdeletevalue

[Run]
; ── Verificar e instalar WebView2 se necessário ──────────────────────────────
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; \
    StatusMsg: "A instalar Microsoft Edge WebView2…"; \
    Check: WebView2Missing; \
    Flags: shellexec waituntilterminated skipifdoesntexist

; ── Lançar a aplicação no fim ────────────────────────────────────────────────
Filename: "{app}\{#AppExeName}"; \
    Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; \
    Flags: nowait postinstall skipifsilent

[UninstallRun]
; Nada extra — os dados do utilizador ficam em {userappdata}\FinanceFlow e nunca são apagados

[Code]
// ============================================================================
//  Funções de suporte
// ============================================================================

// Verifica se o WebView2 Runtime está instalado
function WebView2Missing: Boolean;
var
  Version: String;
begin
  // Verifica chave de registo do WebView2 Runtime (máquina ou utilizador)
  if RegQueryStringValue(HKLM,
    'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}',
    'pv', Version) then
  begin
    Result := (Version = '') or (Version = '0.0.0.0');
    Exit;
  end;
  if RegQueryStringValue(HKCU,
    'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}',
    'pv', Version) then
  begin
    Result := (Version = '') or (Version = '0.0.0.0');
    Exit;
  end;
  // Também verifica o canal estável (máquinas com Edge instalado)
  if RegQueryStringValue(HKLM,
    'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}',
    'pv', Version) then
  begin
    Result := (Version = '') or (Version = '0.0.0.0');
    Exit;
  end;
  // Não encontrou nenhuma chave — provavelmente não está instalado
  Result := True;
end;

// Descarrega e instala o WebView2 bootstrapper via Internet
procedure DownloadAndInstallWebView2;
var
  BootstrapperURL: String;
  BootstrapperPath: String;
  ResultCode: Integer;
begin
  BootstrapperURL  := 'https://go.microsoft.com/fwlink/p/?LinkId=2124703';
  BootstrapperPath := ExpandConstant('{tmp}\MicrosoftEdgeWebview2Setup.exe');

  if not FileExists(BootstrapperPath) then
  begin
    if MsgBox(ExpandConstant('{cm:WebView2Missing}'), mbConfirmation, MB_YESNO) = IDYES then
    begin
      WizardForm.StatusLabel.Caption := 'A descarregar Microsoft Edge WebView2…';
      if not DownloadTemporaryFile(BootstrapperURL, 'MicrosoftEdgeWebview2Setup.exe', '', nil) = 0 then
      begin
        MsgBox('Não foi possível descarregar o WebView2. Instala manualmente em: https://developer.microsoft.com/microsoft-edge/webview2/', mbError, MB_OK);
        Exit;
      end;
    end;
  end;

  if FileExists(BootstrapperPath) then
    Exec(BootstrapperPath, '/silent /install', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

// Executado ao iniciar o instalador
function InitializeSetup: Boolean;
begin
  Result := True;
  if WebView2Missing then
    DownloadAndInstallWebView2;
end;

// Migração de dados: copia a BD se existir numa instalação anterior
// na pasta da aplicação (versões antigas guardavam aqui)
procedure CurStepChanged(CurStep: TSetupStep);
var
  OldDB, NewDB: String;
begin
  if CurStep = ssPostInstall then
  begin
    OldDB := ExpandConstant('{app}\financeflow.db');
    NewDB := ExpandConstant('{userappdata}\{#AppName}\financeflow.db');
    // Se a BD já está na pasta de dados, não faz nada
    if FileExists(NewDB) then Exit;
    // Se existe uma BD na pasta de instalação (versão antiga), copia-a
    if FileExists(OldDB) then
      FileCopy(OldDB, NewDB, False);
  end;
end;
