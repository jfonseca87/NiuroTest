"use client";

import { type ReactNode } from "react";
import { Navbar } from "./Navbar";

interface AppLayoutProps {
  children: ReactNode;
  isLoading?: boolean;
}

function PageSkeleton() {
  return (
    <div className="card-modern p-4 p-md-5 mx-auto" style={{ maxWidth: "720px" }}>
      <div className="d-flex flex-column align-items-center text-center mb-4">
        <div className="skeleton-block mb-3" style={{ width: 180, height: 20 }} />
        <div className="skeleton-block" style={{ width: 280, height: 16 }} />
      </div>
      {Array.from({ length: 4 }).map((_, i) => (
        <div key={i} className="mb-4">
          <div className="skeleton-block mb-2" style={{ width: 120, height: 14 }} />
          <div className="skeleton-block" style={{ width: "100%", height: 46 }} />
        </div>
      ))}
      <div className="d-flex gap-2 mt-4">
        <div className="skeleton-block flex-grow-1" style={{ height: 48 }} />
        <div className="skeleton-block" style={{ width: 110, height: 48 }} />
      </div>
    </div>
  );
}

export function AppLayout({ children, isLoading = false }: AppLayoutProps) {
  return (
    <div className="app-layout min-vh-100 d-flex flex-column">
      <Navbar />

      <main className="flex-grow-1">
        <div className="container py-4 py-lg-5">
          {isLoading ? (
            <PageSkeleton />
          ) : (
            <div className="page-enter">{children}</div>
          )}
        </div>
      </main>

      <footer className="app-footer py-4 mt-auto">
        <div className="container d-flex flex-column flex-sm-row align-items-center justify-content-between gap-2">
          <small className="fw-semibold">
            NiuroTest <span className="gradient-text">·</span> Business financing
          </small>
          <small>&copy; {new Date().getFullYear()} NiuroTest. All rights reserved.</small>
        </div>
      </footer>
    </div>
  );
}
