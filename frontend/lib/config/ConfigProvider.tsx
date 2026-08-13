"use client";

import { createContext, useContext, type ReactNode } from "react";

const DEFAULT_API_URL = "http://localhost:5100";

/**
 * Slice de config de la app. Escrita por los client components.
 * Las variables NEXT_PUBLIC_* se resuelven en build-time, por lo que
 * cambiar el puerto del API requiere reiniciar `npm run dev`.
 */
const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? DEFAULT_API_URL;

export interface Config {
  apiUrl: string;
}

const ConfigContext = createContext<Config | null>(null);

export function ConfigProvider({ children }: { children: ReactNode }) {
  return <ConfigContext.Provider value={{ apiUrl }}>{children}</ConfigContext.Provider>;
}

export function useConfig(): Config {
  const config = useContext(ConfigContext);
  if (!config) {
    throw new Error("useConfig must be used within a ConfigProvider");
  }
  return config;
}