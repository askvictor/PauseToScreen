OutFile "PauseToScreenInstaller.exe"
RequestExecutionLevel user
#install to user's programs directory
InstallDir $LOCALAPPDATA\Programs\PauseToScreen
Icon PauseToScreen\PauseToScreen.ico
!define UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\PauseToScreen"
!define EXE_NAME "PauseToScreen.exe"
LicenseData "LICENSE.md"
page license
page instfiles

Section
    # define output path
    SetOutPath $INSTDIR
     
    # specify files to go in output path
    File PauseToScreen\bin\publish\*
     
    CreateShortcut "$SMSTARTUP\PauseToScreen.lnk" "$INSTDIR\${EXE_NAME}" 
    CreateShortcut "$SMPROGRAMS\PauseToScreen.lnk" "$INSTDIR\${EXE_NAME}" 
    
    SetRegView 64
    WriteRegStr HKCU "${UNINST_KEY}" "DisplayName" "PauseToScreen"
    WriteRegStr HKCU "${UNINST_KEY}" "UninstallString"  "$\"$INSTDIR\uninstall.exe$\"" 
 
    WriteUninstaller $INSTDIR\uninstall.exe

    Exec $INSTDIR\${EXE_NAME}
    MessageBox MB_OK "PauseToScreen is now installed and running. To use, press Ctrl-Shift-P or the orange button in the system tray"
SectionEnd
 
Section "Uninstall"
    SetRegView 64
    DeleteRegKey HKCU "${UNINST_KEY}"
    #kill existing process
    ExecWait "taskkill /im ${EXE_NAME} /f"
    sleep 5000
    # Always delete uninstaller first
    Delete $INSTDIR\uninstall.exe
     
    Delete "$SMSTARTUP\PauseToScreen.lnk"
     
    # Delete the directory recursive (TODO - probably shouldn't do this)
    RMDir /r /rebootok $INSTDIR
SectionEnd
