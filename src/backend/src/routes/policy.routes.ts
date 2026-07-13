import { Router } from 'express';
import {
  getPolicies,
  getPolicyByType,
  createPolicy,
  updatePolicy,
  deletePolicy
} from '../controllers/policy.controller';

const router = Router();

router.get('/', getPolicies);
router.get('/:policyType', getPolicyByType);
router.post('/', createPolicy);
router.put('/:policyType', updatePolicy);
router.delete('/:policyType', deletePolicy);

export default router;
