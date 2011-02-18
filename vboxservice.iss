;#pragma option -v+
;#pragma verboselevel 9

#define SrcPath "d:\temp\vb4service2\bin\Debug"
#define AppVer GetFileVersion(AddBackslash(SrcPath)+"\VBoxService.exe")
#define CopyRight GetFileCopyright(AddBackslash(SrcPath)+"\VBoxService.exe")
#define Company GetFileCompany(AddBackslash(SrcPath)+"\VBoxService.exe")
#define FileDesc GetFileDescription(AddBackslash(SrcPath)+"\VBoxService.exe")

[Files]
Source: {#SrcPath}\VBoxService.exe; DestDir: {app}
Source: {#SrcPath}\VirtualBox.dll; DestDir: {app}
Source: {#SrcPath}\LICENSE.txt; DestDir: {app}
[Setup]
DefaultDirName={pf}\VirtualBoxServerService
AllowUNCPath=false
UsePreviousGroup=false
AppendDefaultGroupName=false
VersionInfoVersion={#AppVer}
AppVersion={#AppVer}
AppName=Virtualbox Server Service
VersionInfoTextVersion={#AppVer}
VersionInfoProductVersion={#AppVer}
AppVerName=Virtualbox Server Service ({#AppVer})
OutputDir=d:\temp\vb4service2
SourceDir={#SrcPath}
OutputBaseFilename=VBoxService-setup_{#AppVer}
VersionInfoCopyright={#CopyRight}
AppCopyright={#CopyRight}
VersionInfoCompany={#Company}
VersionInfoDescription={#FileDesc}
WizardImageFile=C:\Program Files\Inno Setup 5\WizModernImage-IS.bmp
WizardSmallImageFile=C:\Program Files\Inno Setup 5\WizModernSmallImage-IS.bmp
DisableProgramGroupPage=true
[Run]
Filename: {app}\VBoxService.exe; Parameters: -install; WorkingDir: {app}; StatusMsg: Installing service; Flags: runhidden
Filename: {sys}\schtasks.exe; Parameters: "/create /tn ""Virtualbox Service Server"" /tr ""\""{app}\VBoxService.exe\"" -tray"" /sc onlogon /rl highest /delay 0000:30"; WorkingDir: {sys}; StatusMsg: Creating scheduled task; MinVersion: 0,6.0.6000; Flags: runhidden
[UninstallRun]
Filename: {app}\VBoxService.exe; Parameters: -uninstall; WorkingDir: {app}; Flags: runhidden
Filename: {sys}\schtasks.exe; Parameters: "/tn ""Virtualbox Service Server"""; WorkingDir: {sys}; MinVersion: 0,6.0.6000; Flags: runhidden
[Icons]
Name: {userstartup}\VirtualBox Server Service Trayicon; Filename: {app}\VBoxService.exe; Parameters: -tray; WorkingDir: {app}; IconFilename: {app}\VBoxService.exe; Comment: Virtualbox Server Service Trayicon; Flags: runminimized; OnlyBelowVersion: 0,6.0.6000
[Code]
function InitializeSetup(): Boolean;

begin
	Result:=true;
	if (not RegKeyExists(HKLM, 'Software\Microsoft\.NETFramework\policy\v4.0')) then
	begin
		MsgBox('Application needs the Microsoft .NET Framework 4.0 to be installed by an Administrator', mbInformation, MB_OK);
		Result:=false;
	end
end;
