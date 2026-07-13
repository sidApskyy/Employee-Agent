import { Request, Response } from 'express';
import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

export const getEvents = async (req: Request, res: Response) => {
  try {
    const { deviceId, employeeId, companyId, eventType, limit = 100 } = req.query;
    
    const events = await prisma.agentEvent.findMany({
      where: {
        deviceId: deviceId as string,
        employeeId: employeeId as string,
        companyId: companyId as string,
        eventType: eventType as string
      },
      orderBy: { occurredAt: 'desc' },
      take: parseInt(limit as string)
    });
    
    res.json(events);
  } catch (error) {
    res.status(500).json({ error: 'Failed to fetch events' });
  }
};

export const createEvent = async (req: Request, res: Response) => {
  try {
    const { deviceId, employeeId, companyId, eventType, eventData, occurredAt } = req.body;
    
    const event = await prisma.agentEvent.create({
      data: {
        deviceId,
        employeeId,
        companyId,
        eventType,
        eventData,
        occurredAt: occurredAt ? new Date(occurredAt) : new Date()
      }
    });
    
    res.status(201).json(event);
  } catch (error) {
    res.status(500).json({ error: 'Failed to create event' });
  }
};
