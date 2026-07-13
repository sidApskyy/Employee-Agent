import { Request, Response } from 'express';
import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

export const getHealthReports = async (req: Request, res: Response) => {
  try {
    const { deviceId, employeeId, companyId, limit = 100 } = req.query;
    
    const reports = await prisma.healthReport.findMany({
      where: {
        deviceId: deviceId as string,
        employeeId: employeeId as string,
        companyId: companyId as string
      },
      orderBy: { reportedAt: 'desc' },
      take: parseInt(limit as string)
    });
    
    res.json(reports);
  } catch (error) {
    res.status(500).json({ error: 'Failed to fetch health reports' });
  }
};

export const createHealthReport = async (req: Request, res: Response) => {
  try {
    const { deviceId, employeeId, companyId, cpuPercent, ramUsedMb, diskUsedGb, isOnline, queueSize, databaseStatus, serviceStatus } = req.body;
    
    const report = await prisma.healthReport.create({
      data: {
        deviceId,
        employeeId,
        companyId,
        cpuPercent,
        ramUsedMb,
        diskUsedGb,
        isOnline,
        queueSize,
        databaseStatus,
        serviceStatus
      }
    });
    
    res.status(201).json(report);
  } catch (error) {
    res.status(500).json({ error: 'Failed to create health report' });
  }
};
