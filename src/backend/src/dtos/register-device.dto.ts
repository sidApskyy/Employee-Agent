export interface RegisterDeviceRequest {
  employeeId: string;
  companyId: string;
  deviceName: string;
  computerName: string;
  machineGuid: string;
  fingerprint: string;
  osVersion: string;
  windowsUsername: string;
  processor: string;
  ramGb: number;
  diskSizeGb: number;
  macAddress: string;
  agentVersion: string;
}

export interface RegisterDeviceResponse {
  deviceId: string;
  deviceName: string;
  configVersion: string;
  isBlocked: boolean;
  registeredAt: string;
}
