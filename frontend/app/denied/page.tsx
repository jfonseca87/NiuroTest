"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { getDenialMessage } from "@/lib/messages";

export default function DeniedPage() {
  const searchParams = useSearchParams();
  const reason = searchParams.get("reason") ?? undefined;
  const friendlyMessage = getDenialMessage(reason);

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
                background: "linear-gradient(135deg, #ef4444, #dc2626)",
                boxShadow: "0 8px 24px rgba(239, 68, 68, 0.35)",
              }}
              aria-hidden="true"
            >
              <i className="pi pi-times" style={{ fontSize: "2rem" }}></i>
            </span>
          </div>

          <span className="eyebrow mb-3" style={{ color: "#dc2626", borderColor: "rgba(220,38,38,0.2)", background: "rgba(239,68,68,0.08)" }}>
            <i className="pi pi-times-circle"></i>
            Application not approved
          </span>

          <h2 className="fw-bold mt-3 mb-3">We couldn&apos;t approve your application</h2>

          <p className="text-muted mx-auto" style={{ maxWidth: "440px" }}>
            {friendlyMessage}
          </p>

          <p className="text-muted mx-auto mt-2" style={{ maxWidth: "440px" }}>
            Please review the eligibility criteria and try again. If you believe
            this is an error, our support team is happy to help.
          </p>

          <div className="mt-2">
            <Link href="/">
              <span className="btn btn-gradient btn-lg d-inline-flex align-items-center gap-2">
                <i className="pi pi-refresh"></i>
                Try Again
              </span>
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
