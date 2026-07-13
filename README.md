# RDCS Employee Agent

A commercial-grade Windows desktop agent for employee monitoring, built with .NET 8, WPF, and a Node.js/Express backend.

## Phase 1 Scope

This foundation phase implements:
- Authentication with JWT
- Device registration with fingerprinting
- Configuration download
- Heartbeat monitoring
- Modular architecture for future expansion

## Technology Stack

### Frontend (Windows Agent)
- **.NET 8** with WPF
- **MVVM** with CommunityToolkit.Mvvm
- **Microsoft.Extensions.Hosting** for DI and lifecycle
- **Serilog** for logging
- **Polly** for resilience
- **Windows Credential Manager** for secure token storage

### Backend (API)
- **Node.js** with Express
- **Prisma ORM** with PostgreSQL
- **JWT** authentication
- **Express-validator** for input validation
- **Rate limiting** with express-rate-limit

## Project Structure

```
RDCS Employee Agent/
├── docs/                      # Architecture documentation
├── src/                       # Visual Studio solution
│   ├── RDCS.EmployeeAgent.sln
│   ├── RDCS.EmployeeAgent.UI/        # WPF application
│   ├── RDCS.EmployeeAgent.Core/      # Domain models, interfaces
│   ├── RDCS.EmployeeAgent.Services/   # Business logic
│   ├── RDCS.EmployeeAgent.Infrastructure/ # Windows-specific implementations
│   ├── RDCS.EmployeeAgent.Shared/    # Utilities, constants
│   └── RDCS.EmployeeAgent.Tests/     # Unit tests
├── backend/                   # Express/Node.js API
│   ├── src/
│   │   ├── prisma/
│   │   ├── controllers/
│   │   ├── services/
│   │   ├── repositories/
│   │   ├── routes/
│   │   ├── middleware/
│   │   └── utils/
│   └── package.json
├── config/                    # Configuration files
│   └── settings.json
└── scripts/                   # Build scripts
    └── build.ps1
```

## Getting Started

### Prerequisites

- **.NET 8 SDK**
- **Node.js** 18+
- **PostgreSQL** database (Supabase)
- **PowerShell** (for build scripts)

### Windows Agent Setup

1. Clone the repository
2. Open `src/RDCS.EmployeeAgent.sln` in Visual Studio 2022
3. Restore NuGet packages
4. Update `config/settings.json` with your API URL
5. Build and run the solution

### Backend Setup

1. Navigate to `backend/`
2. Install dependencies: `npm install`
3. Set `DATABASE_URL` in `.env`
4. Run Prisma migrations: `npx prisma migrate dev`
5. Start development server: `npm run dev`

### Building

Run the build script:
```powershell
.\scripts\build.ps1
```

Or build manually:
```bash
cd src
dotnet build RDCS.EmployeeAgent.sln --configuration Release
```

## API Endpoints

### Authentication
- `POST /api/agent/login` - Authenticate employee
- `POST /api/agent/refresh-token` - Refresh access token

### Device Management
- `POST /api/agent/register-device` - Register employee device

### Monitoring
- `POST /api/agent/heartbeat` - Send device heartbeat

### Configuration
- `GET /api/agent/config` - Download agent configuration

## Security

- HTTPS only
- JWT authentication with short-lived access tokens
- Windows Credential Manager for secure token storage
- Device fingerprinting for endpoint validation
- Rate limiting on all endpoints

## Future Modules

Phase 2+ will add:
- Screenshot capture with Amazon S3 upload
- Application monitoring
- Website monitoring
- Idle detection
- USB device monitoring
- Auto-update functionality

## License

Proprietary - All rights reserved
