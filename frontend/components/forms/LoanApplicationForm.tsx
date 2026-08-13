"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { InputText } from "primereact/inputtext";
import { InputMask } from "primereact/inputmask";
import { InputNumber } from "primereact/inputnumber";
import { Dropdown } from "primereact/dropdown";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { ProgressBar } from "primereact/progressbar";
import { useLoanApplication, getFieldError } from "@/hooks/useLoanApplication";
import type { LoanApplicationRequest, ValidationError } from "@/types/loan";

const US_STATES = [
  { label: "Alabama", value: "AL" },
  { label: "Alaska", value: "AK" },
  { label: "Arizona", value: "AZ" },
  { label: "Arkansas", value: "AR" },
  { label: "California", value: "CA" },
  { label: "Colorado", value: "CO" },
  { label: "Connecticut", value: "CT" },
  { label: "Delaware", value: "DE" },
  { label: "Florida", value: "FL" },
  { label: "Georgia", value: "GA" },
  { label: "Hawaii", value: "HI" },
  { label: "Idaho", value: "ID" },
  { label: "Illinois", value: "IL" },
  { label: "Indiana", value: "IN" },
  { label: "Iowa", value: "IA" },
  { label: "Kansas", value: "KS" },
  { label: "Kentucky", value: "KY" },
  { label: "Louisiana", value: "LA" },
  { label: "Maine", value: "ME" },
  { label: "Maryland", value: "MD" },
  { label: "Massachusetts", value: "MA" },
  { label: "Michigan", value: "MI" },
  { label: "Minnesota", value: "MN" },
  { label: "Mississippi", value: "MS" },
  { label: "Missouri", value: "MO" },
  { label: "Montana", value: "MT" },
  { label: "Nebraska", value: "NE" },
  { label: "Nevada", value: "NV" },
  { label: "New Hampshire", value: "NH" },
  { label: "New Jersey", value: "NJ" },
  { label: "New Mexico", value: "NM" },
  { label: "New York", value: "NY" },
  { label: "North Carolina", value: "NC" },
  { label: "North Dakota", value: "ND" },
  { label: "Ohio", value: "OH" },
  { label: "Oklahoma", value: "OK" },
  { label: "Oregon", value: "OR" },
  { label: "Pennsylvania", value: "PA" },
  { label: "Rhode Island", value: "RI" },
  { label: "South Carolina", value: "SC" },
  { label: "South Dakota", value: "SD" },
  { label: "Tennessee", value: "TN" },
  { label: "Texas", value: "TX" },
  { label: "Utah", value: "UT" },
  { label: "Vermont", value: "VT" },
  { label: "Virginia", value: "VA" },
  { label: "Washington", value: "WA" },
  { label: "West Virginia", value: "WV" },
  { label: "Wisconsin", value: "WI" },
  { label: "Wyoming", value: "WY" },
];

interface FormData {
  firstName: string;
  lastName: string;
  street: string;
  city: string;
  state: string;
  zipCode: string;
  companyName: string;
  requestedAmount: number | null;
  ssn: string;
}

const initialFormData: FormData = {
  firstName: "",
  lastName: "",
  street: "",
  city: "",
  state: "",
  zipCode: "",
  companyName: "",
  requestedAmount: null,
  ssn: "",
};

export function LoanApplicationForm() {
  const router = useRouter();
  const { status, error, validationErrors, decision, submit, reset } = useLoanApplication();
  const [formData, setFormData] = useState<FormData>(initialFormData);

  useEffect(() => {
    if (status === "success") {
      const applicationId = decision?.applicationId;
      router.push(applicationId ? `/approved?applicationId=${encodeURIComponent(applicationId)}` : "/approved");
    } else if (status === "denied") {
      const reason = decision?.reason;
      router.push(reason ? `/denied?reason=${encodeURIComponent(reason)}` : "/denied");
    }
  }, [status, decision, router]);

  const handleFieldChange = (field: keyof FormData, value: string | number | null) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const getError = (field: string): string | undefined => getFieldError(validationErrors, field);

  const renderFieldError = (field: string) => {
    const errorMsg = getError(field);
    if (!errorMsg) return null;
    return (
      <small className="text-danger d-block mt-1">{errorMsg}</small>
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const request: LoanApplicationRequest = {
      firstName: formData.firstName.trim(),
      lastName: formData.lastName.trim(),
      address: {
        street: formData.street.trim(),
        city: formData.city.trim(),
        state: formData.state,
        zipCode: formData.zipCode.trim(),
      },
      companyName: formData.companyName.trim(),
      requestedAmount: formData.requestedAmount || 0,
      ssn: formData.ssn,
    };

    await submit(request);
  };

  const handleReset = () => {
    setFormData(initialFormData);
    reset();
  };

  const isLoading = status === "loading";

  return (
    <div className="card">
      <div className="card-header bg-primary text-white">
        <h4 className="mb-0">Loan Application</h4>
      </div>
      <div className="card-body">
        {status === "error" && error && !validationErrors.length && (
          <Message severity="error" text={error} className="mb-4 w-100" />
        )}

        <form onSubmit={handleSubmit}>
          {/* Personal Information */}
          <h6 className="text-muted mb-3">Personal Information</h6>
          <div className="row g-3 mb-4">
            <div className="col-md-6">
              <label htmlFor="firstName" className="form-label">First Name</label>
              <InputText
                id="firstName"
                value={formData.firstName}
                onChange={(e) => handleFieldChange("firstName", e.target.value)}
                className={`w-100 ${getError("firstName") ? "p-invalid" : ""}`}
                disabled={isLoading}
              />
              {renderFieldError("firstName")}
            </div>
            <div className="col-md-6">
              <label htmlFor="lastName" className="form-label">Last Name</label>
              <InputText
                id="lastName"
                value={formData.lastName}
                onChange={(e) => handleFieldChange("lastName", e.target.value)}
                className={`w-100 ${getError("lastName") ? "p-invalid" : ""}`}
                disabled={isLoading}
              />
              {renderFieldError("lastName")}
            </div>
          </div>

          {/* Address */}
          <h6 className="text-muted mb-3">Address</h6>
          <div className="row g-3 mb-4">
            <div className="col-12">
              <label htmlFor="street" className="form-label">Street</label>
              <InputText
                id="street"
                value={formData.street}
                onChange={(e) => handleFieldChange("street", e.target.value)}
                className={`w-100 ${getError("address.street") ? "p-invalid" : ""}`}
                disabled={isLoading}
              />
              {renderFieldError("address.street")}
            </div>
            <div className="col-md-6">
              <label htmlFor="city" className="form-label">City</label>
              <InputText
                id="city"
                value={formData.city}
                onChange={(e) => handleFieldChange("city", e.target.value)}
                className={`w-100 ${getError("address.city") ? "p-invalid" : ""}`}
                disabled={isLoading}
              />
              {renderFieldError("address.city")}
            </div>
            <div className="col-md-3">
              <label htmlFor="state" className="form-label">State</label>
              <Dropdown
                id="state"
                value={formData.state}
                options={US_STATES}
                onChange={(e) => handleFieldChange("state", e.value)}
                placeholder="Select"
                className={`w-100 ${getError("address.state") ? "p-invalid" : ""}`}
                disabled={isLoading}
              />
              {renderFieldError("address.state")}
            </div>
            <div className="col-md-3">
              <label htmlFor="zipCode" className="form-label">Zip Code</label>
              <InputText
                id="zipCode"
                value={formData.zipCode}
                onChange={(e) => handleFieldChange("zipCode", e.target.value)}
                className={`w-100 ${getError("address.zipCode") ? "p-invalid" : ""}`}
                disabled={isLoading}
              />
              {renderFieldError("address.zipCode")}
            </div>
          </div>

          {/* Employment */}
          <h6 className="text-muted mb-3">Employment</h6>
          <div className="row g-3 mb-4">
            <div className="col-md-6">
              <label htmlFor="companyName" className="form-label">Company Name</label>
              <InputText
                id="companyName"
                value={formData.companyName}
                onChange={(e) => handleFieldChange("companyName", e.target.value)}
                className={`w-100 ${getError("companyName") ? "p-invalid" : ""}`}
                disabled={isLoading}
              />
              {renderFieldError("companyName")}
            </div>
            <div className="col-md-6">
              <label htmlFor="requestedAmount" className="form-label">Requested Amount ($)</label>
              <InputNumber
                id="requestedAmount"
                value={formData.requestedAmount}
                onValueChange={(e) => handleFieldChange("requestedAmount", e.value ?? null)}
                mode="currency"
                currency="USD"
                locale="en-US"
                className={`w-100 ${getError("requestedAmount") ? "p-invalid" : ""}`}
                disabled={isLoading}
              />
              {renderFieldError("requestedAmount")}
            </div>
          </div>

          {/* SSN */}
          <h6 className="text-muted mb-3">Social Security Number</h6>
          <div className="row mb-4">
            <div className="col-md-4">
              <label htmlFor="ssn" className="form-label">SSN</label>
              <InputMask
                id="ssn"
                value={formData.ssn}
                onChange={(e) => handleFieldChange("ssn", e.target.value || "")}
                mask="999-99-9999"
                placeholder="###-##-####"
                className={`w-100 ${getError("ssn") ? "p-invalid" : ""}`}
                disabled={isLoading}
              />
              {renderFieldError("ssn")}
            </div>
          </div>

          {/* Submit */}
          <div className="d-flex gap-2">
            <Button
              type="submit"
              label={isLoading ? "Submitting..." : "Submit Application"}
              icon="pi pi-check"
              loading={isLoading}
              className="p-button-primary"
              disabled={isLoading}
            />
            <Button
              type="button"
              label="Reset"
              icon="pi pi-times"
              className="p-button-secondary"
              onClick={handleReset}
              disabled={isLoading}
            />
          </div>

          {isLoading && (
            <ProgressBar mode="indeterminate" className="mt-3" style={{ height: "4px" }} />
          )}
        </form>
      </div>
    </div>
  );
}
