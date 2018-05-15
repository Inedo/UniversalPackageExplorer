reg delete HKCU\Software\Inedo\UniversalPackageExplorer /f
reg delete HKCU\Software\Classes\Applications\UniversalPackageExplorer.exe /f
reg delete HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\UniversalPackageExplorer /f
rd /s /q %~dp0
