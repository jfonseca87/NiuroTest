"use client";

import Link from "next/link";
import { Button } from "primereact/button";
import { Message } from "primereact/message";

export default function ApprovedPage() {
  return (
    <div className="row justify-content-center">
      <div className="col-lg-6 col-md-8">
        <div className="card text-center">
          <div className="card-header bg-success text-white">
            <h4 className="mb-0">Application Approved</h4>
          </div>
          <div className="card-body">
            <div className="mb-4">
              <i className="pi pi-check-circle text-success" style={{ fontSize: "4rem" }}></i>
            </div>

            <Message
              severity="success"
              text="Congratulations! Your loan application has been approved."
              className="w-100 mb-4"
            />

            <p className="text-muted">
              Your application has been processed successfully. You will receive
              further instructions via email.
            </p>

            <div className="d-flex gap-2 justify-content-center mt-4">
              <Link href="/">
                <Button
                  label="Apply Again"
                  icon="pi pi-plus"
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
