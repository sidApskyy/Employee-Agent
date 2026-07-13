import { Request, Response } from 'express';
import bcrypt from 'bcrypt';
import { successResponse, errorResponse } from '../utils/response.util';
import { LoginRequest, LoginResponse } from '../dtos/login.dto';
import { generateAccessToken, generateRefreshToken } from '../utils/jwt.util';
import { EmployeeRepository } from '../repositories/employee.repository';
import { AuthRequest } from '../middleware/auth.middleware';

const employeeRepo = new EmployeeRepository();

export class AuthController {
  async login(req: Request, res: Response) {
    try {
      const body: LoginRequest = req.body;

      if (!body.email || !body.password) {
        return res.status(400).json(errorResponse('Email and password are required'));
      }

      const employee = await employeeRepo.findByEmail(body.email);
      if (!employee) {
        return res.status(401).json(errorResponse('Invalid email or password'));
      }

      if (!employee.isActive || employee.isBlocked) {
        return res.status(403).json(errorResponse('Account is inactive or blocked'));
      }

      const passwordValid = await bcrypt.compare(body.password, employee.passwordHash);
      if (!passwordValid) {
        return res.status(401).json(errorResponse('Invalid email or password'));
      }

      const accessToken = generateAccessToken({
        employeeId: employee.id,
        companyId: employee.companyId,
      });

      const rawRefreshToken = generateRefreshToken();
      await employeeRepo.storeRefreshToken(employee.id, rawRefreshToken);

      const response: LoginResponse = {
        accessToken,
        refreshToken: rawRefreshToken,
        expiresIn: 3600,
        tokenType: 'Bearer',
        employeeId: employee.id,
        companyId: employee.companyId,
        deviceId: null,
        configVersion: '1.0.0',
        requiresDeviceRegistration: true,
      };

      return res.json(successResponse(response));
    } catch (error) {
      console.error('[AuthController] login error:', error);
      return res.status(500).json(errorResponse('Internal server error', 500));
    }
  }

  async refresh(req: Request, res: Response) {
    try {
      const { refreshToken } = req.body;
      if (!refreshToken) {
        return res.status(400).json(errorResponse('Refresh token is required'));
      }

      const record = await employeeRepo.findRefreshToken(refreshToken);
      if (!record || record.revokedAt || record.expiresAt < new Date()) {
        return res.status(401).json(errorResponse('Invalid or expired refresh token'));
      }

      if (!record.employee.isActive || record.employee.isBlocked) {
        return res.status(403).json(errorResponse('Account is inactive or blocked'));
      }

      // Rotate: revoke old, issue new
      await employeeRepo.revokeRefreshToken(refreshToken);
      const newRawRefreshToken = generateRefreshToken();
      await employeeRepo.storeRefreshToken(record.employee.id, newRawRefreshToken, record.deviceId ?? undefined);

      const accessToken = generateAccessToken({
        employeeId: record.employee.id,
        companyId: record.employee.companyId,
      });

      return res.json(successResponse({
        accessToken,
        refreshToken: newRawRefreshToken,
        expiresIn: 3600,
        tokenType: 'Bearer',
      }));
    } catch (error) {
      console.error('[AuthController] refresh error:', error);
      return res.status(500).json(errorResponse('Internal server error', 500));
    }
  }

  async logout(req: AuthRequest, res: Response) {
    try {
      const { refreshToken } = req.body;
      if (refreshToken) {
        await employeeRepo.revokeRefreshToken(refreshToken);
      }
      if (req.user?.employeeId) {
        await employeeRepo.revokeAllForEmployee(req.user.employeeId);
      }
      return res.json(successResponse(null, 'Logged out successfully'));
    } catch (error) {
      return res.status(500).json(errorResponse('Internal server error', 500));
    }
  }
}
