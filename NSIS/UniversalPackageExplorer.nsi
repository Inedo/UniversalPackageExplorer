!include "FileFunc.nsh"

SetCompressor /SOLID lzma

Name "Universal Package Explorer"
Icon "romp.ico"
OutFile "UPESetup.exe"
BrandingText "Inedo, LLC"
Caption "Extracting installer files..."
SubCaption 3 " "
InstallDir $TEMP\UPESetup
RequestExecutionLevel admin

;SilentInstall silent

Section ""
  ; Set output path to the installation directory.
  SetOutPath $INSTDIR

  SetAutoClose true

  File UniversalPackageExplorer.Setup.exe
  File UniversalPackageExplorer.Setup.exe.config
  File Inedo.Installer.dll
  File Interop.IWshRuntimeLibrary.dll
  File Newtonsoft.Json.dll
  File /r UniversalPackageExplorer

  HideWindow

  ExecWait 'UniversalPackageExplorer.Setup.exe $CMDLINE' $0
  SetErrorLevel $0

  RMDir /r $INSTDIR

SectionEnd