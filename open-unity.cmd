@echo off
REM Opens the Unity project at the correct path.
REM
REM The Unity project is the "unity" subfolder, not this repo root. Pointing Unity
REM at the repo root does not raise an error - it quietly creates a new, empty
REM project here and you get an editor with no scenes and no Tools menu. Double
REM clicking this file removes the chance of picking the wrong folder.

setlocal
set "PROJECT=%~dp0unity"
set "EDITOR=C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"

if not exist "%PROJECT%\Assets" (
    echo.
    echo   ERROR: no Unity project found at:
    echo     %PROJECT%
    echo.
    pause
    exit /b 1
)

if not exist "%EDITOR%" (
    echo.
    echo   Unity 6000.6.0f1 was not found at:
    echo     %EDITOR%
    echo   Open this folder in Unity Hub instead:
    echo     %PROJECT%
    echo.
    pause
    exit /b 1
)

echo Opening %PROJECT%
start "" "%EDITOR%" -projectPath "%PROJECT%"
endlocal
