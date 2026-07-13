export interface HeartbeatRequest {
  employeeId: string;
  deviceId: string;
  agentVersion: string;
  computerName: string;
  isOnline: boolean;
  timestamp: string;
  configVersion: string;
  systemMetrics?: {
    cpuPercent: number;
    memoryUsedMb: number;
  };
}

export interface HeartbeatResponse {
  nextHeartbeatIntervalSeconds: number;
  configVersion: string;
  configChanged: boolean;
  isBlocked: boolean;
  requiresLogout: boolean;
}
