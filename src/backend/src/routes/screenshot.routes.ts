import { Router } from 'express';
import screenshotController from '../controllers/screenshot.controller';

const router = Router();

router.get('/screenshots/:id', screenshotController.getScreenshotById.bind(screenshotController));
router.get('/screenshots/employee/:employeeId', screenshotController.getScreenshotsByEmployeeId.bind(screenshotController));
router.get('/screenshots/device/:deviceId', screenshotController.getScreenshotsByDeviceId.bind(screenshotController));
router.post('/screenshots', screenshotController.createScreenshot.bind(screenshotController));
router.put('/screenshots/:id', screenshotController.updateScreenshot.bind(screenshotController));
router.delete('/screenshots/:id', screenshotController.deleteScreenshot.bind(screenshotController));
router.get('/screenshots/employee/:employeeId/timeline', screenshotController.getScreenshotTimeline.bind(screenshotController));
router.get('/screenshots/image/:id', screenshotController.getSignedImageUrl.bind(screenshotController));

export default router;
