[Setup]
AppName=PlastiCAD
AppVersion=0.1.0
DefaultDirName={autopf}\PlastiCAD
DefaultGroupName=PlastiCAD
OutputDir=Installer
OutputBaseFilename=PlastiCAD_Setup_0.1.0
Compression=lzma
SolidCompression=yes
WizardStyle=modern
Uninstallable=yes

SetupIconFile=C:\Users\bj\source\repos\Paksy\PlastiCAD\PlastiCAD.ico

[Files]
Source: "C:\Users\bj\source\repos\Paksy\PlastiCAD\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Users\bj\source\repos\Paksy\PlastiCAD\PlastiCAD.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\PlastiCAD"; Filename: "{app}\PlastiCAD.exe"; IconFilename: "{app}\PlastiCAD.ico"
Name: "{autodesktop}\PlastiCAD"; Filename: "{app}\PlastiCAD.exe"; IconFilename: "{app}\PlastiCAD.ico"

[Run]
Filename: "{app}\PlastiCAD.exe"; Description: "PlastiCAD starten"; Flags: nowait postinstall skipifsilent