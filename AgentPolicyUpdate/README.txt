RDCS Agent - Drop-in Policy Update Folder
=========================================

Purpose
-------
A self-contained folder you can copy to each employee PC to speed up uploads and optimize screenshots.

What’s inside
-------------
- apply_policy_update.ps1  PowerShell script that applies policy updates without needing sqlite3.exe
- update_policies.sql      SQL that upserts UploadPolicy and ScreenshotPolicy in agent.db

Default paths used by the script
--------------------------------
- Database: C:\RDCS Agent\Database\agent.db
- Agent App: C:\RDCS Agent\RDCS.EmployeeAgent.UI.exe

How to use (per PC)
-------------------
1) Copy the entire AgentPolicyUpdate folder to the PC, e.g. C:\RDCS Agent\AgentPolicyUpdate
2) Right-click the folder, Properties -> Unblock (if Windows shows it as downloaded from internet)
3) Right-click apply_policy_update.ps1 -> Run with PowerShell (as Administrator recommended)
4) The script will:
   - Backup the DB to agent.db.bak.<timestamp>
   - Stop the agent if running
   - Apply the SQL using System.Data.SQLite if available, else ODBC fallback
   - Restart the agent

Optional toggle
---------------
- Disable weekends: run the script with -DisableWeekends
  Example (from PowerShell as Admin):
    Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
    cd "C:\RDCS Agent\AgentPolicyUpdate"
    .\apply_policy_update.ps1 -DisableWeekends

Troubleshooting
---------------
- If you get an execution policy error, temporarily bypass it:
    Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
- If both SQLite providers are missing, install one of:
  - System.Data.SQLite (managed provider): https://system.data.sqlite.org
  - SQLite ODBC driver: https://www.ch-werner.de/sqliteodbc/

Verification
------------
- Check C:\RDCS Agent\Diagnostics\screenshot-worker-trace.txt for faster cycles and successful uploads.
