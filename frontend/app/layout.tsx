import type { Metadata } from "next";
import { Inter } from "next/font/google";
import { ConfigProvider } from "@/lib/config";
import { AppLayout } from "@/components/layout/AppLayout";

// Orden de estilos: theme de PrimeReact (estética Bootstrap) → core → iconos → Bootstrap → overrides propios
import "primereact/resources/themes/bootstrap4-dark-blue/theme.css";
import "primereact/resources/primereact.min.css";
import "primeicons/primeicons.css";
import "bootstrap/dist/css/bootstrap.min.css";
import "./globals.css";

const inter = Inter({
  subsets: ["latin"],
  display: "swap",
  variable: "--font-inter",
});

export const metadata: Metadata = {
  title: "NiuroTest — Business Loan Application",
  description:
    "Apply for a business loan in minutes. No hidden fees, instant decisions.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className={inter.variable}>
      <body className={inter.className}>
        <ConfigProvider>
          <AppLayout>{children}</AppLayout>
        </ConfigProvider>
      </body>
    </html>
  );
}