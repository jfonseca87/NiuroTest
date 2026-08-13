/**
 * Mensajes legibles para códigos de denegación del rule engine.
 * Mantiene el frontend desacoplado de los reason codes del backend.
 */
export const DENIAL_MESSAGES: Record<string, string> = {
  STATE_NY: "We cannot process applications from New York at this time.",
  SSN_BLACKLISTED: "Your application could not be processed. Please contact support.",
};

/**
 * Obtiene el mensaje legible para un reason code.
 * Si el código no existe, retorna un mensaje genérico.
 */
export function getDenialMessage(reason: string | undefined): string {
  if (!reason) {
    return "Your application could not be processed.";
  }
  return DENIAL_MESSAGES[reason] ?? "Your application was denied. Please try again later.";
}
