import { Router } from 'express';
import {
  getHealthReports,
  createHealthReport
} from '../controllers/health.controller';

const router = Router();

router.get('/', getHealthReports);
router.post('/', createHealthReport);

export default router;
