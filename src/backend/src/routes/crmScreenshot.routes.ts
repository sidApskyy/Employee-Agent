import { Router } from 'express';
import { listCrmScreenshots, viewCrmScreenshot } from '../controllers/crmScreenshot.controller';
import { authenticateCrmScreenshotService } from '../middleware/crmScreenshot.middleware';

const router = Router();

router.get('/screenshots', authenticateCrmScreenshotService, listCrmScreenshots);
router.get('/screenshots/:id/view', authenticateCrmScreenshotService, viewCrmScreenshot);

export default router;
