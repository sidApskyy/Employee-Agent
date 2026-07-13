export const successResponse = (data: any, message: string = 'Success') => ({
  success: true,
  data,
  message,
});

export const errorResponse = (message: string, statusCode: number = 400) => ({
  success: false,
  error: message,
  statusCode,
});
