@echo off
setlocal EnableDelayedExpansion
title Menu Remover
cd /d "%~dp0"

echo ==========================================
echo   PVTX hacks - remove from game
echo ==========================================
echo.

set "GAME="
if defined GorillaTag_GamePath if exist "%GorillaTag_GamePath%\Gorilla Tag.exe" set "GAME=%GorillaTag_GamePath%"
if not defined GAME if exist "C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag\Gorilla Tag.exe" set "GAME=C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag"
if not defined GAME if exist "C:\Program Files\Steam\steamapps\common\Gorilla Tag\Gorilla Tag.exe" set "GAME=C:\Program Files\Steam\steamapps\common\Gorilla Tag"
if not defined GAME if exist "D:\SteamLibrary\steamapps\common\Gorilla Tag\Gorilla Tag.exe" set "GAME=D:\SteamLibrary\steamapps\common\Gorilla Tag"
if not defined GAME if exist "E:\SteamLibrary\steamapps\common\Gorilla Tag\Gorilla Tag.exe" set "GAME=E:\SteamLibrary\steamapps\common\Gorilla Tag"
if not defined GAME if exist "C:\Program Files\Oculus\Software\Software\another-axiom-gorilla-tag\Gorilla Tag.exe" set "GAME=C:\Program Files\Oculus\Software\Software\another-axiom-gorilla-tag"

if not defined GAME (
    set /p "GAME=Could not find the game. Paste your Gorilla Tag folder path: "
)
set "GAME=!GAME:"=!"
if "!GAME:~-1!"=="\" set "GAME=!GAME:~0,-1!"

if not exist "!GAME!\BepInEx\plugins" (
    echo [ERROR] No BepInEx\plugins folder in: !GAME!
    pause
    exit /b 1
)

tasklist /FI "IMAGENAME eq Gorilla Tag.exe" 2>nul | find /I "Gorilla Tag.exe" >nul
if not errorlevel 1 (
    echo [WARNING] Gorilla Tag is running. Close it first or the delete may fail.
    pause
)

set "TARGET=!GAME!\BepInEx\plugins\MonkeMenu.dll"
if exist "!TARGET!" (
    del /F /Q "!TARGET!"
    if exist "!TARGET!" (
        echo [ERROR] Could not delete the DLL. Is the game still open?
        pause
        exit /b 1
    )
    echo Removed MonkeMenu.dll from the game.
) else (
    echo MonkeMenu.dll was not installed - nothing to remove from the game.
)
if exist "!GAME!\BepInEx\plugins\MonkeMenu.pdb" del /F /Q "!GAME!\BepInEx\plugins\MonkeMenu.pdb"

echo.
set /p "CLEAN=Also delete the build folders here (bin and obj)? [y/N]: "
if /I "!CLEAN!"=="y" (
    if exist "MonkeMenu\bin" rmdir /S /Q "MonkeMenu\bin"
    if exist "MonkeMenu\obj" rmdir /S /Q "MonkeMenu\obj"
    echo Build folders deleted.
)

echo.
echo Done. The menu is gone - the game is back to how it was.
pause
exit /b 0
