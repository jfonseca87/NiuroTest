"use client";

import { LoanApplicationForm } from "@/components/forms/LoanApplicationForm";

const benefits = [
  { icon: "pi pi-bolt", title: "Decision in seconds", copy: "Get a funding answer fast, no long wait times." },
  { icon: "pi pi-shield", title: "Secure application", copy: "Your data is encrypted and kept private." },
  { icon: "pi pi-percentage", title: "Competitive rates", copy: "Transparent terms with no hidden fees." },
];

export default function Home() {
  return (
    <div className="row justify-content-center">
      <div className="col-lg-8 col-md-10">
        <div className="text-center mb-5">
          <span className="eyebrow mb-3">
            <i className="pi pi-briefcase"></i>
            Business Loans · No hidden fees
          </span>
          <h1 className="display-5 fw-bold mt-3 mb-3">
            The capital your business needs, <span className="gradient-text">without the friction</span>
          </h1>
          <p className="text-muted fs-5 mx-auto" style={{ maxWidth: "560px" }}>
            Complete a short application and get a decision in seconds. Simple,
            secure, and transparent — designed to get you back to building.
          </p>

          <div className="row justify-content-center g-4 mt-3">
            {benefits.map((b) => (
              <div key={b.title} className="col-md-4 col-12 d-flex gap-3 text-start">
                <span className="icon-badge" style={{ width: "2.75rem", height: "2.75rem" }}>
                  <i className={b.icon}></i>
                </span>
                <div>
                  <h6 className="mb-1">{b.title}</h6>
                  <small className="text-muted">{b.copy}</small>
                </div>
              </div>
            ))}
          </div>
        </div>

        <LoanApplicationForm />
      </div>
    </div>
  );
}
