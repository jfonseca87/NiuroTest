// Tipos que mapean al DTO del backend y la respuesta

export interface Address {
  street: string;
  city: string;
  state: string;
  zipCode: string;
}

export interface LoanApplicationRequest {
  firstName: string;
  lastName: string;
  address: Address;
  companyName: string;
  requestedAmount: number;
  ssn: string;
}

export interface LoanDecision {
  status: "approved" | "denied";
  reason?: string;
  applicationId?: string;
}

export type LoanApplicationStatus = "idle" | "loading" | "success" | "denied" | "error";

export interface ValidationError {
  field: string;
  message: string;
}

export interface UseLoanApplicationReturn {
  status: LoanApplicationStatus;
  error: string | null;
  validationErrors: ValidationError[];
  submit: (data: LoanApplicationRequest) => Promise<void>;
  reset: () => void;
}
