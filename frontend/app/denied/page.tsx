"use client";

import Link from "next/link";
import { Button } from "primereact/button";
import { Message } from "primereact/message";

export default function DeniedPage() {
  return (
    <div className="row justify-content-center">
      <div className="col-lg-6 col-md-8">
        <div className="card text-center">
          <div className="card-header bg-danger text-white">
            <h4 className="mb-0">Application Denied</h4>
          </div>
          <div className="card-body">
            <div className="mb-4">
              <i className="pi pi-times-circle text-danger" style={{ fontSize: "4rem" }}></i>
            </div>

            <Message
              severity="error"
              text="We regret to inform you that your loan application has been denied."
              className="w-100 mb-4"
            />

            <p className="text-muted">
              Unfortunately, we are unable to approve your application at this time.
              Please review the eligibility criteria and try again.
            </p>

            <div className="d-flex gap-2 justify-content-center mt-4">
              <Link href="/">
                <Button
                  label="Try Again"
                  icon="pi pi-refresh"
                  className="p-button-primary"
                />
              </Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
