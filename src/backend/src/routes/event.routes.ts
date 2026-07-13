import { Router } from 'express';
import {
  getEvents,
  createEvent
} from '../controllers/event.controller';

const router = Router();

router.get('/', getEvents);
router.post('/', createEvent);

export default router;
