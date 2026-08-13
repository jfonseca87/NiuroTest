"use client";

import { type ReactNode } from "react";
import { Navbar } from "./Navbar";
import { ProgressSpinner } from "primereact/progressspinner";

interface AppLayoutProps {
  children: ReactNode;
  isLoading?: boolean;
}

export function AppLayout({ children, isLoading = false }: AppLayoutProps) {
  return (
    <div className="app-layout min-vh-100 d-flex flex-column">
      <Navbar />

      <main className="flex-grow-1">
        <div className="container py-4">
          {isLoading ? (
            <div className="d-flex justify-content-center align-items-center min-vh-50">
              <ProgressSpinner
                style={{ width: "50px", height: "50px" }}
                strokeWidth="4"
              />
            </div>
          ) : (
            children
          )}
        </div>
      </main>

      <footer className="bg-light py-3 mt-auto border-top">
        <div className="container text-center text-muted">
          <small>NiuroTest &copy; {new Date().getFullYear()}</small>
        </div>
      </footer>
    </div>
  );
}
