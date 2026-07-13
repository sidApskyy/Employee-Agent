import { Request, Response } from 'express';
import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

export const getPolicies = async (req: Request, res: Response) => {
  try {
    const { companyId } = req.query;
    
    const policies = await prisma.policy.findMany({
      where: companyId ? { companyId: companyId as string } : undefined,
      orderBy: { updatedAt: 'desc' }
    });
    
    res.json(policies);
  } catch (error) {
    res.status(500).json({ error: 'Failed to fetch policies' });
  }
};

export const getPolicyByType = async (req: Request, res: Response) => {
  try {
    const { policyType } = req.params;
    const { companyId } = req.query;
    
    const policy = await prisma.policy.findFirst({
      where: {
        policyType,
        companyId: companyId as string
      }
    });
    
    if (!policy) {
      return res.status(404).json({ error: 'Policy not found' });
    }
    
    res.json(policy);
  } catch (error) {
    res.status(500).json({ error: 'Failed to fetch policy' });
  }
};

export const createPolicy = async (req: Request, res: Response) => {
  try {
    const { policyType, companyId, policyJson, version, isActive } = req.body;
    
    const policy = await prisma.policy.create({
      data: {
        policyType,
        companyId,
        policyJson,
        version,
        isActive: isActive ?? true
      }
    });
    
    res.status(201).json(policy);
  } catch (error) {
    res.status(500).json({ error: 'Failed to create policy' });
  }
};

export const updatePolicy = async (req: Request, res: Response) => {
  try {
    const { policyType } = req.params;
    const { policyJson, version, isActive } = req.body;
    
    const policy = await prisma.policy.updateMany({
      where: { policyType },
      data: {
        policyJson,
        version,
        isActive
      }
    });
    
    res.json(policy);
  } catch (error) {
    res.status(500).json({ error: 'Failed to update policy' });
  }
};

export const deletePolicy = async (req: Request, res: Response) => {
  try {
    const { policyType } = req.params;
    
    await prisma.policy.delete({
      where: { policyType }
    });
    
    res.status(204).send();
  } catch (error) {
    res.status(500).json({ error: 'Failed to delete policy' });
  }
};
