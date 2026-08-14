"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";

export default function ApprovedPage() {
  const searchParams = useSearchParams();
  const applicationId = searchParams.get("applicationId") ?? undefined;

  return (
    <div className="row justify-content-center">
      <div className="col-lg-6 col-md-8">
        <div className="card-modern p-4 p-md-5 text-center">
          <div className="mb-4">
            <span
              className="icon-badge"
              style={{
                width: "5rem",
                height: "5rem",
                background: "linear-gradient(135deg, #10b981, #0d9488)",
                boxShadow: "0 8px 24px rgba(16, 185, 129, 0.35)",
              }}
              aria-hidden="true"
            >
              <i className="pi pi-check" style={{ fontSize: "2rem" }}></i>
            </span>
          </div>

          <span className="eyebrow mb-3">
            <i className="pi pi-check-circle"></i>
            Application approved
          </span>

          <h2 className="fw-bold mt-3 mb-3">You&apos;re all set</h2>

          <p className="text-muted mx-auto" style={{ maxWidth: "420px" }}>
            Congratulations! Your loan application has been approved. You will
            receive further instructions via email shortly.
          </p>

          {applicationId && (
            <div
              className="d-inline-flex align-items-center gap-2 rounded-pill px-4 py-2 my-4"
              style={{ background: "rgba(16, 185, 129, 0.1)", border: "1px solid rgba(16, 185, 129, 0.25)" }}
            >
              <span className="text-muted small">Application ID:</span>
              <strong className="small" style={{ color: "#047857" }}>
                {applicationId}
              </strong>
            </div>
          )}

          <div className="mt-2">
            <Link href="/">
              <ButtonCTA label="Apply Again" icon="pi pi-plus" />
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}

function ButtonCTA({ label, icon }: { label: string; icon: string }) {
  return (
    <span className="btn btn-gradient btn-lg d-inline-flex align-items-center gap-2">
      <i className={icon}></i>
      {label}
    </span>
  );
}
