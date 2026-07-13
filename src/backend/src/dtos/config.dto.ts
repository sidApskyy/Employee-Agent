export interface ConfigurationResponse {
  configVersion: string;
  apiUrl: string;
  environment: string;
  agentVersion: string;
  retryCount: number;
  timeoutSeconds: number;
  loggingLevel: string;
  heartbeatIntervalSeconds: number;
  features: {
    screenshotsEnabled: boolean;
    applicationMonitoringEnabled: boolean;
    websiteMonitoringEnabled: boolean;
    idleDetectionEnabled: boolean;
    usbMonitoringEnabled: boolean;
  };
}
