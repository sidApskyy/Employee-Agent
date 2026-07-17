import { createApp } from './app';
import { config } from './config';
import { AmazonS3Provider } from './providers/AmazonS3Provider';

async function bootstrap(): Promise<void> {
  const s3 = new AmazonS3Provider();
  await s3.runStartupDiagnostics();

  const app = createApp();
  app.listen(config.port, () => {
    console.log(`Server running on port ${config.port} in ${config.environment} mode`);
  });
}

void bootstrap();
