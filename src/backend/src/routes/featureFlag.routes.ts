import { Router } from 'express';
import {
  getFeatureFlags,
  getFeatureFlagByName,
  createFeatureFlag,
  updateFeatureFlag,
  deleteFeatureFlag
} from '../controllers/featureFlag.controller';

const router = Router();

router.get('/', getFeatureFlags);
router.get('/:flagName', getFeatureFlagByName);
router.post('/', createFeatureFlag);
router.put('/:flagName', updateFeatureFlag);
router.delete('/:flagName', deleteFeatureFlag);

export default router;
