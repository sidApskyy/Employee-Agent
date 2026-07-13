# RDCS Employee Agent — Production Deployment Guide
## 30 Employees Ready-to-Use Setup

---

## Prerequisites

| Requirement | Notes |
|---|---|
| Node.js 20+ | Backend server |
| PostgreSQL 15+ **or** Supabase account | Database |
| AWS S3 bucket | Screenshot storage |
| Windows machine per employee | Desktop agent |
| .NET 8 Runtime on each employee machine | Desktop agent |

---

## Part 1 — Backend Server Setup

### 1.1 Configure environment

```bash
cd src/backend
cp .env.example .env
```

Edit `.env` and fill in **every** `REPLACE_WITH_` / `YOUR_` value:

| Variable | How to get it |
|---|---|
| `DATABASE_URL` | Your PostgreSQL connection string |
| `JWT_SECRET` | Run: `node -e "console.log(require('crypto').randomBytes(64).toString('hex'))"` |
| `S3_BUCKET_NAME` | Your AWS S3 bucket name |
| `S3_ACCESS_KEY_ID` | AWS IAM user with S3 write permissions |
| `S3_SECRET_ACCESS_KEY` | AWS IAM secret |
| `API_URL` | The public IP/hostname of this server, e.g. `http://192.168.1.100:3000` |

### 1.2 Install dependencies

```bash
npm install
```

### 1.3 Run database migrations

```bash
npm run prisma:migrate:deploy
```

This creates all tables including `employees`, `refresh_tokens`, `uploaded_files`, etc.

### 1.4 Generate Prisma client

```bash
npm run prisma:generate
```

### 1.5 Seed 30 employees

```bash
npm run db:seed
```

This creates 30 employee accounts with the default password from `.env` → `SEED_DEFAULT_PASSWORD` (default: `Change@Me2024!`).

Employee emails follow the pattern: `firstname.lastname@rdcs.local`

> **Security:** Distribute each employee their email + default password and instruct them to change it immediately.

### 1.6 Start the backend

**Development:**
```bash
npm run dev
```

**Production (with PM2):**
```bash
npm install -g pm2
npm run build
pm2 start dist/server.js --name rdcs-backend
pm2 save
pm2 startup
```

### 1.7 Verify backend is running

```
GET http://YOUR_SERVER_IP:3000/api/agent/health
```
Expected: `{ "status": "ok", "timestamp": "..." }`

---

## Part 2 — Desktop Agent Setup (per employee machine)

### 2.1 Edit `appsettings.json`

Located next to the built `.exe`. Set:

```json
{
  "ApiUrl": "http://YOUR_SERVER_IP:3000"
}
```

Replace `YOUR_SERVER_IP:3000` with the same value as `API_URL` in your `.env`.

### 2.2 Build the desktop agent

```bash
cd src/RDCS.EmployeeAgent.UI
dotnet publish -c Release -r win-x64 --self-contained false
```

### 2.3 Deploy to employee machines

Copy the `publish/` output folder to each machine. Each employee:
1. Runs `RDCS.EmployeeAgent.UI.exe`
2. Logs in with their email (`firstname.lastname@rdcs.local`) and the default password
3. The agent registers their device automatically on first login
4. Screenshots begin capturing per policy (every 30 seconds by default)
5. Screenshots are queued locally and uploaded to S3 via the backend

---

## Part 3 — AWS S3 Bucket Configuration

### 3.1 Create the bucket

```bash
aws s3 mb s3://rdcs-employee-screenshots --region us-east-1
```

### 3.2 Block public access (required)

```bash
aws s3api put-public-access-block \
  --bucket rdcs-employee-screenshots \
  --public-access-block-configuration \
  "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
```

### 3.3 Create IAM user with minimal S3 permissions

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:PutObject", "s3:GetObject", "s3:DeleteObject", "s3:ListBucket"],
      "Resource": [
        "arn:aws:s3:::rdcs-employee-screenshots",
        "arn:aws:s3:::rdcs-employee-screenshots/*"
      ]
    }
  ]
}
```

---

## Part 4 — API Endpoints Reference

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/agent/login` | None | Employee login |
| POST | `/api/agent/refresh` | None | Refresh access token |
| POST | `/api/agent/logout` | Bearer | Logout + revoke tokens |
| POST | `/api/agent/register-device` | Bearer | Register employee device |
| POST | `/api/agent/heartbeat` | Bearer | Send heartbeat |
| GET | `/api/agent/config` | None | Get agent configuration |
| GET | `/api/agent/health` | None | Health check |
| POST | `/api/storage/upload` | Bearer | Upload screenshot |
| POST | `/api/storage/upload/complete` | Bearer | Confirm upload complete |
| GET | `/api/storage/usage` | Bearer | Get storage usage |
| GET | `/api/storage/files` | Bearer | List uploaded files |

---

## Part 5 — Employee Credentials Reference

All 30 employees are seeded with the same default password (`SEED_DEFAULT_PASSWORD`).

| # | Name | Email |
|---|---|---|
| 1 | Ahmed Hassan | ahmed.hassan@rdcs.local |
| 2 | Sara Khan | sara.khan@rdcs.local |
| 3 | Omar Farooq | omar.farooq@rdcs.local |
| 4 | Fatima Ali | fatima.ali@rdcs.local |
| 5 | Bilal Mahmood | bilal.mahmood@rdcs.local |
| 6 | Ayesha Siddiqui | ayesha.siddiqui@rdcs.local |
| 7 | Usman Sheikh | usman.sheikh@rdcs.local |
| 8 | Hina Riaz | hina.riaz@rdcs.local |
| 9 | Zain Raza | zain.raza@rdcs.local |
| 10 | Maria Baig | maria.baig@rdcs.local |
| 11 | Kashif Nawaz | kashif.nawaz@rdcs.local |
| 12 | Sana Iqbal | sana.iqbal@rdcs.local |
| 13 | Tariq Mehmood | tariq.mehmood@rdcs.local |
| 14 | Nadia Akram | nadia.akram@rdcs.local |
| 15 | Imran Butt | imran.butt@rdcs.local |
| 16 | Rabia Aslam | rabia.aslam@rdcs.local |
| 17 | Danish Qureshi | danish.qureshi@rdcs.local |
| 18 | Amna Javed | amna.javed@rdcs.local |
| 19 | Faisal Chaudhry | faisal.chaudhry@rdcs.local |
| 20 | Saima Malik | saima.malik@rdcs.local |
| 21 | Adnan Zahid | adnan.zahid@rdcs.local |
| 22 | Lubna Wahid | lubna.wahid@rdcs.local |
| 23 | Waqar Hussain | waqar.hussain@rdcs.local |
| 24 | Rubina Sajid | rubina.sajid@rdcs.local |
| 25 | Saad Nadeem | saad.nadeem@rdcs.local |
| 26 | Farah Zubair | farah.zubair@rdcs.local |
| 27 | Naveed Akhtar | naveed.akhtar@rdcs.local |
| 28 | Mehwish Mirza | mehwish.mirza@rdcs.local |
| 29 | Asif Rehman | asif.rehman@rdcs.local |
| 30 | Shazia Anwar | shazia.anwar@rdcs.local |

---

## Part 6 — Verification Checklist

Before going live, verify each item:

- [ ] Backend starts without errors (`npm run dev`)
- [ ] `GET /api/agent/health` returns `{ "status": "ok" }`
- [ ] Login works: `POST /api/agent/login` with any seeded employee email + default password returns `accessToken`
- [ ] Database has 30 rows in `employees` table (`npm run prisma:studio`)
- [ ] S3 bucket exists, is private, IAM credentials have PutObject permission
- [ ] Desktop agent `appsettings.json` has correct `ApiUrl`
- [ ] Desktop agent starts, login window appears
- [ ] After login, screenshots folder created at `C:\RDCS Agent\Screenshots\`
- [ ] After 30 seconds, first screenshot appears in the folder
- [ ] After ~1 minute, first screenshot appears in the S3 bucket under `{companyId}/{employeeId}/`

---

## Part 7 — Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| Login returns 401 | Wrong email/password | Check email exactly as listed; password = `SEED_DEFAULT_PASSWORD` |
| Login returns 500 | DB not migrated or wrong `DATABASE_URL` | Run `prisma:migrate:deploy` + check `.env` |
| Screenshots not uploading | `ApiUrl` wrong in `appsettings.json` | Set to `http://YOUR_SERVER_IP:3000` |
| S3 upload fails | Wrong AWS credentials or bucket name | Verify `.env` S3 values |
| Agent won't start | .NET 8 not installed | Install from https://dotnet.microsoft.com/download/dotnet/8.0 |
| "No stored access token" in agent logs | Agent restarted after token expired | Employee should log in again |
