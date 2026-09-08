RDCS Employee Agent - Policy Update
===================================

This package updates agent policies to speed up uploads and reduce image size.
It sets:
- UploadPolicy: IntervalSeconds=5, MaxParallelUploads=4
- ScreenshotPolicy: Quality=75, MaxWidth=1600, MaxHeight=900

Contents
--------
- update_policies.sql      The SQL to upsert both policies into the agent database
- update_policies.bat      One-click script to apply the SQL and restart the agent
- (bring your own) sqlite3.exe  Place SQLite CLI here (see below)

Prerequisites
-------------
- You need sqlite3.exe in this same folder.
  Download: https://www.sqlite.org/download.html (Windows -> sqlite-tools zip)
  Extract and copy sqlite3.exe next to the .bat and .sql

Default Paths
-------------
- Database: C:\RDCS Agent\Database\agent.db
- Agent app: C:\RDCS Agent\RDCS.EmployeeAgent.UI.exe

If your installation uses different paths, edit update_policies.bat (DB_PATH, AGENT_EXE).

How to apply (per employee PC)
------------------------------
1) Copy the entire PolicyUpdate folder to the PC (e.g., C:\RDCS Agent\PolicyUpdate)
2) Place sqlite3.exe into the same folder (see Prerequisites)
3) Right-click update_policies.bat and Run as administrator
4) The script will:
   - Backup the database to agent.db.bak.<timestamp>
   - Stop the running agent (if found)
   - Apply the SQL to upsert UploadPolicy and ScreenshotPolicy
   - Restart the agent

Verification
------------
- Check C:\RDCS Agent\Diagnostics\screenshot-worker-trace.txt for lines like:
  - UPLOAD_EXEC: Policy Enabled=true
  - UPLOAD_EXEC: Dequeued X jobs
  - UPLOAD_HTTP: SUCCESS 200
- You should see faster job pickup (about every 5s) and up to 4 concurrent uploads.

Rollback
--------
- To undo, close the agent, restore the backup file over agent.db, and restart the agent.
  Example:
  1) taskkill /IM RDCS.EmployeeAgent.UI.exe /F
  2) copy /Y "C:\RDCS Agent\Database\agent.db.bak.<timestamp>" "C:\RDCS Agent\Database\agent.db"
  3) start "" "C:\RDCS Agent\RDCS.EmployeeAgent.UI.exe"

Notes
-----
- Weekend capture can be turned off by editing update_policies.sql and setting "DisableWeekends": true, then re-running the .bat.
- If you use DB Browser for SQLite, you can paste the two INSERT OR REPLACE statements from update_policies.sql directly.
