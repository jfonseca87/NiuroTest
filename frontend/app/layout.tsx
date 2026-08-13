import type { Metadata } from "next";
import { ConfigProvider } from "@/lib/config";
import { AppLayout } from "@/components/layout/AppLayout";

// Orden de estilos: theme de PrimeReact (estética Bootstrap) → core → iconos → Bootstrap → overrides propios
import "primereact/resources/themes/bootstrap4-dark-blue/theme.css";
import "primereact/resources/primereact.min.css";
import "primeicons/primeicons.css";
import "bootstrap/dist/css/bootstrap.min.css";
import "./globals.css";

export const metadata: Metadata = {
  title: "NiuroTest",
  description: "Loan application flow",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en">
      <body>
        <ConfigProvider>
          <AppLayout>{children}</AppLayout>
        </ConfigProvider>
      </body>
    </html>
  );
}