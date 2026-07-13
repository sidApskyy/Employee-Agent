import { Request, Response } from 'express';
import { PrismaClient } from '@prisma/client';
import { AmazonS3Provider } from '../providers/AmazonS3Provider';

const prisma = new PrismaClient();
const s3 = new AmazonS3Provider();

export class ScreenshotController {
  async getScreenshotById(req: Request, res: Response) {
    try {
      const { id } = req.params;

      const screenshot = await prisma.screenshot.findUnique({
        where: { id },
        include: {
          device: true,
        },
      });

      if (!screenshot) {
        return res.status(404).json({ error: 'Screenshot not found' });
      }

      res.json(screenshot);
    } catch (error) {
      console.error('Error fetching screenshot:', error);
      res.status(500).json({ error: 'Failed to fetch screenshot' });
    }
  }

  async getScreenshotsByEmployeeId(req: Request, res: Response) {
    try {
      const { employeeId } = req.params;
      const { startDate, endDate, limit = 50, offset = 0 } = req.query;

      const where: any = { employeeId };

      if (startDate && endDate) {
        where.captureTimeUtc = {
          gte: new Date(startDate as string),
          lte: new Date(endDate as string),
        };
      }

      const screenshots = await prisma.screenshot.findMany({
        where,
        include: {
          device: true,
        },
        orderBy: { captureTimeUtc: 'desc' },
        take: Number(limit),
        skip: Number(offset),
      });

      const total = await prisma.screenshot.count({ where });

      res.json({
        screenshots,
        total,
        limit: Number(limit),
        offset: Number(offset),
      });
    } catch (error) {
      console.error('Error fetching screenshots:', error);
      res.status(500).json({ error: 'Failed to fetch screenshots' });
    }
  }

  async getScreenshotsByDeviceId(req: Request, res: Response) {
    try {
      const { deviceId } = req.params;
      const { startDate, endDate, limit = 50, offset = 0 } = req.query;

      const where: any = { deviceId };

      if (startDate && endDate) {
        where.captureTimeUtc = {
          gte: new Date(startDate as string),
          lte: new Date(endDate as string),
        };
      }

      const screenshots = await prisma.screenshot.findMany({
        where,
        include: {
          device: true,
        },
        orderBy: { captureTimeUtc: 'desc' },
        take: Number(limit),
        skip: Number(offset),
      });

      const total = await prisma.screenshot.count({ where });

      res.json({
        screenshots,
        total,
        limit: Number(limit),
        offset: Number(offset),
      });
    } catch (error) {
      console.error('Error fetching screenshots:', error);
      res.status(500).json({ error: 'Failed to fetch screenshots' });
    }
  }

  async createScreenshot(req: Request, res: Response) {
    try {
      const screenshotData = req.body;

      const screenshot = await prisma.screenshot.create({
        data: screenshotData,
        include: {
          device: true,
        },
      });

      res.status(201).json(screenshot);
    } catch (error) {
      console.error('Error creating screenshot:', error);
      res.status(500).json({ error: 'Failed to create screenshot' });
    }
  }

  async updateScreenshot(req: Request, res: Response) {
    try {
      const { id } = req.params;
      const updateData = req.body;

      const screenshot = await prisma.screenshot.update({
        where: { id },
        data: updateData,
        include: {
          device: true,
        },
      });

      res.json(screenshot);
    } catch (error) {
      console.error('Error updating screenshot:', error);
      res.status(500).json({ error: 'Failed to update screenshot' });
    }
  }

  async deleteScreenshot(req: Request, res: Response) {
    try {
      const { id } = req.params;

      await prisma.screenshot.delete({
        where: { id },
      });

      res.status(204).send();
    } catch (error) {
      console.error('Error deleting screenshot:', error);
      res.status(500).json({ error: 'Failed to delete screenshot' });
    }
  }

  async getSignedImageUrl(req: Request, res: Response) {
    try {
      const { id } = req.params;

      const screenshot = await prisma.screenshot.findUnique({ where: { id } });
      if (!screenshot) {
        return res.status(404).json({ error: 'Screenshot not found' });
      }

      const uploaded = await prisma.uploadedFile.findFirst({
        where: { employeeId: screenshot.employeeId, deviceId: screenshot.deviceId },
        orderBy: { uploadedAt: 'desc' },
      });

      if (!uploaded?.s3ObjectKey) {
        return res.status(404).json({ error: 'No uploaded file found for this screenshot' });
      }

      const signedUrl = await s3.getSignedUrl(uploaded.s3ObjectKey, 300);
      return res.json({ url: signedUrl, expiresInSeconds: 300 });
    } catch (error) {
      console.error('Error generating signed URL:', error);
      res.status(500).json({ error: 'Failed to generate image URL' });
    }
  }

  async getScreenshotTimeline(req: Request, res: Response) {
    try {
      const { employeeId } = req.params;
      const { startDate, endDate } = req.query;

      const where: any = { employeeId };

      if (startDate && endDate) {
        where.captureTimeUtc = {
          gte: new Date(startDate as string),
          lte: new Date(endDate as string),
        };
      }

      const screenshots = await prisma.screenshot.findMany({
        where,
        select: {
          id: true,
          captureTimeUtc: true,
          monitorId: true,
          width: true,
          height: true,
          format: true,
          storagePath: true,
          uploadStatus: true,
        },
        orderBy: { captureTimeUtc: 'desc' },
      });

      res.json(screenshots);
    } catch (error) {
      console.error('Error fetching screenshot timeline:', error);
      res.status(500).json({ error: 'Failed to fetch screenshot timeline' });
    }
  }
}

export default new ScreenshotController();
