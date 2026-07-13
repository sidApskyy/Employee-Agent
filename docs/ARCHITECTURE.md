# RDCS Employee Agent — Phase 1 Architecture

> **Version:** 1.0  
> **Scope:** Foundation only. Screenshot / Activity / Browser / Recording modules are **not** implemented in Phase 1. The architecture must allow them to be plugged in later without structural changes.

---

## 1. Executive Summary

The **RDCS Employee Agent** is a commercial-grade Windows desktop agent that authenticates with a Render-hosted Express/Node.js backend, registers the employee device, downloads configuration, and emits a heartbeat every 60 seconds. The design is built around a **modular, hosted-service core** so that future modules (screenshots, application monitoring, USB/device tracking, etc.) can be added as self-contained `IHostedService` modules with minimal integration cost.

### Core Architectural Principles

- **Modular Plugin Architecture:** Every future feature is an `IAgentModule` registered in a central `ModuleRegistry` and managed by a `ModuleHost`.
- **SOLID / Clean Architecture:** UI depends on Services, which depend on Core abstractions. Infrastructure depends on Core. No circular dependencies.
- **MVVM:** WPF views bind to ViewModels from `CommunityToolkit.Mvvm`. No business logic in code-behind.
- **Enterprise Hosting:** `Microsoft.Extensions.Hosting` gives us a first-class DI container, configuration, logging, and lifecycle management.
- **Security:** Windows Credential Manager for tokens, JWT authentication over HTTPS, device fingerprinting, input validation, rate limiting, and no password storage.
- **Scalability:** Stateless services, unique `HttpClient` instances via `IHttpClientFactory`, efficient heartbeats, and worker-ready backend patterns for 10,000+ employees.

---

## 2. Solution & Project Structure

### Solution File

`D:\RDCS Employee Agent\src\RDCS.EmployeeAgent.sln`

### Project Inventory

| Project | Type | Responsibility |
| --- | --- | --- |
| **RDCS.EmployeeAgent.UI** | WPF Application (.NET 8) | The visual shell, views, view models, converters, and window navigation. No business logic. |
| **RDCS.EmployeeAgent.Core** | .NET 8 Class Library | Domain models, interfaces, enums, module abstractions (`IAgentModule`), settings contracts, and DTO contracts. Has no external dependencies on infrastructure. |
| **RDCS.EmployeeAgent.Services** | .NET 8 Class Library | Business logic / orchestration services: `AuthenticationService`, `DeviceRegistrationService`, `HeartbeatService`, `ConfigurationService`, `ModuleManager`, `ApplicationOrchestrator`. |
| **RDCS.EmployeeAgent.Infrastructure** | .NET 8 Class Library | Concrete implementations for Windows-specific concerns: secure token storage, platform information, API client, HTTP client factory policies, logging sinks, and exception handlers. |
| **RDCS.EmployeeAgent.Shared** | .NET 8 Class Library | Common utilities, constants, guards, Result/Error types, and cross-cutting helpers used by all projects. |
| **RDCS.EmployeeAgent.Tests** | xUnit Test Project (.NET 8) | Unit and integration tests for services, infrastructure, and view model behavior. |

### Why This Project Layout?

- **Core is dependency-free.** It owns contracts and abstractions. This makes the architecture stable and testable.
- **Services sit on Core and depend on Infrastructure via abstractions.** This preserves the Dependency Inversion Principle.
- **Infrastructure is isolated.** Windows Credential Manager, `HttpClient`, Serilog sinks, and OS-specific APIs are here and can be swapped out.
- **UI is thin.** It only references Services and Core; it never talks directly to the API or Windows Credential Manager.
- **Shared contains true cross-cutting concerns** that have no business meaning, preventing duplication.
- **Tests** verify the business layer and infrastructure contracts without launching the full WPF app.

---

## 3. Folder Structure

```
D:\RDCS Employee Agent
├── docs
│   ├── ARCHITECTURE.md
│   ├── API_CONTRACTS.md
│   ├── DATABASE_SCHEMA.md
│   └── CHANGELOG.md
├── src
│   ├── RDCS.EmployeeAgent.sln
│   ├── RDCS.EmployeeAgent.UI
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── Views
│   │   │   ├── LoginWindow.xaml
│   │   │   ├── MainWindow.xaml
│   │   │   ├── ShellWindow.xaml
│   │   │   └── SettingsWindow.xaml
│   │   ├── ViewModels
│   │   │   ├── LoginViewModel.cs
│   │   │   ├── MainViewModel.cs
│   │   │   ├── ShellViewModel.cs
│   │   │   └── SettingsViewModel.cs
│   │   ├── Converters
│   │   ├── Behaviors
│   │   ├── Resources
│   │   │   ├── Styles.xaml
│   │   │   └── Themes
│   │   └── Services
│   │       └── INavigationService.cs
│   ├── RDCS.EmployeeAgent.Core
│   │   ├── Models
│   │   │   ├── AgentSettings.cs
│   │   │   ├── DeviceInfo.cs
│   │   │   ├── AgentIdentity.cs
│   │   │   ├── HeartbeatPayload.cs
│   │   │   └── ConfigurationManifest.cs
│   │   ├── Interfaces
│   │   │   ├── IAgentModule.cs
│   │   │   ├── IModuleHost.cs
│   │   │   ├── IAuthenticationService.cs
│   │   │   ├── IDeviceRegistrationService.cs
│   │   │   ├── IHeartbeatService.cs
│   │   │   ├── IConfigurationService.cs
│   │   │   ├── IApiClient.cs
│   │   │   ├── ITokenStorage.cs
│   │   │   ├── IDeviceInfoProvider.cs
│   │   │   ├── IExceptionHandler.cs
│   │   │   └── IAgentLogger.cs
│   │   ├── Enums
│   │   │   ├── AgentStatus.cs
│   │   │   ├── LogCategory.cs
│   │   │   └── ModuleState.cs
│   │   ├── Contracts
│   │   │   ├── ApiContracts
│   │   │   └── DTOs
│   │   └── Exceptions
│   │       ├── AgentException.cs
│   │       └── NetworkException.cs
│   ├── RDCS.EmployeeAgent.Services
│   │   ├── Authentication
│   │   │   └── AuthenticationService.cs
│   │   ├── DeviceRegistration
│   │   │   ├── DeviceRegistrationService.cs
│   │   │   └── DeviceFingerprintBuilder.cs
│   │   ├── Configuration
│   │   │   └── ConfigurationService.cs
│   │   ├── Heartbeat
│   │   │   └── HeartbeatService.cs
│   │   ├── Modules
│   │   │   ├── ModuleHost.cs
│   │   │   ├── ModuleRegistry.cs
│   │   │   └── ModuleManager.cs
│   │   └── Orchestration
│   │       └── ApplicationOrchestrator.cs
│   ├── RDCS.EmployeeAgent.Infrastructure
│   │   ├── Api
│   │   │   ├── ApiClient.cs
│   │   │   ├── BaseApiService.cs
│   │   │   ├── AuthenticationHeaderHandler.cs
│   │   │   └── ApiClientException.cs
│   │   ├── Configuration
│   │   │   ├── SettingsProvider.cs
│   │   │   └── ConfigurationDownloader.cs
│   │   ├── Security
│   │   │   ├── WindowsCredentialStorage.cs
│   │   │   └── TokenEncryption.cs
│   │   ├── Platform
│   │   │   ├── WindowsDeviceInfoProvider.cs
│   │   │   └── WindowsVersionInfoProvider.cs
│   │   ├── Logging
│   │   │   ├── SerilogConfigurator.cs
│   │   │   ├── CategoryEnricher.cs
│   │   │   └── LogFolderProvider.cs
│   │   ├── Http
│   │   │   ├── HttpClientFactoryExtensions.cs
│   │   │   └── PollyPolicyProvider.cs
│   │   └── ExceptionHandling
│   │       ├── GlobalExceptionHandler.cs
│   │       └── NetworkExceptionHandler.cs
│   ├── RDCS.EmployeeAgent.Shared
│   │   ├── Constants
│   │   │   ├── ApiRoutes.cs
│   │   │   ├── ConfigurationKeys.cs
│   │   │   └── ApplicationConstants.cs
│   │   ├── Results
│   │   │   └── Result.cs
│   │   ├── Guards
│   │   │   └── Guard.cs
│   │   └── Utilities
│   │       ├── JsonExtensions.cs
│   │       └── DateTimeProvider.cs
│   └── RDCS.EmployeeAgent.Tests
│       ├── Services
│       ├── Infrastructure
│       ├── ViewModels
│       └── Fixtures
├── config
│   └── settings.json.example
├── scripts
│   └── build.ps1
└── README.md
```

---

## 4. Communication Flow

### 4.1. Application Startup Flow

```
1. App.xaml.cs
   └── Build GenericHost
2. GenericHost
   ├── Configuration
   │   └── appsettings.json + settings.json
   ├── Logging
   │   └── SerilogConfigurator
   ├── Services
   │   └── IAuthenticationService, IDeviceRegistrationService, etc.
   └── HostedServices
       └── ModuleHost
3. ModuleHost
   └── ModuleRegistry → StartAsync each IAgentModule
       ├── HeartbeatModule
       └── ConfigurationModule
4. MainWindow shown
```

### 4.2. Login Flow

```
[LoginViewModel]
    │
    ▼
[IAuthenticationService]
    │
    ▼
[IApiClient] ── POST /api/agent/login
    │
    ▼
[Backend] validates → returns JWT, RefreshToken, EmployeeId, CompanyId, DeviceId, ConfigVersion
    │
    ▼
[IAuthenticationService] stores tokens via [ITokenStorage]
    │
    ▼
[ShellViewModel] shows main dashboard
```

### 4.3. Device Registration Flow

```
[ApplicationOrchestrator] detects first login (no DeviceId / token)
    │
    ▼
[IDeviceRegistrationService]
    │
    ├── [IDeviceInfoProvider] collects OS / HW / MAC / GUID
    ├── [DeviceFingerprintBuilder] computes fingerprint
    │
    ▼
[IApiClient] ── POST /api/agent/register-device
    │
    ▼
[Backend] stores employee_devices, returns DeviceId
    │
    ▼
[ITokenStorage] saves DeviceId + tokens
```

### 4.4. Heartbeat Flow

```
[HeartbeatModule] ── IHostedService executing every 60 seconds
    │
    ▼
[IHeartbeatService]
    │
    ├── [IDeviceInfoProvider] current state
    │
    ▼
[IApiClient] ── POST /api/agent/heartbeat
    │
    ▼
[Backend] updates last_seen_at in employee_devices
```

### 4.5. Configuration Download Flow

```
[ConfigurationModule] on startup
    │
    ▼
[IConfigurationService]
    │
    ▼
[IApiClient] ── GET /api/agent/config?version=X
    │
    ▼
[Backend] returns configuration payload
    │
    ▼
[IConfigurationService] merges into local settings.json + in-memory IOptions
    │
    ▼
[IConfiguration] reloaded; modules react to IOptionsSnapshot<T>
```

---

## 5. Class Responsibilities

### 5.1. Core (Interfaces + Models)

| Class / Interface | Responsibility |
| --- | --- |
| `IAgentModule` | Contract for all future modules. Methods: `Task StartAsync(CancellationToken)`, `Task StopAsync(CancellationToken)`, `ModuleState State { get; }`, `string Name { get; }`. |
| `IModuleHost` | Lifecycle manager that starts and stops modules in dependency order on app startup/shutdown. |
| `IAuthenticationService` | Login, logout, refresh token, session status. |
| `IDeviceRegistrationService` | Collects device info, computes fingerprint, and registers with backend. |
| `IConfigurationService` | Downloads, validates, and applies remote configuration. |
| `IHeartbeatService` | Builds and sends heartbeat payloads. |
| `IApiClient` | Low-level typed REST client. Wraps `HttpClient` and returns `Result<T>` / exceptions. |
| `ITokenStorage` | Secure storage abstraction for JWT, refresh token, device ID, employee ID, company ID. |
| `IDeviceInfoProvider` | Cross-platform abstraction for reading device / OS / hardware information. |
| `IExceptionHandler` | Handle recoverable errors and log them. |

### 5.2. UI

| Class | Responsibility |
| --- | --- |
| `LoginWindow` / `LoginViewModel` | Login UI, `Remember Me`, `Show Password`, loading state, error display. |
| `ShellWindow` / `ShellViewModel` | Main window after login, status indicators, navigation. |
| `MainWindow` / `MainViewModel` | Dashboard placeholder, status of modules. |
| `SettingsWindow` / `SettingsViewModel` | Display local settings and configuration version. |
| `INavigationService` | ViewModel-to-view navigation abstraction (e.g., `MainWindow` / `ShellWindow`). |

### 5.3. Services

| Class | Responsibility |
| --- | --- |
| `AuthenticationService` | Implements `IAuthenticationService`. Calls API, validates input, stores tokens via `ITokenStorage`, emits auth state. |
| `DeviceRegistrationService` | Implements `IDeviceRegistrationService`. Orchestrates info collection, fingerprint, and registration. |
| `DeviceFingerprintBuilder` | Creates deterministic device hash from machine GUID + MAC + CPU + disk serial. |
| `HeartbeatService` | Implements `IHeartbeatService`. Builds payload every 60 seconds via `IHostedService` heartbeat module. |
| `ConfigurationService` | Implements `IConfigurationService`. Downloads config and applies to `IConfiguration`. |
| `ModuleHost` | Implements `IModuleHost` and `IHostedService`. Starts/stops all registered `IAgentModule`s. |
| `ModuleRegistry` | Discovers and registers all modules. Used by `ModuleHost`. |
| `ModuleManager` | Provides runtime state inspection (e.g., for the dashboard). |
| `ApplicationOrchestrator` | High-level coordinator: if not authenticated, show login; if no device, register; then start modules. |

### 5.4. Infrastructure

| Class | Responsibility |
| --- | --- |
| `ApiClient` | `IApiClient` implementation. Uses `HttpClient`, handles JSON serialization, response validation, and returns `Result<T>`. |
| `BaseApiService` | Base class for `AuthenticationService`/`HeartbeatService` etc. Provides common retry/cancellation logic. |
| `AuthenticationHeaderHandler` | `DelegatingHandler` that reads access token from `ITokenStorage` and attaches `Authorization: Bearer`. |
| `WindowsCredentialStorage` | `ITokenStorage` implementation using `CredentialManager` from Windows. Encrypted by Windows DPAPI. |
| `TokenEncryption` | Optional additional AES-256 encryption layer before storing large blobs. |
| `WindowsDeviceInfoProvider` | `IDeviceInfoProvider` for Windows. Uses WMI / `System.Management` and `Registry` for Machine GUID. |
| `SerilogConfigurator` | Configures global logger: console, rolling file, category enricher, JSON formatter for logs. |
| `CategoryEnricher` | Adds `LogCategory` to every log entry (Application, Authentication, Exception, Heartbeat). |
| `LogFolderProvider` | Resolves `%LocalAppData%\RDCS\EmployeeAgent\logs`. |
| `GlobalExceptionHandler` | Handles `AppDomain.CurrentDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException`. |
| `NetworkExceptionHandler` | Transforms `HttpRequestException` / timeout into typed `NetworkException`. |
| `PollyPolicyProvider` | Defines retry, circuit breaker, and timeout policies for `IHttpClientFactory`. |
| `HttpClientFactoryExtensions` | Registers the `agent` named `HttpClient` with base address, headers, handler, and Polly policies. |

---

## 6. Database Schema (Prisma)

### 6.1. Model Overview

```prisma
model EmployeeDevice {
  id                String   @id @default(uuid())
  employeeId        String   @map("employee_id")
  companyId         String   @map("company_id")
  deviceName        String   @map("device_name")
  computerName      String   @map("computer_name")
  machineGuid       String   @map("machine_guid")
  fingerprint       String   @unique
  osVersion         String   @map("os_version")
  windowsUsername   String   @map("windows_username")
  processor         String
  ramGb             Int      @map("ram_gb")
  diskSizeGb        Int      @map("disk_size_gb")
  macAddress        String   @map("mac_address")
  agentVersion      String   @map("agent_version")
  configVersion     String   @map("config_version")
  isOnline          Boolean  @default(false) @map("is_online")
  isActive          Boolean  @default(true) @map("is_active")
  isBlocked         Boolean  @default(false) @map("is_blocked")
  lastSeenAt        DateTime? @map("last_seen_at")
  registeredAt      DateTime @default(now()) @map("registered_at")
  updatedAt         DateTime @updatedAt @map("updated_at")

  sessions          AgentSession[]
  logs              AgentLog[]

  @@index([employeeId])
  @@index([companyId])
  @@index([fingerprint])
  @@index([isOnline, lastSeenAt])
  @@index([registeredAt])
  @@map("employee_devices")
}

model AgentSession {
  id              String    @id @default(uuid())
  deviceId        String    @map("device_id")
  employeeId      String    @map("employee_id")
  companyId       String    @map("company_id")
  accessTokenJti  String?   @map("access_token_jti")
  refreshTokenHash String  @map("refresh_token_hash")
  ipAddress       String?   @map("ip_address")
  userAgent       String?   @map("user_agent")
  startedAt       DateTime  @default(now()) @map("started_at")
  lastActivityAt  DateTime  @default(now()) @map("last_activity_at")
  expiresAt       DateTime? @map("expires_at")
  isRevoked       Boolean   @default(false) @map("is_revoked")
  revokedAt       DateTime? @map("revoked_at")
  createdAt       DateTime  @default(now()) @map("created_at")
  updatedAt       DateTime  @updatedAt @map("updated_at")

  device          EmployeeDevice @relation(fields: [deviceId], references: [id], onDelete: Cascade)

  @@index([deviceId])
  @@index([employeeId])
  @@index([refreshTokenHash])
  @@index([isRevoked, expiresAt])
  @@map("agent_sessions")
}

model AgentVersion {
  id              String   @id @default(uuid())
  version         String   @unique
  environment     String   @default("production")
  downloadUrl     String?  @map("download_url")
  releaseNotes    String?  @map("release_notes")
  isMandatory     Boolean  @default(false) @map("is_mandatory")
  minimumConfigVersion String? @map("minimum_config_version")
  publishedAt     DateTime @default(now()) @map("published_at")
  createdAt       DateTime @default(now()) @map("created_at")
  updatedAt       DateTime @updatedAt @map("updated_at")

  @@index([version])
  @@index([environment])
  @@map("agent_versions")
}

model AgentLog {
  id              String   @id @default(uuid())
  deviceId        String?  @map("device_id")
  employeeId      String?  @map("employee_id")
  companyId       String?  @map("company_id")
  sessionId       String?  @map("session_id")
  category        String   // AUTH, HEARTBEAT, EXCEPTION, APPLICATION
  level           String   // Debug, Information, Warning, Error, Fatal
  message         String
  exception       String?
  properties      Json?    // structured JSON
  loggedAt        DateTime @default(now()) @map("logged_at")
  receivedAt      DateTime @default(now()) @map("received_at")

  device          EmployeeDevice? @relation(fields: [deviceId], references: [id], onDelete: SetNull)

  @@index([deviceId])
  @@index([employeeId])
  @@index([companyId])
  @@index([category])
  @@index([level])
  @@index([loggedAt])
  @@index([receivedAt])
  @@map("agent_logs")
}
```

### 6.2. Schema Rationale

- **`employee_devices`** is the master identity table for an agent endpoint. It stores the device fingerprint and is unique at the company + employee + machine level.
- **`agent_sessions`** tracks active and revoked tokens. `refresh_token_hash` is stored (never plain text) so the backend can validate and revoke refresh tokens.
- **`agent_versions`** is the foundation for the future Auto-Update module. It maps a version to the required configuration schema.
- **`agent_logs`** is the remote audit table. Local Serilog files remain the primary diagnostic source; the backend stores log events for compliance and auditing.

### 6.3. Constraints & Indexes

- Unique index on `employee_devices.fingerprint` to prevent duplicate registrations of the same machine.
- Composite index on `employee_devices.is_online` + `last_seen_at` for fast online-user dashboards.
- Index on `agent_sessions.refresh_token_hash` for O(1) refresh-token validation.
- Indexes on `agent_logs` support filtering by device, company, category, level, and time range.

---

## 7. API Contracts

### 7.1. Authentication

#### `POST /api/agent/login`

**Request:**

```json
{
  "email": "string",
  "password": "string",
  "clientVersion": "1.0.0",
  "environment": "production"
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
    "expiresIn": 3600,
    "tokenType": "Bearer",
    "employeeId": "uuid",
    "companyId": "uuid",
    "deviceId": "uuid | null",
    "configVersion": "string",
    "requiresDeviceRegistration": true
  }
}
```

**Errors:**

- `400 Bad Request` — validation errors
- `401 Unauthorized` — invalid credentials
- `429 Too Many Requests` — rate limit

### 7.2. Device Registration

#### `POST /api/agent/register-device`

**Request:**

```json
{
  "employeeId": "uuid",
  "companyId": "uuid",
  "deviceName": "Dell-Laptop-001",
  "computerName": "DESKTOP-ABC123",
  "machineGuid": "windows-registry-guid",
  "fingerprint": "sha256-of-guid+mac+cpu+disk",
  "osVersion": "Windows 11 Pro 23H2",
  "windowsUsername": "jdoe",
  "processor": "Intel Core i7-1370P",
  "ramGb": 32,
  "diskSizeGb": 512,
  "macAddress": "00:1A:2B:3C:4D:5E",
  "agentVersion": "1.0.0"
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "deviceId": "uuid",
    "deviceName": "Dell-Laptop-001",
    "configVersion": "string",
    "isBlocked": false,
    "registeredAt": "2026-01-01T00:00:00Z"
  }
}
```

### 7.3. Heartbeat

#### `POST /api/agent/heartbeat`

**Request:**

```json
{
  "employeeId": "uuid",
  "deviceId": "uuid",
  "agentVersion": "1.0.0",
  "computerName": "DESKTOP-ABC123",
  "isOnline": true,
  "timestamp": "2026-01-01T00:00:00Z",
  "configVersion": "string",
  "systemMetrics": {
    "cpuPercent": 12.5,
    "memoryUsedMb": 8192
  }
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "nextHeartbeatIntervalSeconds": 60,
    "configVersion": "string",
    "configChanged": false,
    "isBlocked": false,
    "requiresLogout": false
  }
}
```

### 7.4. Configuration Download

#### `GET /api/agent/config`

**Query:**

```
?version=current&environment=production
```

**Response:**

```json
{
  "success": true,
  "data": {
    "configVersion": "string",
    "apiUrl": "https://api.rdcs.example.com",
    "environment": "production",
    "agentVersion": "1.0.0",
    "retryCount": 3,
    "timeoutSeconds": 30,
    "loggingLevel": "Information",
    "heartbeatIntervalSeconds": 60,
    "features": {
      "screenshotsEnabled": false,
      "applicationMonitoringEnabled": false,
      "websiteMonitoringEnabled": false,
      "idleDetectionEnabled": false,
      "usbMonitoringEnabled": false
    }
  }
}
```

### 7.5. Token Refresh (Supporting)

#### `POST /api/agent/refresh-token`

**Request:**

```json
{
  "refreshToken": "string"
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "accessToken": "string",
    "refreshToken": "string",
    "expiresIn": 3600
  }
}
```

---

## 8. Backend Structure

### 8.1. Express Project Layout

```
backend/
├── src
│   ├── server.ts
│   ├── app.ts
│   ├── config
│   │   ├── index.ts
│   │   ├── database.ts
│   │   └── jwt.ts
│   ├── routes
│   │   ├── index.ts
│   │   └── agent.routes.ts
│   ├── controllers
│   │   ├── auth.controller.ts
│   │   ├── device.controller.ts
│   │   ├── heartbeat.controller.ts
│   │   └── config.controller.ts
│   ├── services
│   │   ├── auth.service.ts
│   │   ├── device.service.ts
│   │   ├── heartbeat.service.ts
│   │   ├── config.service.ts
│   │   └── token.service.ts
│   ├── repositories
│   │   ├── device.repository.ts
│   │   ├── session.repository.ts
│   │   ├── version.repository.ts
│   │   └── log.repository.ts
│   ├── dtos
│   │   ├── login.dto.ts
│   │   ├── register-device.dto.ts
│   │   ├── heartbeat.dto.ts
│   │   └── config.dto.ts
│   ├── validators
│   │   ├── login.validator.ts
│   │   ├── register-device.validator.ts
│   │   └── heartbeat.validator.ts
│   ├── middleware
│   │   ├── auth.middleware.ts
│   │   ├── device.middleware.ts
│   │   ├── rate-limit.middleware.ts
│   │   ├── error.middleware.ts
│   │   └── validation.middleware.ts
│   ├── prisma
│   │   └── schema.prisma
│   └── utils
│       ├── password.util.ts
│       ├── jwt.util.ts
│       └── response.util.ts
├── tests
├── package.json
└── tsconfig.json
```

### 8.2. Middleware Responsibilities

- **`auth.middleware.ts`** — validates JWT, attaches `req.user`.
- **`device.middleware.ts`** — ensures `deviceId` in token/session matches the device making the request.
- **`rate-limit.middleware.ts`** — per-IP and per-device limits (e.g., 100 requests / 15 minutes).
- **`validation.middleware.ts`** — runs `express-validator` DTOs.
- **`error.middleware.ts`** — centralized error handling, returns standardized JSON error responses.

---

## 9. Security Design

- **HTTPS only:** Client configuration and HttpClient base address enforce `https://`.
- **JWT Authentication:** Short-lived access tokens (1 hour), refresh tokens (7–30 days), stored as hashes in `agent_sessions`.
- **Secure Token Storage:** Windows Credential Manager (DPAPI) plus optional AES-256 encryption. Passwords are never persisted.
- **Device Validation:** Backend links the device fingerprint to the token/session. Requests from an unknown or blocked device are rejected.
- **Input Validation:** `express-validator` on backend, `FluentValidation` / `System.ComponentModel.DataAnnotations` on client.
- **Rate Limiting:** `express-rate-limit` + Redis-compatible store for scaled Render deployments.
- **Logging Sanitization:** No passwords or tokens in logs. Exception details are sanitized before remote upload.
- **Sensitive Data Handling:** WMI queries return only hardware identifiers; no user files or screen content are collected in Phase 1.

---

## 10. Implementation Roadmap

### Phase 1 — Foundation (Current)

| Milestone | Deliverables |
| --- | --- |
| **1.1 Repository & Solution** | Create Visual Studio solution, 6 projects, NuGet package references, folder structure. |
| **1.2 Core Models & Interfaces** | Define domain models, `IAgentModule`, service interfaces, DTOs, Result type. |
| **1.3 Shared Utilities** | Constants, guards, JSON helpers, DateTime provider. |
| **1.4 Infrastructure** | Serilog setup, Windows credential storage, device info provider, exception handlers, `HttpClient` factory + Polly. |
| **1.5 API Client** | `ApiClient`, `BaseApiService`, `AuthenticationHeaderHandler`, typed REST calls. |
| **1.6 Services** | `AuthenticationService`, `DeviceRegistrationService`, `ConfigurationService`, `HeartbeatService`, `ModuleHost`, `ModuleRegistry`, `ApplicationOrchestrator`. |
| **1.7 UI** | Login screen, main shell, dashboard, settings view, `CommunityToolkit.Mvvm` ViewModels. |
| **1.8 Backend APIs** | Express routes, controllers, services, repositories, Prisma schema, validators, middleware, JWT utilities. |
| **1.9 Configuration** | `settings.json` schema, configuration downloader, version-based config updates. |
| **1.10 Testing** | Unit tests for services, API client, ViewModels, and integration tests for backend endpoints. |
| **1.11 Packaging** | `build.ps1`, `README.md`, deployment notes for Render + future S3 setup. |

### Future Phases (Not in Phase 1)

| Phase | Module |
| --- | --- |
| **2** | Screenshot Capture + Amazon S3 upload |
| **3** | Application Monitoring + Browser Monitoring |
| **4** | Idle Detection + Live Status + Notifications |
| **5** | Device Monitoring + USB Monitoring |
| **6** | Auto Update + OTA distribution |

---

## 11. Design Decisions & Assumptions

- **Modularity:** Every module is an `IHostedService` with explicit `StartAsync`/`StopAsync` methods. The `ModuleHost` is the single orchestrator. This removes the need for each module to manage its own timers or app lifecycle.
- **Configuration:** Local `settings.json` is the bootstrap source (API URL, log path). Remote configuration is downloaded after login and merged into `IConfiguration` so services can use `IOptionsSnapshot<T>`.
- **Settings Security:** `settings.json` contains non-sensitive data only. Secrets (tokens, refresh tokens) are stored in Windows Credential Manager.
- **Result Pattern:** Core services return `Result<T>` (or `Result`) for predictable error handling without throwing exceptions for business failures.
- **Cancellation Tokens:** All async operations accept `CancellationToken`. Heartbeat and configuration modules respect `StopAsync` cancellation.
- **Enterprise Logging:** Serilog writes to rolling files under `%LocalAppData%\RDCS\EmployeeAgent\logs`. Categories are enriched for filtering.
- **Backend-First IDs:** The backend generates the `deviceId`, `sessionId`, and token JTI. The agent never invents these values.
- **Render + Supabase:** Backend is stateless and uses Prisma with the Supabase PostgreSQL URL. Future S3 storage will replace local screenshot storage; no Supabase Storage is used.
