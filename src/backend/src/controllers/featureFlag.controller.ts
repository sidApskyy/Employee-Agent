import { Request, Response } from 'express';
import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

export const getFeatureFlags = async (req: Request, res: Response) => {
  try {
    const { companyId } = req.query;
    
    const flags = await prisma.featureFlag.findMany({
      where: companyId ? { companyId: companyId as string } : undefined,
      orderBy: { updatedAt: 'desc' }
    });
    
    res.json(flags);
  } catch (error) {
    res.status(500).json({ error: 'Failed to fetch feature flags' });
  }
};

export const getFeatureFlagByName = async (req: Request, res: Response) => {
  try {
    const { flagName } = req.params;
    const { companyId } = req.query;
    
    const flag = await prisma.featureFlag.findFirst({
      where: {
        flagName,
        companyId: companyId as string
      }
    });
    
    if (!flag) {
      return res.status(404).json({ error: 'Feature flag not found' });
    }
    
    res.json(flag);
  } catch (error) {
    res.status(500).json({ error: 'Failed to fetch feature flag' });
  }
};

export const createFeatureFlag = async (req: Request, res: Response) => {
  try {
    const { flagName, companyId, isEnabled, description } = req.body;
    
    const flag = await prisma.featureFlag.create({
      data: {
        flagName,
        companyId,
        isEnabled: isEnabled ?? false,
        description
      }
    });
    
    res.status(201).json(flag);
  } catch (error) {
    res.status(500).json({ error: 'Failed to create feature flag' });
  }
};

export const updateFeatureFlag = async (req: Request, res: Response) => {
  try {
    const { flagName } = req.params;
    const { isEnabled, description } = req.body;
    
    const flag = await prisma.featureFlag.updateMany({
      where: { flagName },
      data: {
        isEnabled,
        description
      }
    });
    
    res.json(flag);
  } catch (error) {
    res.status(500).json({ error: 'Failed to update feature flag' });
  }
};

export const deleteFeatureFlag = async (req: Request, res: Response) => {
  try {
    const { flagName } = req.params;
    
    await prisma.featureFlag.delete({
      where: { flagName }
    });
    
    res.status(204).send();
  } catch (error) {
    res.status(500).json({ error: 'Failed to delete feature flag' });
  }
};
