# Enterprise Backend Integration Architecture

## 1. System Architecture

### 1.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Desktop Agent                            │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │   UI Layer   │  │  Core Layer  │  │ Services     │         │
│  │  (WPF/MVVM)  │  │  (Domain)    │  │  (Business)  │         │
│  └──────────────┘  └──────────────┘  └──────────────┘         │
│         │                 │                 │                 │
│         └─────────────────┼─────────────────┘                 │
│                           │                                     │
│  ┌────────────────────────┼──────────────────────────────────┐ │
│  │           Infrastructure Layer                             │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  │ │
│  │  │  Queue   │  │  Event   │  │  SQLite  │  │  Storage │  │ │
│  │  │  System  │  │  Bus     │  │  DB      │  │  Manager │  │ │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘  │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  │ │
│  │  │  Policy  │  │  Health  │  │  Feature │  │  Retry   │  │ │
│  │  │  Engine  │  │  Monitor │  │  Flags   │  │  Engine  │  │ │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘  │ │
│  └────────────────────────────────────────────────────────────┘ │
│                           │                                     │
│  ┌────────────────────────┼──────────────────────────────────┐ │
│  │           Backend Integration Layer                        │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  │ │
│  │  │  Auth    │  │  Device  │  │  Policy  │  │  Heart   │  │ │
│  │  │  Sync    │  │  Reg     │  │  Sync    │  │  Beat    │  │ │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘  │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  │ │
│  │  │  Feature │  │  Queue   │  │  Offline │  │  API     │  │ │
│  │  │  Sync    │  │  Sync    │  │  Manager │  │  Client  │  │ │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘  │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ HTTPS/JWT
                              │
┌─────────────────────────────────────────────────────────────────┐
│                      Backend API (Node.js)                      │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐         │
│  │  Auth    │  │  Device  │  │  Policy  │  │  Heart   │         │
│  │  API     │  │  API     │  │  API     │  │  Beat    │         │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐         │
│  │  Feature │  │  Queue   │  │  Config  │  │  CRM     │         │
│  │  API     │  │  API     │  │  API     │  │  API     │         │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘         │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │
┌─────────────────────────────────────────────────────────────────┐
│                   Supabase PostgreSQL                            │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐         │
│  │Employee  │  │ Device   │  │ Policy   │  │ Feature  │         │
│  │          │  │          │  │          │  │ Flags    │         │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐         │
│  │Heartbeat │  │ Queue    │  │ Screenshot│  │ Config   │         │
│  │          │  │          │  │ Metadata │  │          │         │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘         │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Component Responsibilities

#### Desktop Agent Components

**Authentication Synchronization Service**
- Manages JWT token lifecycle
- Handles token refresh before expiration
- Stores tokens securely in Windows Credential Manager
- Implements automatic re-authentication on token expiry
- Validates token status on startup

**Device Registration Service**
- Generates device fingerprint (hardware ID, MAC, hostname, OS version)
- Registers new devices with backend
- Updates device heartbeat status
- Stores device credentials locally
- Handles device re-registration scenarios

**Policy Synchronization Service**
- Downloads policies from backend
- Caches policies in SQLite for offline access
- Detects policy changes via version comparison
- Publishes policy change events via Event Bus
- Implements configurable refresh intervals

**Feature Flag Synchronization Service**
- Downloads feature flags from backend
- Caches flags locally with version tracking
- Implements hot reload for flag changes
- Publishes flag change events
- Supports A/B testing scenarios

**Heartbeat Synchronization Service**
- Collects system metrics (CPU, RAM, Disk, Storage)
- Monitors screenshot worker status
- Reports queue size and database health
- Sends agent version information
- Implements configurable heartbeat intervals

**Offline Synchronization Manager**
- Detects network connectivity status
- Stores events locally when offline
- Implements automatic retry on reconnection
- Ensures no data loss during offline periods
- Manages offline queue priority

**Queue Synchronization Service**
- Synchronizes screenshot metadata
- Syncs health reports and logs
- Uploads queued events
- Syncs queue history
- Implements batch upload optimization

**Retry Engine**
- Implements exponential backoff strategy
- Enforces retry limits per operation type
- Manages dead letter queue for failed operations
- Supports cancellation tokens for cancellation
- Implements timeout management

#### Backend API Components

**Authentication API**
- Validates user credentials
- Issues JWT access tokens
- Handles token refresh requests
- Validates device fingerprints
- Implements rate limiting

**Device API**
- Registers new devices
- Updates device heartbeat
- Retrieves device information
- Manages device status
- Handles device deregistration

**Policy API**
- Serves policy configurations
- Handles policy versioning
- Supports policy rollback
- Implements policy caching
- Manages policy distribution

**Feature Flag API**
- Serves feature flag configurations
- Handles flag versioning
- Supports targeted flag delivery
- Implements flag caching
- Manages flag distribution

**Heartbeat API**
- Receives heartbeat data
- Updates device status
- Stores historical metrics
- Implements anomaly detection
- Generates health alerts

**Queue API**
- Receives queued events
- Processes screenshot metadata
- Stores health reports
- Manages event deduplication
- Implements batch processing

**Configuration API**
- Serves agent configuration
- Handles configuration versioning
- Supports dynamic configuration updates
- Implements configuration validation
- Manages configuration distribution

**CRM Dashboard API**
- Aggregates device statistics
- Provides real-time device status
- Generates health reports
- Implements analytics queries
- Supports export functionality

### 1.3 Integration Points

**Existing System Integration**

1. **Queue System Integration**
   - Queue Sync Service uses existing IJobQueue
   - Screenshot metadata queued via existing job system
   - Offline events queued via existing job system
   - Retry Engine uses existing job retry logic

2. **Event Bus Integration**
   - Policy changes published via IEventBus
   - Feature flag changes published via IEventBus
   - Network status changes published via IEventBus
   - Sync status events published via IEventBus

3. **SQLite Integration**
   - Policies cached in existing SQLite database
   - Feature flags cached in existing SQLite database
   - Offline events stored in existing SQLite database
   - Device credentials stored in existing SQLite database

4. **Policy Engine Integration**
   - Heartbeat intervals controlled by Policy Engine
   - Sync intervals controlled by Policy Engine
   - Retry limits controlled by Policy Engine
   - Feature flag behavior controlled by Policy Engine

5. **Health Monitor Integration**
   - Backend connectivity monitored by Health Monitor
   - Sync status monitored by Health Monitor
   - Queue health monitored by Health Monitor
   - Database health monitored by Health Monitor

6. **Storage Infrastructure Integration**
   - Policies stored in Config folder
   - Feature flags stored in Config folder
   - Offline events stored in Queue folder
   - Logs stored in Logs folder

### 1.4 Performance Architecture

**Scalability Requirements**
- Support 100,000+ concurrent agents
- Handle millions of queued events
- Maintain low CPU/RAM footprint per agent
- Support offline mode for extended periods
- Implement efficient reconnection logic

**Performance Optimizations**
- Batch API calls for multiple events
- Compress large payloads before transmission
- Implement delta sync for policies and flags
- Use connection pooling for HTTP clients
- Cache frequently accessed data in memory
- Implement lazy loading for large datasets

**Resource Management**
- Limit memory usage per agent (< 200MB)
- Limit CPU usage (< 5% idle, < 20% active)
- Implement efficient garbage collection
- Use async/await for all I/O operations
- Implement cancellation tokens for long-running operations

### 1.5 Security Architecture

**Authentication Security**
- JWT tokens with short expiration (15 minutes)
- Refresh tokens with longer expiration (7 days)
- Secure token storage in Windows Credential Manager
- Token validation on every API call
- Automatic token refresh before expiration

**Transport Security**
- HTTPS only for all API calls
- Certificate validation
- TLS 1.2+ requirement
- Certificate pinning for production
- HSTS support

**Data Security**
- Encrypted local storage for sensitive data
- Device fingerprint validation
- Replay attack prevention
- Request signing for critical operations
- Data encryption at rest in SQLite

**Device Security**
- Device validation on every request
- Fingerprint verification
- IP-based restrictions
- Device blacklisting support
- Unauthorized device detection

## 2. Communication Flow

### 2.1 Agent to Backend Communication

**Request Flow**

```
Agent Service
    ↓
API Client (Polly Retry)
    ↓
HTTP Client (Connection Pool)
    ↓
HTTPS (TLS 1.2+)
    ↓
Backend API (Express)
    ↓
Middleware (Auth, Rate Limit, Validation)
    ↓
Controller
    ↓
Service Layer
    ↓
Repository Layer
    ↓
PostgreSQL (Supabase)
```

**Response Flow**

```
PostgreSQL
    ↓
Repository Layer
    ↓
Service Layer
    ↓
Controller
    ↓
Middleware (Response Formatting)
    ↓
HTTPS Response
    ↓
HTTP Client
    ↓
API Client (Response Parsing)
    ↓
Agent Service
```

### 2.2 Authentication Flow

```
┌──────────┐
│   User   │
└────┬─────┘
     │ Email/Password
     ↓
┌──────────────────┐
│ LoginViewModel   │
└────┬─────────────┘
     │ LoginCommand
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Validate Input     │
│ - Call Backend API  │
└────┬─────────────────┘
     │ POST /api/agent/login
     ↓
┌──────────────────┐
│ Backend Auth API │
│ - Validate Creds │
│ - Generate JWT   │
└────┬─────────────┘
     │ JWT Response
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Store Access Token │
│ - Store Refresh Token│
│ - Store Identity     │
└────┬─────────────────┘
     │ Success
     ↓
┌──────────────────┐
│ Device Reg      │
│ Service         │
└────┬─────────────┘
     │ Register Device
     ↓
┌──────────────────┐
│ Backend Device   │
│ API              │
└────┬─────────────┘
     │ Device ID
     ↓
┌──────────────────┐
│ Policy Sync      │
│ Service          │
└────┬─────────────┘
     │ Download Policies
     ↓
┌──────────────────┐
│ Backend Policy   │
│ API              │
└────┬─────────────┘
     │ Policies
     ↓
┌──────────────────┐
│ Feature Flag     │
│ Service          │
└────┬─────────────┘
     │ Download Flags
     ↓
┌──────────────────┐
│ Backend Feature  │
│ Flag API         │
└────┬─────────────┘
     │ Flags
     ↓
┌──────────────────┐
│ Heartbeat        │
│ Service          │
└────┬─────────────┘
     │ Start Loop
     ↓
┌──────────────────┐
│ Shell View       │
└──────────────────┘
```

### 2.3 Token Refresh Flow

```
┌──────────────────────┐
│ Auth Sync Service    │
│ - Check Token Expiry │
│ - If < 5 min left   │
└────┬─────────────────┘
     │ Refresh Required
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Get Refresh Token  │
│ - Call Refresh API   │
└────┬─────────────────┘
     │ POST /api/agent/refresh-token
     ↓
┌──────────────────┐
│ Backend Auth API │
│ - Validate Token │
│ - Generate New   │
└────┬─────────────┘
     │ New JWT
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Update Access Token│
│ - Update Refresh Token│
│ - Update Expiry      │
└────┬─────────────────┘
     │ Success
     ↓
┌──────────────────────┐
│ Continue Operations  │
└──────────────────────┘
```

### 2.4 Device Registration Flow

```
┌──────────────────────┐
│ Device Reg Service    │
│ - Generate Fingerprint│
│ - Get System Info    │
└────┬─────────────────┘
     │ Device Info
     ↓
┌──────────────────────┐
│ Device Reg Service    │
│ - Check Local Storage │
│ - If Device ID exists │
└────┬─────────────────┘
     │ New Device
     ↓
┌──────────────────────┐
│ Device Reg Service    │
│ - Call Register API  │
└────┬─────────────────┘
     │ POST /api/agent/register-device
     ↓
┌──────────────────┐
│ Backend Device   │
│ API              │
│ - Validate Device│
│ - Create Record  │
└────┬─────────────┘
     │ Device ID
     ↓
┌──────────────────────┐
│ Device Reg Service    │
│ - Store Device ID    │
│ - Store Company ID   │
│ - Store Employee ID  │
└────┬─────────────────┘
     │ Success
     ↓
┌──────────────────────┐
│ Continue Startup     │
└──────────────────────┘
```

### 2.5 Policy Synchronization Flow

```
┌──────────────────────┐
│ Policy Sync Service  │
│ - Check Last Sync    │
│ - If Interval Passed │
└────┬─────────────────┘
     │ Sync Required
     ↓
┌──────────────────────┐
│ Policy Sync Service  │
│ - Get Local Version  │
│ - Call Policy API    │
└────┬─────────────────┘
     │ GET /api/agent/policies
     ↓
┌──────────────────┐
│ Backend Policy   │
│ API              │
│ - Get Latest     │
│ - Return Version │
└────┬─────────────┘
     │ Policies
     ↓
┌──────────────────────┐
│ Policy Sync Service  │
│ - Compare Versions   │
│ - If Changed        │
└────┬─────────────────┘
     │ Update Required
     ↓
┌──────────────────────┐
│ Policy Sync Service  │
│ - Update SQLite     │
│ - Publish Event     │
│ - Update Version    │
└────┬─────────────────┘
     │ Success
     ↓
┌──────────────────────┐
│ Wait for Next Interval│
└──────────────────────┘
```

### 2.6 Heartbeat Flow

```
┌──────────────────────┐
│ Heartbeat Service    │
│ - Start Timer        │
│ - Wait Interval      │
└────┬─────────────────┘
     │ Interval Passed
     ↓
┌──────────────────────┐
│ Heartbeat Service    │
│ - Collect CPU        │
│ - Collect RAM        │
│ - Collect Disk       │
│ - Collect Storage    │
│ - Get Queue Size     │
│ - Get DB Health      │
│ - Get Worker Status  │
└────┬─────────────────┘
     │ Metrics Collected
     ↓
┌──────────────────────┐
│ Heartbeat Service    │
│ - Call Heartbeat API │
└────┬─────────────────┘
     │ POST /api/agent/heartbeat
     ↓
┌──────────────────┐
│ Backend Heart    │
│ Beat API         │
│ - Store Metrics  │
│ - Update Status  │
└────┬─────────────┘
     │ Success
     ↓
┌──────────────────────┐
│ Wait for Next Interval│
└──────────────────────┘
```

## 3. API Contracts

### 3.1 Authentication API

#### POST /api/agent/login

**Request**
```json
{
  "email": "string",
  "password": "string",
  "clientVersion": "string",
  "environment": "string",
  "deviceFingerprint": "string"
}
```

**Response**
```json
{
  "success": true,
  "data": {
    "accessToken": "string",
    "refreshToken": "string",
    "expiresIn": 900,
    "employeeId": "string",
    "companyId": "string",
    "deviceId": "string",
    "configVersion": "string",
    "requiresDeviceRegistration": false
  }
}
```

#### POST /api/agent/refresh-token

**Request**
```json
{
  "refreshToken": "string",
  "deviceId": "string"
}
```

**Response**
```json
{
  "success": true,
  "data": {
    "accessToken": "string",
    "refreshToken": "string",
    "expiresIn": 900
  }
}
```

#### POST /api/agent/logout

**Request**
```json
{
  "deviceId": "string"
}
```

**Response**
```json
{
  "success": true
}
```

### 3.2 Device Registration API

#### POST /api/agent/register-device

**Request**
```json
{
  "employeeId": "string",
  "companyId": "string",
  "fingerprint": "string",
  "hostname": "string",
  "osVersion": "string",
  "architecture": "string",
  "agentVersion": "string",
  "ipAddress": "string"
}
```

**Response**
```json
{
  "success": true,
  "data": {
    "deviceId": "string",
    "registeredAt": "datetime",
    "status": "active"
  }
}
```

#### PUT /api/agent/device/{deviceId}

**Request**
```json
{
  "hostname": "string",
  "osVersion": "string",
  "agentVersion": "string",
  "ipAddress": "string"
}
```

**Response**
```json
{
  "success": true,
  "data": {
    "deviceId": "string",
    "updatedAt": "datetime"
  }
}
```

#### GET /api/agent/device/{deviceId}

**Response**
```json
{
  "success": true,
  "data": {
    "deviceId": "string",
    "employeeId": "string",
    "companyId": "string",
    "hostname": "string",
    "osVersion": "string",
    "architecture": "string",
    "agentVersion": "string",
    "status": "active",
    "lastHeartbeat": "datetime",
    "registeredAt": "datetime"
  }
}
```

### 3.3 Policy API

#### GET /api/agent/policies

**Query Parameters**
- `version` (optional): Current local version for delta sync

**Response**
```json
{
  "success": true,
  "data": {
    "version": "string",
    "policies": [
      {
        "id": "string",
        "name": "string",
        "type": "screenshot|heartbeat|queue|feature",
        "config": {},
        "enabled": true,
        "priority": 0
      }
    ],
    "syncInterval": 300,
    "effectiveAt": "datetime"
  }
}
```

#### GET /api/agent/policies/{policyId}

**Response**
```json
{
  "success": true,
  "data": {
    "id": "string",
    "name": "string",
    "type": "string",
    "config": {},
    "enabled": true,
    "priority": 0,
    "version": "string",
    "createdAt": "datetime",
    "updatedAt": "datetime"
  }
}
```

### 3.4 Feature Flag API

#### GET /api/agent/feature-flags

**Query Parameters**
- `version` (optional): Current local version for delta sync

**Response**
```json
{
  "success": true,
  "data": {
    "version": "string",
    "flags": [
      {
        "id": "string",
        "name": "string",
        "enabled": true,
        "targeting": {},
        "rolloutPercentage": 100,
        "value": {}
      }
    ],
    "syncInterval": 60
  }
}
```

#### GET /api/agent/feature-flags/{flagId}

**Response**
```json
{
  "success": true,
  "data": {
    "id": "string",
    "name": "string",
    "enabled": true,
    "targeting": {},
    "rolloutPercentage": 100,
    "value": {},
    "version": "string"
  }
}
```

### 3.5 Heartbeat API

#### POST /api/agent/heartbeat

**Request**
```json
{
  "deviceId": "string",
  "employeeId": "string",
  "companyId": "string",
  "timestamp": "datetime",
  "metrics": {
    "cpu": {
      "usage": 0.5,
      "cores": 4
    },
    "memory": {
      "total": 16384,
      "used": 8192,
      "available": 8192
    },
    "disk": {
      "total": 512000,
      "used": 256000,
      "available": 256000
    },
    "storage": {
      "total": 1024000,
      "used": 512000,
      "available": 512000
    }
  },
  "status": {
    "screenshotWorker": "active|idle|error",
    "queueSize": 100,
    "databaseHealth": "healthy|degraded|error",
    "lastSync": "datetime",
    "agentVersion": "string"
  }
}
```

**Response**
```json
{
  "success": true,
  "data": {
    "receivedAt": "datetime",
    "nextHeartbeat": 60
  }
}
```

### 3.6 Queue Synchronization API

#### POST /api/agent/queue/sync

**Request**
```json
{
  "deviceId": "string",
  "events": [
    {
      "id": "string",
      "type": "screenshot|heartbeat|log|error",
      "timestamp": "datetime",
      "data": {}
    }
  ],
  "batchId": "string"
}
```

**Response**
```json
{
  "success": true,
  "data": {
    "processed": 10,
    "failed": 0,
    "batchId": "string"
  }
}
```

#### GET /api/agent/queue/history

**Query Parameters**
- `deviceId`: Device ID
- `fromDate`: Start date
- `toDate`: End date
- `limit`: Page size
- `offset`: Page offset

**Response**
```json
{
  "success": true,
  "data": {
    "events": [
      {
        "id": "string",
        "type": "string",
        "timestamp": "datetime",
        "status": "processed|failed|pending"
      }
    ],
    "total": 1000,
    "page": 1,
    "pageSize": 50
  }
}
```

### 3.7 Configuration API

#### GET /api/agent/config

**Query Parameters**
- `version` (optional): Current local version

**Response**
```json
{
  "success": true,
  "data": {
    "version": "string",
    "config": {
      "heartbeatInterval": 60,
      "syncInterval": 300,
      "screenshotInterval": 300,
      "maxQueueSize": 10000,
      "offlineMode": true,
      "retryLimit": 3,
      "compressionEnabled": true
    },
    "effectiveAt": "datetime"
  }
}
```

### 3.8 Screenshot Metadata API

#### POST /api/agent/screenshots/metadata

**Request**
```json
{
  "deviceId": "string",
  "employeeId": "string",
  "screenshotId": "string",
  "timestamp": "datetime",
  "filePath": "string",
  "fileSize": 1024,
  "width": 1920,
  "height": 1080,
  "format": "jpeg",
  "compression": 0.8,
  "hash": "string"
}
```

**Response**
```json
{
  "success": true,
  "data": {
    "screenshotId": "string",
    "uploadUrl": "string",
    "uploadedAt": "datetime"
  }
}
```

### 3.9 Health Report API

#### POST /api/agent/health-report

**Request**
```json
{
  "deviceId": "string",
  "employeeId": "string",
  "timestamp": "datetime",
  "report": {
    "overallHealth": "healthy|warning|critical",
    "components": [
      {
        "name": "string",
        "status": "healthy|warning|critical",
        "message": "string",
        "metrics": {}
      }
    ],
    "errors": [
      {
        "code": "string",
        "message": "string",
        "timestamp": "datetime"
      }
    ]
  }
}
```

**Response**
```json
{
  "success": true,
  "data": {
    "reportId": "string",
    "receivedAt": "datetime"
  }
}
```

### 3.10 CRM Dashboard API

#### GET /api/crm/devices

**Query Parameters**
- `companyId`: Company ID
- `status`: Filter by status
- `page`: Page number
- `limit`: Page size

**Response**
```json
{
  "success": true,
  "data": {
    "devices": [
      {
        "deviceId": "string",
        "employeeId": "string",
        "employeeName": "string",
        "hostname": "string",
        "osVersion": "string",
        "agentVersion": "string",
        "status": "online|offline|error",
        "lastHeartbeat": "datetime",
        "storageUsage": 0.5,
        "screenshotCount": 1000,
        "queueSize": 50,
        "healthStatus": "healthy|warning|critical",
        "policyVersion": "string"
      }
    ],
    "total": 1000,
    "online": 800,
    "offline": 200,
    "page": 1,
    "pageSize": 50
  }
}
```

#### GET /api/crm/devices/{deviceId}/details

**Response**
```json
{
  "success": true,
  "data": {
    "deviceId": "string",
    "employeeId": "string",
    "employeeName": "string",
    "companyId": "string",
    "hostname": "string",
    "osVersion": "string",
    "architecture": "string",
    "agentVersion": "string",
    "ipAddress": "string",
    "status": "online|offline|error",
    "registeredAt": "datetime",
    "lastHeartbeat": "datetime",
    "storage": {
      "total": 1024000,
      "used": 512000,
      "available": 512000,
      "usagePercentage": 50
    },
    "screenshotCount": 1000,
    "queueSize": 50,
    "healthStatus": "healthy|warning|critical",
    "policyVersion": "string",
    "featureFlags": {},
    "metrics": {
      "cpu": 0.5,
      "memory": 0.5,
      "disk": 0.5
    },
    "recentErrors": [
      {
        "code": "string",
        "message": "string",
        "timestamp": "datetime"
      }
    ]
  }
}
```

#### GET /api/crm/statistics

**Query Parameters**
- `companyId`: Company ID
- `fromDate`: Start date
- `toDate`: End date

**Response**
```json
{
  "success": true,
  "data": {
    "totalDevices": 1000,
    "onlineDevices": 800,
    "offlineDevices": 200,
    "healthyDevices": 750,
    "warningDevices": 40,
    "criticalDevices": 10,
    "totalScreenshots": 1000000,
    "totalEvents": 5000000,
    "averageQueueSize": 50,
    "averageCpuUsage": 0.3,
    "averageMemoryUsage": 0.4,
    "storageUsage": 0.5
  }
}
```

## 4. Sync Flow

### 4.1 Policy Sync Flow

```
┌──────────────────────┐
│ Policy Sync Service  │
│ - Check Last Sync    │
│ - Get Interval from  │
│   Policy Engine      │
└────┬─────────────────┘
     │ Interval Passed
     ↓
┌──────────────────────┐
│ Policy Sync Service  │
│ - Get Local Version  │
│ - Call Backend API   │
│ - Include Version    │
└────┬─────────────────┘
     │ GET /api/agent/policies?version=X
     ↓
┌──────────────────┐
│ Backend Policy   │
│ API              │
│ - Compare Version │
│ - If Same        │
│   Return 304     │
│ - If Different   │
│   Return Delta   │
└────┬─────────────┘
     │ Response
     ↓
┌──────────────────────┐
│ Policy Sync Service  │
│ - If 304             │
│   Skip Update        │
│ - If 200             │
│   Update SQLite      │
│   Publish Event      │
│   Update Version     │
└────┬─────────────────┘
     │ Success
     ↓
┌──────────────────────┐
│ Wait for Next Interval│
└──────────────────────┘
```

### 4.2 Feature Flag Sync Flow

```
┌──────────────────────────┐
│ Feature Flag Sync Service│
│ - Check Last Sync        │
│ - Get Interval from      │
│   Policy Engine          │
└────┬─────────────────────┘
     │ Interval Passed
     ↓
┌──────────────────────────┐
│ Feature Flag Sync Service│
│ - Get Local Version      │
│ - Call Backend API       │
│ - Include Version        │
└────┬─────────────────────┘
     │ GET /api/agent/feature-flags?version=X
     ↓
┌──────────────────┐
│ Backend Feature  │
│ Flag API         │
│ - Compare Version│
│ - If Same       │
│   Return 304    │
│ - If Different  │
│   Return Delta  │
└────┬─────────────┘
     │ Response
     ↓
┌──────────────────────────┐
│ Feature Flag Sync Service│
│ - If 304                 │
│   Skip Update            │
│ - If 200                 │
│   Update Memory Cache    │
│   Publish Event          │
│   Update Version         │
└────┬─────────────────────┘
     │ Success
     ↓
┌──────────────────────────┐
│ Wait for Next Interval   │
└──────────────────────────┘
```

### 4.3 Configuration Sync Flow

```
┌──────────────────────┐
│ Config Sync Service │
│ - Check Last Sync   │
│ - Get Interval from │
│   Policy Engine     │
└────┬─────────────────┘
     │ Interval Passed
     ↓
┌──────────────────────┐
│ Config Sync Service  │
│ - Get Local Version  │
│ - Call Backend API   │
│ - Include Version    │
└────┬─────────────────┘
     │ GET /api/agent/config?version=X
     ↓
┌──────────────────┐
│ Backend Config   │
│ API              │
│ - Compare Version│
│ - If Same       │
│   Return 304    │
│ - If Different  │
│   Return Delta  │
└────┬─────────────┘
     │ Response
     ↓
┌──────────────────────┐
│ Config Sync Service  │
│ - If 304             │
│   Skip Update        │
│ - If 200             │
│   Update SQLite      │
│   Publish Event      │
│   Update Version     │
│ - Apply New Config   │
└────┬─────────────────┘
     │ Success
     ↓
┌──────────────────────┐
│ Wait for Next Interval│
└──────────────────────┘
```

### 4.4 Delta Sync Strategy

**Delta Sync Logic**

1. **Version Comparison**
   - Agent sends current local version
   - Backend compares with server version
   - If same, return HTTP 304 Not Modified
   - If different, return only changed items

2. **Change Detection**
   - Backend tracks last modified timestamp per item
   - Returns items modified after agent's last sync
   - Includes deleted items list

3. **Conflict Resolution**
   - Server version always wins
   - Local changes are overwritten
   - Conflict events published to Event Bus

4. **Batch Processing**
   - Large deltas paginated
   - Agent requests pages sequentially
   - Each page processed before next request

## 5. Queue Flow

### 5.1 Event Queuing Flow

```
┌──────────────────┐
│ Event Source     │
│ (Screenshot,     │
│  Heartbeat,      │
│  Log, Error)     │
└────┬─────────────┘
     │ Event Created
     ↓
┌──────────────────────┐
│ Queue Sync Service   │
│ - Check Network     │
│ - If Online         │
└────┬─────────────────┘
     │ Online
     ↓
┌──────────────────────┐
│ Queue Sync Service   │
│ - Add to IJobQueue   │
│ - Set Priority      │
│ - Set Retry Count    │
└────┬─────────────────┘
     │ Queued
     ↓
┌──────────────────────┐
│ Queue Worker         │
│ - Dequeue Job        │
│ - Process Job        │
└────┬─────────────────┘
     │ Processing
     ↓
┌──────────────────────┐
│ API Client           │
│ - Send to Backend    │
│ - Apply Retry Engine │
└────┬─────────────────┘
     │ API Call
     ↓
┌──────────────────┐
│ Backend Queue    │
│ API              │
└────┬─────────────┘
     │ Response
     ↓
┌──────────────────────┐
│ Queue Worker         │
│ - If Success         │
│   Mark Complete      │
│ - If Failed         │
│   Retry or Dead Letter│
└────┬─────────────────┘
     │ Complete
     ↓
┌──────────────────────┐
│ Remove from Queue    │
└──────────────────────┘
```

### 5.2 Offline Queue Flow

```
┌──────────────────┐
│ Event Source     │
└────┬─────────────┘
     │ Event Created
     ↓
┌──────────────────────┐
│ Queue Sync Service   │
│ - Check Network     │
│ - If Offline        │
└────┬─────────────────┘
     │ Offline
     ↓
┌──────────────────────┐
│ Queue Sync Service   │
│ - Store in SQLite   │
│ - Mark as Pending   │
│ - Add Timestamp     │
└────┬─────────────────┘
     │ Stored
     ↓
┌──────────────────────┐
│ Offline Manager      │
│ - Monitor Network   │
│ - Detect Online     │
└────┬─────────────────┘
     │ Online Detected
     ↓
┌──────────────────────┐
│ Queue Sync Service   │
│ - Load Pending Events│
│ - Sort by Priority  │
│ - Add to IJobQueue   │
└────┬─────────────────┘
     │ Loaded
     ↓
┌──────────────────────┐
│ Queue Worker         │
│ - Process Jobs      │
│ - Apply Retry Engine │
└────┬─────────────────┘
     │ Processed
     ↓
┌──────────────────────┐
│ Update SQLite        │
│ - Mark as Synced     │
│ - Remove from Pending│
└──────────────────────┘
```

### 5.3 Batch Upload Flow

```
┌──────────────────────┐
│ Queue Sync Service   │
│ - Check Queue Size  │
│ - If > Batch Size   │
└────┬─────────────────┘
     │ Batch Ready
     ↓
┌──────────────────────┐
│ Queue Sync Service   │
│ - Collect Batch     │
│ - Compress Payload  │
│ - Create Batch ID   │
└────┬─────────────────┘
     │ Batch Created
     ↓
┌──────────────────────┐
│ API Client           │
│ - Send Batch        │
│ - Apply Retry Engine │
└────┬─────────────────┘
     │ POST /api/agent/queue/sync
     ↓
┌──────────────────┐
│ Backend Queue    │
│ API              │
│ - Process Batch  │
│ - Return Results │
└────┬─────────────┘
     │ Response
     ↓
┌──────────────────────┐
│ Queue Sync Service   │
│ - Process Results   │
│ - Mark Success      │
│ - Retry Failed      │
└────┬─────────────────┘
     │ Complete
     ↓
┌──────────────────────┐
│ Update Queue Status  │
└──────────────────────┘
```

### 5.4 Dead Letter Queue Flow

```
┌──────────────────────┐
│ Queue Worker         │
│ - Process Job        │
│ - Max Retries Reached│
└────┬─────────────────┘
     │ Max Retries
     ↓
┌──────────────────────┐
│ Queue Worker         │
│ - Move to Dead Letter│
│ - Store Error Info  │
│ - Publish Event      │
└────┬─────────────────┘
     │ Dead Letter
     ↓
┌──────────────────────┐
│ DLQ Manager          │
│ - Monitor DLQ       │
│ - Alert on Threshold│
└────┬─────────────────┘
     │ Alert
     ↓
┌──────────────────────┐
│ Admin Intervention   │
│ - Review DLQ        │
│ - Retry or Delete   │
└────┬─────────────────┘
     │ Action Taken
     ↓
┌──────────────────────┐
│ Update DLQ Status    │
└──────────────────────┘
```

## 6. Authentication Flow

### 6.1 Login Flow

```
┌──────────┐
│   User   │
└────┬─────┘
     │ Enter Credentials
     ↓
┌──────────────────┐
│ LoginViewModel   │
│ - Validate Input │
│ - Call Service   │
└────┬─────────────┘
     │ LoginAsync
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Validate Input     │
│ - Check Network      │
└────┬─────────────────┘
     │ Network OK
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Call Backend API  │
│ - Apply Retry Engine │
└────┬─────────────────┘
     │ POST /api/agent/login
     ↓
┌──────────────────┐
│ Backend Auth API │
│ - Validate Email │
│ - Validate Password│
│ - Generate JWT   │
└────┬─────────────┘
     │ Success
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Parse Response    │
│ - Store Access Token │
│ - Store Refresh Token│
│ - Store Identity    │
│ - Set Expiry Timer  │
└────┬─────────────────┘
     │ Stored
     ↓
┌──────────────────────┐
│ Device Reg Service   │
│ - Check Device ID    │
└────┬─────────────────┘
     │ Device Exists
     ↓
┌──────────────────────┐
│ Continue Startup     │
└──────────────────────┘
```

### 6.2 Token Refresh Flow

```
┌──────────────────────┐
│ Auth Sync Service    │
│ - Check Token Expiry │
│ - If < 5 min left   │
└────┬─────────────────┘
     │ Refresh Required
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Get Refresh Token  │
│ - Check Network      │
└────┬─────────────────┘
     │ Network OK
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Call Refresh API  │
│ - Apply Retry Engine │
└────┬─────────────────┘
     │ POST /api/agent/refresh-token
     ↓
┌──────────────────┐
│ Backend Auth API │
│ - Validate Token │
│ - Generate New   │
└────┬─────────────┘
     │ Success
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Update Access Token│
│ - Update Refresh Token│
│ - Update Expiry      │
│ - Reset Timer       │
└────┬─────────────────┘
     │ Updated
     ↓
┌──────────────────────┐
│ Continue Operations  │
└──────────────────────┘
```

### 6.3 Automatic Re-authentication Flow

```
┌──────────────────────┐
│ Auth Sync Service    │
│ - API Call Failed    │
│ - 401 Unauthorized   │
└────┬─────────────────┘
     │ Token Expired
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Try Refresh Token │
└────┬─────────────────┘
     │ Refresh Failed
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Clear Tokens      │
│ - Publish Event     │
│ - Redirect to Login │
└────┬─────────────────┘
     │ Logout
     ↓
┌──────────────────┐
│ Login Window     │
└──────────────────┘
```

### 6.4 Credential Storage Flow

```
┌──────────────────────┐
│ Auth Sync Service    │
│ - Receive Tokens     │
└────┬─────────────────┘
     │ Store
     ↓
┌──────────────────────┐
│ Windows Credential   │
│ Manager              │
│ - Encrypt Tokens    │
│ - Store Securely    │
└────┬─────────────────┘
     │ Stored
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Set Expiry Timer   │
│ - Start Refresh Loop│
└────┬─────────────────┘
     │ Running
     ↓
┌──────────────────────┐
│ Auth Sync Service    │
│ - Retrieve Tokens    │
└────┬─────────────────┘
     │ Retrieve
     ↓
┌──────────────────────┐
│ Windows Credential   │
│ Manager              │
│ - Decrypt Tokens    │
│ - Return to Service │
└────┬─────────────────┘
     │ Returned
     ↓
┌──────────────────────┐
│ Use in API Calls    │
└──────────────────────┘
```

## 7. Offline Recovery Flow

### 7.1 Offline Detection Flow

```
┌──────────────────────┐
│ Network Monitor      │
│ - Ping Backend      │
│ - Check Connectivity│
└────┬─────────────────┘
     │ Network Lost
     ↓
┌──────────────────────┐
│ Offline Manager      │
│ - Set Offline Mode  │
│ - Publish Event     │
└────┬─────────────────┘
     │ Offline Mode
     ↓
┌──────────────────────┐
│ Queue Sync Service   │
│ - Store Events Locally│
│ - Disable API Calls  │
└────┬─────────────────┘
     │ Storing
     ↓
┌──────────────────────┐
│ Policy Sync Service  │
│ - Use Cached Data   │
│ - Disable Sync      │
└────┬─────────────────┘
     │ Using Cache
     ↓
┌──────────────────────┐
│ Heartbeat Service    │
│ - Pause Heartbeat    │
│ - Store Locally     │
└────┬─────────────────┘
     │ Paused
     ↓
┌──────────────────────┐
│ Feature Flag Service │
│ - Use Cached Flags  │
└──────────────────────┘
```

### 7.2 Online Recovery Flow

```
┌──────────────────────┐
│ Network Monitor      │
│ - Detect Network     │
│ - Ping Backend      │
└────┬─────────────────┘
     │ Network Restored
     ↓
┌──────────────────────┐
│ Offline Manager      │
│ - Set Online Mode   │
│ - Publish Event     │
└────┬─────────────────┘
     │ Online Mode
     ↓
┌──────────────────────┐
│ Queue Sync Service   │
│ - Load Pending Events│
│ - Sort by Priority  │
│ - Add to IJobQueue   │
└────┬─────────────────┘
     │ Loading
     ↓
┌──────────────────────┐
│ Queue Worker         │
│ - Process Jobs      │
│ - Apply Retry Engine │
└────┬─────────────────┘
     │ Processing
     ↓
┌──────────────────────┐
│ Policy Sync Service  │
│ - Check for Updates │
│ - Sync Policies     │
└────┬─────────────────┘
     │ Syncing
     ↓
┌──────────────────────┐
│ Feature Flag Service │
│ - Check for Updates │
│ - Sync Flags        │
└────┬─────────────────┘
     │ Syncing
     ↓
┌──────────────────────┐
│ Heartbeat Service    │
│ - Resume Heartbeat   │
│ - Send Pending Data │
└────┬─────────────────┘
     │ Resumed
     ↓
┌──────────────────────┐
│ Normal Operation     │
└──────────────────────┘
```

### 7.3 Offline Event Storage Flow

```
┌──────────────────┐
│ Event Source     │
└────┬─────────────┘
     │ Event Created
     ↓
┌──────────────────────┐
│ Queue Sync Service   │
│ - Check Network     │
│ - If Offline        │
└────┬─────────────────┘
     │ Offline
     ↓
┌──────────────────────┐
│ Queue Sync Service   │
│ - Serialize Event   │
│ - Add Timestamp     │
│ - Add Priority     │
└────┬─────────────────┘
     │ Serialized
     ↓
┌──────────────────────┐
│ SQLite Database     │
│ - Insert to Queue   │
│ - Mark as Pending   │
└────┬─────────────────┘
     │ Stored
     ↓
┌──────────────────────┐
│ Update Queue Count   │
└──────────────────────┘
```

### 7.4 Offline Data Integrity Flow

```
┌──────────────────────┐
│ Offline Manager      │
│ - Monitor Queue Size │
│ - If > Threshold    │
└────┬─────────────────┘
     │ Threshold Exceeded
     ↓
┌──────────────────────┐
│ Offline Manager      │
│ - Compress Old Data │
│ - Archive to Disk   │
└────┬─────────────────┘
     │ Archived
     ↓
┌──────────────────────┐
│ Offline Manager      │
│ - Check Disk Space  │
│ - If Low Space      │
└────┬─────────────────┘
     │ Low Space
     ↓
┌──────────────────────┐
│ Offline Manager      │
│ - Delete Oldest     │
│ - Alert User        │
└────┬─────────────────┘
     │ Cleaned
     ↓
┌──────────────────────┐
│ Continue Storing     │
└──────────────────────┘
```

## 8. Retry Strategy

### 8.1 Exponential Backoff Strategy

**Backoff Formula**
```
delay = min(initial_delay * (2 ^ attempt), max_delay)
```

**Configuration**
- Initial Delay: 1 second
- Max Delay: 60 seconds
- Max Retries: 3 (configurable per operation type)
- Jitter: ±25% random

**Retry Logic**
```
Attempt 1: 1s delay
Attempt 2: 2s delay
Attempt 3: 4s delay
Attempt 4: 8s delay
Attempt 5: 16s delay
Attempt 6: 32s delay
Attempt 7: 60s delay (max)
```

### 8.2 Retry Flow

```
┌──────────────────────┐
│ API Client           │
│ - Make Request      │
└────┬─────────────────┘
     │ Request Failed
     ↓
┌──────────────────────┐
│ Retry Engine         │
│ - Check Retry Count  │
│ - If < Max Retries  │
└────┬─────────────────┘
     │ Retry Available
     ↓
┌──────────────────────┐
│ Retry Engine         │
│ - Calculate Delay   │
│ - Apply Jitter      │
└────┬─────────────────┘
     │ Delay Calculated
     ↓
┌──────────────────────┐
│ Retry Engine         │
│ - Wait Delay        │
│ - Check Cancellation│
└────┬─────────────────┘
     │ Delay Complete
     ↓
┌──────────────────────┐
│ API Client           │
│ - Retry Request     │
└────┬─────────────────┘
     │ Success
     ↓
┌──────────────────────┐
│ Return Success       │
└──────────────────────┘
```

### 8.3 Dead Letter Queue Flow

```
┌──────────────────────┐
│ Retry Engine         │
│ - Max Retries Reached│
└────┬─────────────────┘
     │ Max Retries
     ↓
┌──────────────────────┐
│ Retry Engine         │
│ - Move to DLQ       │
│ - Store Error Info  │
│ - Store Timestamp   │
└────┬─────────────────┘
     │ Moved to DLQ
     ↓
┌──────────────────────┐
│ DLQ Manager          │
│ - Monitor DLQ Size  │
│ - Alert on Threshold│
└────┬─────────────────┘
     │ Alert
     ↓
┌──────────────────────┐
│ Admin Action         │
│ - Review DLQ        │
│ - Retry or Delete   │
└────┬─────────────────┘
     │ Action Taken
     ↓
┌──────────────────────┐
│ Update DLQ Status    │
└──────────────────────┘
```

### 8.4 Cancellation Token Flow

```
┌──────────────────────┐
│ Operation Started    │
│ - Create CTS        │
│ - Pass Token        │
└────┬─────────────────┘
     │ Running
     ↓
┌──────────────────────┐
│ User Cancels         │
│ - Call Cancel()     │
└────┬─────────────────┘
     │ Cancelled
     ↓
┌──────────────────────┐
│ Retry Engine         │
│ - Check Token       │
│ - If Cancelled     │
└────┬─────────────────┘
     │ Cancelled
     ↓
┌──────────────────────┐
│ Retry Engine         │
│ - Stop Retries      │
│ - Cleanup Resources │
│ - Throw Exception   │
└────┬─────────────────┘
     │ Cleanup
     ↓
┌──────────────────────┐
│ Operation Aborted    │
└──────────────────────┘
```

### 8.5 Timeout Management Flow

```
┌──────────────────────┐
│ API Client           │
│ - Set Timeout       │
│ - Start Timer       │
└────┬─────────────────┘
     │ Request Sent
     ↓
┌──────────────────────┐
│ Timer                │
│ - Monitor Duration   │
│ - If Timeout       │
└────┬─────────────────┘
     │ Timeout
     ↓
┌──────────────────────┐
│ API Client           │
│ - Cancel Request    │
│ - Throw Timeout     │
└────┬─────────────────┘
     │ Timeout Exception
     ↓
┌──────────────────────┐
│ Retry Engine         │
│ - Catch Timeout     │
│ - Apply Retry Logic │
└────┬─────────────────┘
     │ Retry or Fail
     ↓
┌──────────────────────┐
│ Return Result        │
└──────────────────────┘
```

## 9. Security Architecture

### 9.1 Authentication Security

**JWT Token Structure**
```
Header: {
  "alg": "RS256",
  "typ": "JWT"
}

Payload: {
  "sub": "employee_id",
  "aud": "rdcs-agent",
  "iss": "rdcs-api",
  "exp": 1234567890,
  "iat": 1234567890,
  "device_id": "device_id",
  "company_id": "company_id"
}

Signature: RS256 with private key
```

**Token Lifecycle**
1. **Access Token**: 15 minutes expiration
2. **Refresh Token**: 7 days expiration
3. **Refresh Window**: 5 minutes before expiry
4. **Grace Period**: 1 minute after expiry

**Token Storage**
- Windows Credential Manager
- Encrypted with DPAPI
- Per-device storage
- Automatic cleanup on logout

### 9.2 Transport Security

**HTTPS Configuration**
- TLS 1.2+ required
- Certificate validation enabled
- Certificate pinning for production
- HSTS support
- Perfect Forward Secrecy

**Certificate Validation**
- Validate certificate chain
- Check expiration
- Verify hostname
- Check revocation (OCSP)
- Pin certificate hash (production)

### 9.3 Data Security

**Encryption at Rest**
- SQLite database encrypted with SQLCipher
- Sensitive data encrypted with AES-256
- Encryption keys stored in Credential Manager
- Key rotation support

**Encryption in Transit**
- All API calls over HTTPS
- Payload compression before encryption
- Signature verification for critical operations

**Device Fingerprint**
- Hardware ID (CPU, Motherboard)
- MAC address
- Hostname
- OS version
- Installation ID
- Hashed with SHA-256

### 9.4 Request Security

**Request Signing**
```
signature = HMAC-SHA256(
  secret_key,
  method + url + timestamp + body_hash
)
```

**Headers**
```
Authorization: Bearer <access_token>
X-Device-ID: <device_id>
X-Request-ID: <guid>
X-Timestamp: <unix_timestamp>
X-Signature: <signature>
```

**Replay Protection**
- Timestamp validation (±5 minutes)
- Request ID tracking
- Nonce for critical operations
- Deduplication in backend

### 9.5 Rate Limiting

**Rate Limits**
- Login: 5 attempts per 15 minutes
- API calls: 100 requests per minute
- Heartbeat: 1 request per minute
- Queue sync: 10 requests per minute

**Rate Limit Headers**
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1234567890
```

### 9.6 Device Validation

**Device Validation Flow**
```
┌──────────────────────┐
│ API Client           │
│ - Add Device ID     │
│ - Add Fingerprint   │
└────┬─────────────────┘
     │ Request Sent
     ↓
┌──────────────────┐
│ Backend API      │
│ - Validate Device│
│ - Check Status   │
└────┬─────────────┘
     │ Device Valid
     ↓
┌──────────────────┐
│ Process Request  │
└──────────────────┘
```

**Device Blacklist**
- Blacklisted devices rejected
- IP-based restrictions
- Geographic restrictions
- Time-based restrictions

## 10. Testing Strategy

### 10.1 Authentication Tests

**Test Cases**
1. Valid credentials login
2. Invalid credentials login
3. Token refresh before expiry
4. Token refresh after expiry
5. Automatic re-authentication
6. Credential storage and retrieval
7. Token expiration handling
8. Concurrent login attempts
9. Rate limiting on login
10. Device validation

**Test Tools**
- xUnit for unit tests
- Moq for mocking
- TestServer for integration tests
- Postman for API testing

### 10.2 Heartbeat Tests

**Test Cases**
1. Successful heartbeat transmission
2. Heartbeat with invalid data
3. Heartbeat during offline mode
4. Heartbeat retry on failure
5. Heartbeat interval accuracy
6. Metrics collection accuracy
7. Database health reporting
8. Queue size reporting
9. Worker status reporting
10. Heartbeat aggregation

### 10.3 Offline Tests

**Test Cases**
1. Offline detection accuracy
2. Online recovery accuracy
3. Event storage during offline
4. Event sync on recovery
5. Data integrity during offline
6. Queue size management
7. Disk space management
8. Compression during offline
9. Archive during offline
10. Recovery from extended offline

### 10.4 Queue Sync Tests

**Test Cases**
1. Event queuing success
2. Event queuing failure
3. Batch upload success
4. Batch upload failure
5. Batch retry logic
6. Dead letter queue
7. Priority handling
8. Deduplication
9. Compression accuracy
10. Large queue handling

### 10.5 Policy Sync Tests

**Test Cases**
1. Initial policy download
2. Policy update detection
3. Delta sync accuracy
4. Version comparison
5. Conflict resolution
6. Cache invalidation
7. Event publishing
8. Offline policy usage
9. Policy rollback
10. Large policy handling

### 10.6 Feature Flag Tests

**Test Cases**
1. Initial flag download
2. Flag update detection
3. Delta sync accuracy
4. Version comparison
5. Hot reload accuracy
6. Targeting accuracy
7. Rollout percentage
8. Offline flag usage
9. Flag rollback
10. Large flag set handling

### 10.7 Device Registration Tests

**Test Cases**
1. New device registration
2. Existing device update
3. Device fingerprint accuracy
4. Device validation
5. Device re-registration
6. Device deregistration
7. Device status update
8. Concurrent registration
9. Invalid device data
10. Device blacklisting

### 10.8 Recovery Tests

**Test Cases**
1. Network recovery detection
2. Event sync on recovery
3. Policy sync on recovery
4. Flag sync on recovery
5. Heartbeat resumption
6. Queue processing resumption
7. Data integrity verification
8. Performance after recovery
9. Multiple recovery cycles
10. Extended offline recovery

### 10.9 Performance Tests

**Test Scenarios**
1. 100,000 concurrent agents
2. 1 million queued events
3. Extended offline period (24 hours)
4. Large policy download (10MB)
5. Large flag set download (1MB)
6. High frequency heartbeat (1 second)
7. Batch upload (10,000 events)
8. Memory usage under load
9. CPU usage under load
10. Network bandwidth usage

**Performance Metrics**
- Memory usage < 200MB per agent
- CPU usage < 5% idle, < 20% active
- API response time < 500ms
- Heartbeat transmission < 100ms
- Policy sync < 5 seconds
- Flag sync < 1 second
- Event sync < 10 seconds per batch

### 10.10 Security Tests

**Test Cases**
1. JWT token validation
2. Token refresh security
3. Credential storage security
4. HTTPS enforcement
5. Certificate validation
6. Request signing
7. Replay attack prevention
8. Rate limiting enforcement
9. Device validation
10. Data encryption at rest

**Security Tools**
- OWASP ZAP for security scanning
- Burp Suite for penetration testing
- SonarQube for code analysis
- NuGet for vulnerability scanning

## 11. CRM Dashboard Architecture

### 11.1 Dashboard Overview

**Purpose**
- Real-time device monitoring
- Health status aggregation
- Performance analytics
- Alert management
- Device management

**Technology Stack**
- React for frontend
- Node.js/Express for backend
- PostgreSQL for data
- WebSocket for real-time updates
- Chart.js for visualization

### 11.2 Dashboard Components

**Device List View**
- Table showing all devices
- Columns: Device ID, Employee, Status, Last Heartbeat, Storage, Screenshots, Health
- Filters: Status, Company, OS Version
- Sorting: All columns
- Pagination: 50 items per page
- Search: By device ID, employee name, hostname

**Device Detail View**
- Device information card
- Real-time metrics (CPU, RAM, Disk)
- Storage usage chart
- Screenshot count trend
- Queue size trend
- Health status timeline
- Recent errors list
- Policy version
- Feature flags

**Statistics View**
- Total devices count
- Online/offline breakdown
- Health status breakdown
- Storage usage aggregate
- Screenshot count aggregate
- Queue size aggregate
- CPU usage average
- RAM usage average
- Disk usage average

**Alert View**
- Real-time alert stream
- Alert severity levels
- Alert filtering
- Alert acknowledgment
- Alert history
- Alert trends

**Map View**
- Geographic device distribution
- Regional status
- Cluster visualization
- Drill-down capability

### 11.3 Real-time Updates

**WebSocket Architecture**
```
┌──────────────────┐
│ Dashboard Client │
└────┬─────────────┘
     │ WebSocket Connection
     ↓
┌──────────────────┐
│ WebSocket Server │
└────┬─────────────┘
     │ Subscribe to Events
     ↓
┌──────────────────┐
│ Event Bus        │
└────┬─────────────┘
     │ Device Events
     ↓
┌──────────────────┐
│ PostgreSQL       │
│ LISTEN/NOTIFY   │
└──────────────────┘
```

**Event Types**
- Device status change
- Heartbeat received
- Health status change
- Alert triggered
- Queue threshold exceeded
- Storage threshold exceeded

### 11.4 Data Aggregation

**Aggregation Strategy**
- Real-time: Last 5 minutes
- Hourly: Last 24 hours
- Daily: Last 30 days
- Monthly: Last 12 months

**Aggregation Tables**
- `device_metrics_hourly`
- `device_metrics_daily`
- `device_metrics_monthly`
- `company_aggregates`
- `global_aggregates`

### 11.5 Performance Optimization

**Caching Strategy**
- Redis for hot data
- CDN for static assets
- Browser caching for UI
- Database query caching

**Query Optimization**
- Indexed columns
- Partitioned tables
- Materialized views
- Query result caching

**Load Balancing**
- Horizontal scaling
- Database read replicas
- Connection pooling
- Request queuing

## Summary

This architecture provides a comprehensive enterprise backend integration for the RDCS Employee Agent. It ensures:

- **Reliability**: Robust retry mechanisms, offline support, data integrity
- **Scalability**: Support for 100,000+ concurrent agents, millions of queued events
- **Security**: JWT authentication, HTTPS encryption, device validation, replay protection
- **Performance**: Low CPU/RAM footprint, efficient sync, batch processing
- **Resilience**: Offline mode, automatic recovery, dead letter queue, exponential backoff
- **Observability**: Heartbeat monitoring, health reports, CRM dashboard, real-time alerts

The architecture integrates seamlessly with existing systems:
- Queue System for event processing
- Event Bus for pub/sub communication
- SQLite for local caching and offline storage
- Policy Engine for configuration management
- Health Monitor for system health tracking
- Storage Infrastructure for file management
