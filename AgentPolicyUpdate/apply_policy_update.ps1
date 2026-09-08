# Requires: Windows PowerShell 5+ (built-in)
# Purpose: Apply policy updates to RDCS Agent without external sqlite3.exe
# This script uses System.Data.SQLite provider if installed; otherwise prompts to install.

param(
  [string]$DbPath = "C:\RDCS Agent\Database\agent.db",
  [string]$AgentExe = "C:\RDCS Agent\RDCS.EmployeeAgent.UI.exe",
  [switch]$DisableWeekends
)

Write-Host "RDCS Agent Policy Update" -ForegroundColor Cyan

# Verify DB exists
if (!(Test-Path -LiteralPath $DbPath)) {
  Write-Error "Database not found: $DbPath"
  exit 1
}

# Stop agent if running
Get-Process -Name "RDCS.EmployeeAgent.UI" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Backup DB
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backup = "$DbPath.bak.$timestamp"
Copy-Item -LiteralPath $DbPath -Destination $backup -Force
Write-Host "Backup created: $backup"

# Load update SQL
$sqlPath = Join-Path $PSScriptRoot "update_policies.sql"
if (!(Test-Path -LiteralPath $sqlPath)) {
  Write-Error "Missing update_policies.sql next to this script."
  exit 1
}
$sql = Get-Content -LiteralPath $sqlPath -Raw

if ($DisableWeekends) {
  $sql = $sql -replace '"DisableWeekends": false', '"DisableWeekends": true'
}

# Use built-in OLE DB/ODBC if SQLite provider isn't available
$connectionString = "Data Source=$DbPath;Version=3;"
try {
  Add-Type -AssemblyName System.Data.SQLite -ErrorAction Stop
  $conn = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
  $conn.Open()
  $cmd = $conn.CreateCommand()
  $cmd.CommandText = $sql
  [void]$cmd.ExecuteNonQuery()
  $conn.Close()
  Write-Host "Policy SQL applied via System.Data.SQLite."
}
catch {
  Write-Warning "System.Data.SQLite not available. Falling back to ODBC."
  $conn = New-Object System.Data.Odbc.OdbcConnection("Driver={SQLite3 ODBC Driver};Database=$DbPath;")
  try {
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    [void]$cmd.ExecuteNonQuery()
    $conn.Close()
    Write-Host "Policy SQL applied via ODBC."
  }
  catch {
    Write-Error "No SQLite provider/driver found. Install either System.Data.SQLite or SQLite ODBC driver."
    Write-Host "Download options:" -ForegroundColor Yellow
    Write-Host "- System.Data.SQLite: https://system.data.sqlite.org/index.html/doc/trunk/www/downloads.wiki"
    Write-Host "- SQLite ODBC: https://www.ch-werner.de/sqliteodbc/"
    exit 1
  }
}

# Restart agent
if (Test-Path -LiteralPath $AgentExe) {
  Start-Process -FilePath $AgentExe | Out-Null
  Write-Host "Agent restarted: $AgentExe"
}
else {
  Write-Warning "Agent executable not found: $AgentExe. Start it manually."
}

Write-Host "Done." -ForegroundColor Green
