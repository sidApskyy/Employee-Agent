import bcrypt from 'bcrypt';
import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

const COMPANY_ID = process.env.COMPANY_ID || 'company-rdcs-001';
const DEFAULT_PASSWORD = process.env.SEED_DEFAULT_PASSWORD || 'Change@Me2024!';
const SALT_ROUNDS = 12;

// ─────────────────────────────────────────────────────────────
// HOW TO ADD A NEW EMPLOYEE:
// 1. Add a new line inside the employees array below
// 2. Format: { firstName: 'First', lastName: 'Last', email: 'email@company.com' }
// 3. Save the file
// 4. Run: npm run db:seed
// The script is SAFE to re-run — it skips employees that already exist
// All new employees get the password from .env → SEED_DEFAULT_PASSWORD
// ─────────────────────────────────────────────────────────────
const employees = [
  // ↓↓ ADD YOUR EMPLOYEES HERE ↓↓
   { firstName: 'Asma',  lastName: 'Khan',  email: 'asma.khan@rdcsgenix.com'  },
   { firstName: 'Noman',  lastName: 'Khan',    email: 'noman.khan@rdcsgenix.com'    },
   { firstName: 'Daniyal',   lastName: 'Khan', email: 'daniyal.khan@rdcsgenix.com'  },
   { firstName: 'Manish',   lastName: 'Vishwakarma', email: 'manish.vishwakarma@rdcsgenix.com'  },
   { firstName: 'Muntaha',   lastName: 'Shaikh', email: 'muntaha.shaikh@rdcsgenix.com'  },
   { firstName: 'Shoaib',   lastName: 'Khan', email: 'shoaib.khan@rdcsgenix.com'  },
   { firstName: 'Afan',   lastName: 'Khan', email: 'afan.khan@rdcsgenix.com'  },
   
];


async function main() {
  console.log(`Seeding ${employees.length} employee(s)...`);
  const passwordHash = await bcrypt.hash(DEFAULT_PASSWORD, SALT_ROUNDS);

  let created = 0;
  let skipped = 0;

  for (const emp of employees) {
    const existing = await prisma.employee.findUnique({ where: { email: emp.email } });
    if (existing) {
      skipped++;
      continue;
    }

    await prisma.employee.create({
      data: {
        email: emp.email,
        passwordHash,
        firstName: emp.firstName,
        lastName: emp.lastName,
        companyId: COMPANY_ID,
        role: 'employee',
        isActive: true,
        isBlocked: false,
      },
    });
    created++;
    console.log(`  ✓ ${emp.firstName} ${emp.lastName} <${emp.email}>`);
  }

  console.log(`\nDone. Created: ${created}, Skipped (already exist): ${skipped}`);
  console.log(`Default password: ${DEFAULT_PASSWORD}`);
  console.log('IMPORTANT: Ask each employee to change their password after first login.');
}

main()
  .catch((e) => {
    console.error('Seed failed:', e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();  // safe here — seed runs as a standalone script
  });
