import { Request, Response } from 'express';
import { prisma } from '../lib/prisma';
import { AmazonS3Provider } from '../providers/AmazonS3Provider';
import { EmployeeRepository } from '../repositories/employee.repository';
import { StorageService } from '../services/storage.service';
import { errorResponse, successResponse } from '../utils/response.util';

const employeeRepository = new EmployeeRepository();
const storageService = new StorageService();
const s3 = new AmazonS3Provider();

const findEmployeeByEmail = async (email: string) => {
  const employee = await employeeRepository.findByEmail(email);
  if (!employee) {
    throw new Error('Employee is not registered in the Employee Agent');
  }

  return employee;
};

export const listCrmScreenshots = async (req: Request, res: Response): Promise<Response> => {
  try {
    const employeeEmail = String(req.query.employeeEmail ?? '').trim();
    const limit = Math.min(Math.max(parseInt(String(req.query.limit ?? '50'), 10) || 50, 1), 100);
    const offset = Math.max(parseInt(String(req.query.offset ?? '0'), 10) || 0, 0);

    if (!employeeEmail) {
      return res.status(400).json(errorResponse('employeeEmail is required'));
    }

    const employee = await findEmployeeByEmail(employeeEmail);
    const files = await storageService.listFiles(employee.id, limit, offset);

    return res.status(200).json(successResponse({
      employee: {
        id: employee.id,
        email: employee.email,
        firstName: employee.firstName,
        lastName: employee.lastName,
      },
      files: files.map((file) => ({
        id: file.id,
        fileSize: file.fileSize,
        checksumVerified: file.checksumVerified,
        uploadStatus: file.uploadStatus,
        uploadedAt: file.uploadedAt,
      })),
    }));
  } catch (error: any) {
    console.error('[CrmScreenshotController] list error:', error, error.stack);
    return res.status(error.message === 'Employee is not registered in the Employee Agent' ? 404 : 500)
      .json(errorResponse(error.message ?? 'Failed to retrieve screenshots'));
  }
};

export const viewCrmScreenshot = async (req: Request, res: Response): Promise<Response> => {
  try {
    const employeeEmail = String(req.query.employeeEmail ?? '').trim();
    const { id } = req.params;

    if (!employeeEmail) {
      return res.status(400).json(errorResponse('employeeEmail is required'));
    }

    const employee = await findEmployeeByEmail(employeeEmail);
    const file = await prisma.uploadedFile.findFirst({
      where: { id, employeeId: employee.id },
    });

    if (!file) {
      return res.status(404).json(errorResponse('Screenshot not found for this employee'));
    }

    const url = await s3.getSignedUrl(file.s3ObjectKey, 300);
    return res.status(200).json(successResponse({ url, expiresInSeconds: 300 }));
  } catch (error: any) {
    console.error('[CrmScreenshotController] view error:', error, error.stack);
    return res.status(error.message === 'Employee is not registered in the Employee Agent' ? 404 : 500)
      .json(errorResponse(error.message ?? 'Failed to generate screenshot URL'));
  }
};
