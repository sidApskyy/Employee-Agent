import { Router } from 'express';
import {
  getQueueJobs,
  createQueueJob,
  updateQueueJob
} from '../controllers/queue.controller';

const router = Router();

router.get('/', getQueueJobs);
router.post('/', createQueueJob);
router.put('/:id', updateQueueJob);

export default router;
