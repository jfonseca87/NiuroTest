"use client";

import { AppLayout } from "@/components/layout/AppLayout";

export default function Loading() {
  return (
    <AppLayout isLoading={true}>
      <div className="text-center">
        <p className="text-muted">Loading...</p>
      </div>
    </AppLayout>
  );
}
