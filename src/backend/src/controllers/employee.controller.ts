import { Request, Response } from 'express';
import bcrypt from 'bcrypt';
import { successResponse, errorResponse } from '../utils/response.util';
import { EmployeeRepository } from '../repositories/employee.repository';
import { prisma } from '../lib/prisma';

const employeeRepo = new EmployeeRepository();
const SALT_ROUNDS = 12;

export class EmployeeController {
  async createEmployee(req: Request, res: Response) {
    try {
      const { firstName, lastName, email, password, companyId } = req.body;

      if (!firstName || !lastName || !email || !password) {
        return res.status(400).json(errorResponse('firstName, lastName, email, and password are required'));
      }

      const existing = await employeeRepo.findByEmail(email);
      if (existing) {
        return res.status(409).json(errorResponse('An employee with this email already exists'));
      }

      const passwordHash = await bcrypt.hash(password, SALT_ROUNDS);

      const employee = await employeeRepo.create({
        firstName,
        lastName,
        email,
        passwordHash,
        companyId: companyId || process.env.COMPANY_ID || 'company-rdcs-001',
      });

      return res.status(201).json(successResponse({
        id: employee.id,
        email: employee.email,
        firstName: employee.firstName,
        lastName: employee.lastName,
        companyId: employee.companyId,
        isActive: employee.isActive,
        createdAt: employee.createdAt,
      }, 'Employee created successfully'));
    } catch (error) {
      console.error('[EmployeeController] createEmployee error:', error);
      return res.status(500).json(errorResponse('Internal server error', 500));
    }
  }

  async listEmployees(req: Request, res: Response) {
    try {
      const employees = await prisma.employee.findMany({
        select: {
          id: true,
          email: true,
          firstName: true,
          lastName: true,
          companyId: true,
          isActive: true,
          isBlocked: true,
          createdAt: true,
        },
        orderBy: { firstName: 'asc' },
      });

      return res.json(successResponse(employees));
    } catch (error) {
      return res.status(500).json(errorResponse('Internal server error', 500));
    }
  }

  async blockEmployee(req: Request, res: Response) {
    try {
      const { id } = req.params;

      await prisma.employee.update({
        where: { id },
        data: { isBlocked: true, isActive: false },
      });

      return res.json(successResponse(null, 'Employee blocked'));
    } catch (error) {
      return res.status(500).json(errorResponse('Internal server error', 500));
    }
  }

  async resetPassword(req: Request, res: Response) {
    try {
      const { id } = req.params;
      const { newPassword } = req.body;

      if (!newPassword || newPassword.length < 8) {
        return res.status(400).json(errorResponse('New password must be at least 8 characters'));
      }

      const passwordHash = await bcrypt.hash(newPassword, SALT_ROUNDS);

      await prisma.employee.update({
        where: { id },
        data: { passwordHash },
      });

      // Revoke all active refresh tokens so they must log in again
      await employeeRepo.revokeAllForEmployee(id);

      return res.json(successResponse(null, 'Password reset successfully. Employee must log in again.'));
    } catch (error) {
      return res.status(500).json(errorResponse('Internal server error', 500));
    }
  }
}
