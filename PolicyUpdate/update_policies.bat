@echo off
setlocal enabledelayedexpansion

set DB_PATH=C:\RDCS Agent\Database\agent.db
set AGENT_EXE=C:\RDCS Agent\RDCS.EmployeeAgent.UI.exe
set SQLITE=sqlite3.exe

echo RDCS Policy Update
ECHO -------------------

if not exist "%DB_PATH%" (
  echo ERROR: Database not found at "%DB_PATH%".
  echo If your agent DB is elsewhere, edit DB_PATH inside update_policies.bat and re-run.
  pause
  exit /b 1
)

if not exist "%SQLITE%" (
  echo ERROR: sqlite3.exe not found in this folder.
  echo Download the Windows "sqlite-tools" zip from https://www.sqlite.org/download.html
  echo Extract and place sqlite3.exe in this same PolicyUpdate folder.
  pause
  exit /b 1
)

for /f "tokens=1 delims=." %%a in ('wmic OS Get localdatetime ^| find "."') do set DTS=%%a
set TS=%DTS:~0,4%-%DTS:~4,2%-%DTS:~6,2%_%DTS:~8,2%-%DTS:~10,2%-%DTS:~12,2%

echo Backing up database to %DB_PATH%.bak.%TS% ...
copy /Y "%DB_PATH%" "%DB_PATH%.bak.%TS%" >nul

echo Stopping agent if running...
taskkill /IM "RDCS.EmployeeAgent.UI.exe" /F >nul 2>&1

ECHO Applying policy SQL...
"%SQLITE%" "%DB_PATH%" ".read update_policies.sql"
if errorlevel 1 (
  echo ERROR: Failed to apply SQL. See messages above.
  pause
  exit /b 1
)

ECHO Restarting agent...
if exist "%AGENT_EXE%" (
  start "" "%AGENT_EXE%"
) else (
  echo WARNING: Agent executable not found at "%AGENT_EXE%". Start it manually.
)

echo Done.
pause
