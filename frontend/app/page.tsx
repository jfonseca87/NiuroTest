"use client";

import { LoanApplicationForm } from "@/components/forms/LoanApplicationForm";

export default function Home() {
  return (
    <div className="row justify-content-center">
      <div className="col-lg-8 col-md-10">
        <div className="text-center mb-4">
          <h2 className="fw-bold">Apply for a Loan</h2>
          <p className="text-muted">
            Complete the form below to submit your loan application
          </p>
        </div>
        <LoanApplicationForm />
      </div>
    </div>
  );
}
