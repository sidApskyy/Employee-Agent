import { Router } from 'express';
import { AuthController } from '../controllers/auth.controller';
import { DeviceController } from '../controllers/device.controller';
import { HeartbeatController } from '../controllers/heartbeat.controller';
import { ConfigController } from '../controllers/config.controller';
import { authenticate } from '../middleware/auth.middleware';

const router = Router();

const authController = new AuthController();
const deviceController = new DeviceController();
const heartbeatController = new HeartbeatController();
const configController = new ConfigController();

router.post('/login', (req, res) => authController.login(req, res));
router.post('/refresh', (req, res) => authController.refresh(req, res));
router.post('/logout', authenticate, (req, res) => authController.logout(req as any, res));
router.post('/register-device', authenticate, (req, res) => deviceController.registerDevice(req, res));
router.post('/heartbeat', authenticate, (req, res) => heartbeatController.heartbeat(req, res));
router.get('/config', (req, res) => configController.getConfig(req, res));
router.get('/health', (_req, res) => res.json({ status: 'ok', timestamp: new Date().toISOString() }));

export default router;
