import { Request, Response } from 'express';
import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

export const getQueueJobs = async (req: Request, res: Response) => {
  try {
    const { deviceId, employeeId, companyId, jobState, limit = 100 } = req.query;
    
    const jobs = await prisma.queueJob.findMany({
      where: {
        deviceId: deviceId as string,
        employeeId: employeeId as string,
        companyId: companyId as string,
        jobState: jobState as string
      },
      orderBy: { createdAt: 'desc' },
      take: parseInt(limit as string)
    });
    
    res.json(jobs);
  } catch (error) {
    res.status(500).json({ error: 'Failed to fetch queue jobs' });
  }
};

export const createQueueJob = async (req: Request, res: Response) => {
  try {
    const { deviceId, employeeId, companyId, jobType, jobPriority, payload, scheduledAt } = req.body;
    
    const job = await prisma.queueJob.create({
      data: {
        deviceId,
        employeeId,
        companyId,
        jobType,
        jobPriority,
        jobState: 'Pending',
        payload,
        scheduledAt: scheduledAt ? new Date(scheduledAt) : null,
        device: { connect: { id: deviceId } }
      }
    });
    
    res.status(201).json(job);
  } catch (error) {
    res.status(500).json({ error: 'Failed to create queue job' });
  }
};

export const updateQueueJob = async (req: Request, res: Response) => {
  try {
    const { id } = req.params;
    const { jobState, error, startedAt, completedAt, nextRetryAt } = req.body;
    
    const job = await prisma.queueJob.update({
      where: { id: String(id) },
      data: {
        jobState,
        error,
        startedAt: startedAt ? new Date(startedAt) : null,
        completedAt: completedAt ? new Date(completedAt) : null,
        nextRetryAt: nextRetryAt ? new Date(nextRetryAt) : null
      }
    });
    
    res.json(job);
  } catch (error) {
    res.status(500).json({ error: 'Failed to update queue job' });
  }
};
