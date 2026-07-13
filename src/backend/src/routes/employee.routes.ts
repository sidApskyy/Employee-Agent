import { Router } from 'express';
import { EmployeeController } from '../controllers/employee.controller';
import { authenticate } from '../middleware/auth.middleware';

const router = Router();
const employeeController = new EmployeeController();

// All employee management routes require authentication (admin token)
router.post('/', authenticate, (req, res) => employeeController.createEmployee(req, res));
router.get('/', authenticate, (req, res) => employeeController.listEmployees(req, res));
router.post('/:id/block', authenticate, (req, res) => employeeController.blockEmployee(req, res));
router.post('/:id/reset-password', authenticate, (req, res) => employeeController.resetPassword(req, res));

export default router;
