export interface ApiErrorResponse {
  status: number;
  errorCode: string;
  message?: string | null;
  traceId?: string | null;
  correlationId?: string | null;
  errors?: ApiError[] | null;
  validationErrors?: ValidationError[] | null;
}

export interface ApiError {
  code: string;
  message: string;
}

export interface ValidationError {
  field: string;
  message: string;
  code?: string | null;
}
