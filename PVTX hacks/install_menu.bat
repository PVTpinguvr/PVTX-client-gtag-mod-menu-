@echo off
setlocal EnableDelayedExpansion
title MonkeMenu Installer
cd /d "%~dp0"

echo ==========================================
echo   MonkeMenu - build and install
echo ==========================================
echo.

rem ---------- 1. Find the Gorilla Tag folder ----------
set "GAME="
if defined GorillaTag_GamePath if exist "%GorillaTag_GamePath%\Gorilla Tag.exe" set "GAME=%GorillaTag_GamePath%"
if not defined GAME if exist "C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag\Gorilla Tag.exe" set "GAME=C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag"
if not defined GAME if exist "C:\Program Files\Steam\steamapps\common\Gorilla Tag\Gorilla Tag.exe" set "GAME=C:\Program Files\Steam\steamapps\common\Gorilla Tag"
if not defined GAME if exist "D:\SteamLibrary\steamapps\common\Gorilla Tag\Gorilla Tag.exe" set "GAME=D:\SteamLibrary\steamapps\common\Gorilla Tag"
if not defined GAME if exist "E:\SteamLibrary\steamapps\common\Gorilla Tag\Gorilla Tag.exe" set "GAME=E:\SteamLibrary\steamapps\common\Gorilla Tag"
if not defined GAME if exist "C:\Program Files\Oculus\Software\Software\another-axiom-gorilla-tag\Gorilla Tag.exe" set "GAME=C:\Program Files\Oculus\Software\Software\another-axiom-gorilla-tag"

if not defined GAME (
    echo Could not find Gorilla Tag automatically.
    echo Open Steam, right click Gorilla Tag, Manage, Browse local files,
    echo then copy the folder path from the address bar.
    echo.
    set /p "GAME=Paste your Gorilla Tag folder path: "
)

set "GAME=!GAME:"=!"
if "!GAME:~-1!"=="\" set "GAME=!GAME:~0,-1!"

if not exist "!GAME!\Gorilla Tag.exe" (
    echo.
    echo [ERROR] "Gorilla Tag.exe" was not found in:
    echo     !GAME!
    goto :fail
)
echo Game folder: !GAME!

rem ---------- 2. Check BepInEx ----------
if not exist "!GAME!\BepInEx\core\BepInEx.dll" (
    echo.
    echo [ERROR] BepInEx is not installed in that folder.
    echo Install BepInEx 5 x64 into the game folder, launch Gorilla Tag once, then run this again.
    goto :fail
)
if not exist "!GAME!\BepInEx\plugins" mkdir "!GAME!\BepInEx\plugins"

rem ---------- 3. Check .NET SDK (the runtime alone is NOT enough) ----------
set "HAVESDK="
where dotnet >nul 2>&1
if not errorlevel 1 (
    dotnet --list-sdks 2>nul | findstr /R "[0-9]" >nul
    if not errorlevel 1 set "HAVESDK=1"
)
if not defined HAVESDK (
    echo.
    echo [PROBLEM] The .NET SDK is not installed. ^(You may have only the runtime, which can not build.^)
    echo.
    where winget >nul 2>&1
    if errorlevel 1 (
        echo Download the SDK ^(version 8 or newer^) from https://dotnet.microsoft.com/download
        echo Install it, then run this file again.
        goto :fail
    )
    set /p "INST=Install the .NET 8 SDK now with winget? [Y/n]: "
    if /I "!INST!"=="n" (
        echo Download it from https://dotnet.microsoft.com/download then run this again.
        goto :fail
    )
    winget install --id Microsoft.DotNet.SDK.8 -e --accept-source-agreements --accept-package-agreements
    echo.
    echo If the install finished, CLOSE this window and double click install_menu.bat again
    echo ^(Windows needs a fresh window to see the new SDK^).
    goto :fail
)

rem ---------- 4. Game must be closed ----------
tasklist /FI "IMAGENAME eq Gorilla Tag.exe" 2>nul | find /I "Gorilla Tag.exe" >nul
if not errorlevel 1 (
    echo.
    echo [WARNING] Gorilla Tag is running. Close it first or the copy may fail.
    pause
)

rem ---------- 5. Build ----------
set "GorillaTag_GamePath=!GAME!"
set "DLL=MonkeMenu\bin\Release\netstandard2.1\MonkeMenu.dll"
if exist "!DLL!" del /F /Q "!DLL!"
echo.
echo Building MonkeMenu ^(first run needs internet to fetch build tools^)...
dotnet build "MonkeMenu\MonkeMenu.csproj" -c Release --nologo -v minimal
if not exist "!DLL!" (
    echo.
    echo [ERROR] Build failed - no DLL was produced. Scroll up for the error lines
    echo ^(build errors contain "error CS" or "error MSB"^). Paste them to Claude to get them fixed.
    goto :fail
)

rem ---------- 6. Install ----------
copy /Y "!DLL!" "!GAME!\BepInEx\plugins\MonkeMenu.dll" >nul
if errorlevel 1 (
    echo [ERROR] Could not copy the DLL into the plugins folder.
    goto :fail
)

rem UI menu sounds (Wii home open + click)
if not exist "!GAME!\BepInEx\plugins\MonkeMenuSounds" mkdir "!GAME!\BepInEx\plugins\MonkeMenuSounds"
if exist "MonkeMenu\Sounds\menu_open.wav"  copy /Y "MonkeMenu\Sounds\menu_open.wav"  "!GAME!\BepInEx\plugins\MonkeMenuSounds\menu_open.wav"  >nul
if exist "MonkeMenu\Sounds\menu_click.wav" copy /Y "MonkeMenu\Sounds\menu_click.wav" "!GAME!\BepInEx\plugins\MonkeMenuSounds\menu_click.wav" >nul
rem also pick them up from the build output folder if present
if exist "MonkeMenu\bin\Release\netstandard2.1\menu_open.wav"  copy /Y "MonkeMenu\bin\Release\netstandard2.1\menu_open.wav"  "!GAME!\BepInEx\plugins\MonkeMenuSounds\menu_open.wav"  >nul
if exist "MonkeMenu\bin\Release\netstandard2.1\menu_click.wav" copy /Y "MonkeMenu\bin\Release\netstandard2.1\menu_click.wav" "!GAME!\BepInEx\plugins\MonkeMenuSounds\menu_click.wav" >nul

echo.
echo ==========================================
echo   DONE - installed to:
echo   !GAME!\BepInEx\plugins\MonkeMenu.dll
echo   !GAME!\BepInEx\plugins\MonkeMenuSounds\  (menu open + click sounds)
echo ==========================================
echo In game: press X + Y together to open the menu.
echo.
pause
exit /b 0

:fail
echo.
echo Install did not finish.
pause
exit /b 1
