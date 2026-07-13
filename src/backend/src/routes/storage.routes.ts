import { Router } from 'express';
import multer from 'multer';
import { StorageController } from '../controllers/storage.controller';
import { authenticate } from '../middleware/auth.middleware';
import { uploadRateLimiter } from '../middleware/rate-limit.middleware';
import { validateUpload, validateCompleteUpload } from '../validators/upload.validator';

const router = Router();
const storageController = new StorageController();

const upload = multer({
  storage: multer.memoryStorage(),
  limits: { fileSize: 52_428_800 },
});

router.post(
  '/upload',
  uploadRateLimiter,
  authenticate,
  upload.single('file'),
  validateUpload,
  (req, res) => storageController.uploadScreenshot(req as any, res)
);

router.post(
  '/upload/complete',
  authenticate,
  validateCompleteUpload,
  (req, res) => storageController.completeUpload(req as any, res)
);

router.get(
  '/usage',
  authenticate,
  (req, res) => storageController.getStorageUsage(req as any, res)
);

router.get(
  '/files',
  authenticate,
  (req, res) => storageController.listFiles(req as any, res)
);

router.get(
  '/files/:id/view',
  authenticate,
  (req, res) => storageController.viewScreenshot(req as any, res)
);

export default router;
