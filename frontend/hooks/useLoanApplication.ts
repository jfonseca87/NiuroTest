import { useState, useCallback } from "react";
import { useConfig } from "@/lib/config";
import type {
  LoanApplicationRequest,
  LoanDecision,
  LoanApplicationStatus,
  ValidationError,
  UseLoanApplicationReturn,
} from "@/types/loan";

const SSN_REGEX = /^\d{3}-\d{2}-\d{4}$/;
const STATE_REGEX = /^[A-Z]{2}$/;
const ZIP_REGEX = /^\d{5}(-\d{4})?$/;

export function validateLoanApplication(data: LoanApplicationRequest): ValidationError[] {
  const errors: ValidationError[] = [];

  if (!data.firstName?.trim()) {
    errors.push({ field: "firstName", message: "First name is required" });
  }

  if (!data.lastName?.trim()) {
    errors.push({ field: "lastName", message: "Last name is required" });
  }

  if (!data.address.street?.trim()) {
    errors.push({ field: "address.street", message: "Street is required" });
  }

  if (!data.address.city?.trim()) {
    errors.push({ field: "address.city", message: "City is required" });
  }

  if (!data.address.state?.trim()) {
    errors.push({ field: "address.state", message: "State is required" });
  } else if (!STATE_REGEX.test(data.address.state)) {
    errors.push({ field: "address.state", message: "State must be 2 uppercase letters (e.g., CA, NY)" });
  }

  if (!data.address.zipCode?.trim()) {
    errors.push({ field: "address.zipCode", message: "Zip code is required" });
  } else if (!ZIP_REGEX.test(data.address.zipCode)) {
    errors.push({ field: "address.zipCode", message: "Zip code must be 5 digits or 5+4 format (e.g., 12345 or 12345-6789)" });
  }

  if (!data.companyName?.trim()) {
    errors.push({ field: "companyName", message: "Company name is required" });
  }

  if (!data.requestedAmount || data.requestedAmount <= 0) {
    errors.push({ field: "requestedAmount", message: "Requested amount must be greater than 0" });
  }

  if (!data.ssn?.trim()) {
    errors.push({ field: "ssn", message: "SSN is required" });
  } else if (!SSN_REGEX.test(data.ssn)) {
    errors.push({ field: "ssn", message: "SSN must be in format ###-##-####" });
  }

  return errors;
}

function normalizeSSN(ssn: string): string {
  const digits = ssn.replace(/\D/g, "");
  if (digits.length === 9) {
    return `${digits.slice(0, 3)}-${digits.slice(3, 5)}-${digits.slice(5)}`;
  }
  return ssn;
}

export function useLoanApplication(): UseLoanApplicationReturn {
  const { apiUrl } = useConfig();
  const [status, setStatus] = useState<LoanApplicationStatus>("idle");
  const [error, setError] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<ValidationError[]>([]);
  const [decision, setDecision] = useState<LoanDecision | null>(null);

  const reset = useCallback(() => {
    setStatus("idle");
    setError(null);
    setValidationErrors([]);
    setDecision(null);
  }, []);

  const submit = useCallback(async (data: LoanApplicationRequest) => {
    // Clear previous errors
    setError(null);
    setValidationErrors([]);

    // Validate
    const errors = validateLoanApplication(data);
    if (errors.length > 0) {
      setValidationErrors(errors);
      setStatus("error");
      return;
    }

    setStatus("loading");

    // Normalize SSN to ###-##-#### format
    const normalizedData: LoanApplicationRequest = {
      ...data,
      ssn: normalizeSSN(data.ssn),
    };

    try {
      const response = await fetch(`${apiUrl}/api/loan-applications`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(normalizedData),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        if (errorData?.errors) {
          const backendErrors: ValidationError[] = [];
          for (const [field, messages] of Object.entries(errorData.errors)) {
            if (Array.isArray(messages)) {
              for (const msg of messages) {
                backendErrors.push({ field, message: msg });
              }
            }
          }
          if (backendErrors.length > 0) {
            setValidationErrors(backendErrors);
            setStatus("error");
            return;
          }
        }
        throw new Error(`Server error: ${response.status}`);
      }

      const decision: LoanDecision = await response.json();
      setDecision(decision);

      if (decision.status === "approved") {
        setStatus("success");
      } else {
        setStatus("denied");
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Network error occurred");
      setStatus("error");
    }
  }, [apiUrl]);

  return {
    status,
    error,
    validationErrors,
    submit,
    reset,
  };
}

export function getFieldError(errors: ValidationError[], field: string): string | undefined {
  return errors.find((e) => e.field === field)?.message;
}
