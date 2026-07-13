# Deployment Guide

## Windows Agent Deployment

### Build Configuration

1. **Update Configuration**
   - Edit `config/settings.json` with production API URL
   - Set appropriate environment (production/staging)

2. **Build Release**
   ```powershell
   .\scripts\build.ps1 -Configuration Release
   ```

3. **Output Location**
   - Build artifacts: `src/RDCS.EmployeeAgent.UI/bin/Release/net8.0-windows/`
   - Required files: All DLLs, config files, and executable

### Installation

1. Create installation directory (e.g., `C:\Program Files\RDCS\EmployeeAgent`)
2. Copy all build artifacts to installation directory
3. Create desktop shortcut to `RDCS.EmployeeAgent.UI.exe`
4. Configure Windows Firewall to allow outbound HTTPS traffic

### Deployment Methods

#### MSI Installer (Recommended)
- Use WiX Toolset or Advanced Installer
- Include all dependencies
- Configure auto-start on Windows login
- Create uninstaller

#### XCopy Deployment
- Simple file copy to target machines
- Requires manual configuration
- Suitable for testing environments

## Backend Deployment

### Environment Variables

Set the following environment variables:

```env
DATABASE_URL=postgresql://user:password@host:5432/database
JWT_SECRET=your-production-secret-key
JWT_EXPIRES_IN=1h
REFRESH_TOKEN_EXPIRES_IN=7d
NODE_ENV=production
PORT=3000
```

### Database Setup

1. **Supabase Setup**
   - Create PostgreSQL database
   - Get connection string
   - Set `DATABASE_URL` environment variable

2. **Run Migrations**
   ```bash
   cd backend
   npx prisma migrate deploy
   ```

### Deployment Options

#### Render (Recommended)
- Push backend code to GitHub
- Connect Render to repository
- Configure environment variables
- Deploy as web service

#### Docker
```dockerfile
FROM node:18-alpine
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production
COPY . .
RUN npx prisma generate
EXPOSE 3000
CMD ["npm", "start"]
```

#### VPS/Cloud Server
- Install Node.js 18+
- Clone repository
- Install dependencies
- Configure PM2 for process management
- Set up Nginx reverse proxy
- Configure SSL certificate

### SSL/TLS

- Use HTTPS only in production
- Configure SSL certificate (Let's Encrypt recommended)
- Update Windows Agent API URL to use HTTPS

## Monitoring

### Backend Monitoring
- Use Render's built-in metrics
- Configure application monitoring (Datadog, New Relic)
- Set up log aggregation

### Windows Agent Monitoring
- Logs stored in `%LOCALAPPDATA%\RDCS\EmployeeAgent\logs\`
- Monitor heartbeat intervals
- Track device registration status

## Security Checklist

- [ ] Change default JWT secrets
- [ ] Enable HTTPS only
- [ ] Configure rate limiting
- [ ] Set up database backups
- [ ] Enable audit logging
- [ ] Configure IP whitelisting (if needed)
- [ ] Test device fingerprinting
- [ ] Verify token storage security

## Troubleshooting

### Windows Agent Issues

**Agent won't start**
- Check .NET 8 is installed
- Verify config/settings.json exists
- Check Windows Event Viewer for errors

**Authentication fails**
- Verify API URL is correct
- Check network connectivity
- Review logs in log folder

**Device registration fails**
- Check device fingerprint generation
- Verify backend is accessible
- Review backend logs

### Backend Issues

**Database connection fails**
- Verify DATABASE_URL is correct
- Check PostgreSQL is accessible
- Test connection with Prisma Studio

**API returns 500 errors**
- Check backend logs
- Verify Prisma client is generated
- Test with Postman/curl

## Rollback Procedure

### Windows Agent
1. Uninstall current version
2. Install previous version
3. Restore configuration from backup

### Backend
1. Deploy previous commit
2. Run database rollback if needed
3. Verify API endpoints work
