"use client";

import { AppLayout } from "@/components/layout/AppLayout";

export default function Loading() {
  return (
    <AppLayout isLoading={true}>
      <div className="d-flex justify-content-center">
        <div className="card-modern p-4 p-md-5 w-100" style={{ maxWidth: "720px" }}>
          <div className="d-flex align-items-center gap-3 mb-4">
            <div className="skeleton-block" style={{ width: 56, height: 56, borderRadius: "50%" }} />
            <div className="flex-grow-1">
              <div className="skeleton-block mb-2" style={{ width: 200, height: 20 }} />
              <div className="skeleton-block" style={{ width: 160, height: 14 }} />
            </div>
          </div>

          <div className="d-flex gap-2 mb-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="skeleton-block flex-grow-1" style={{ height: 34, borderRadius: 999 }} />
            ))}
          </div>

          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="mb-4">
              <div className="skeleton-block mb-2" style={{ width: 130, height: 14 }} />
              <div className="skeleton-block" style={{ width: "100%", height: 46 }} />
            </div>
          ))}

          <div className="d-flex flex-column flex-sm-row gap-2">
            <div className="skeleton-block flex-grow-1" style={{ height: 50 }} />
            <div className="skeleton-block" style={{ width: 110, height: 50 }} />
          </div>
        </div>
      </div>
    </AppLayout>
  );
}
