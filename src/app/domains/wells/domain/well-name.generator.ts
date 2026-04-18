// Generador de nombre de pozo
// Referencia: spec.md RN-16 a RN-22

/**
 * Genera el nombre del pozo a partir de sus componentes.
 * Formato: `{Campo o Contrato}-{Denominación}-{Consecutivo}`
 * - Si hay campo, usa el nombre del campo como prefijo (RN-16)
 * - Si no hay campo, usa el nombre del contrato (RN-17)
 * - Denominación se normaliza a MAYÚSCULAS (RN-20)
 * - Consecutivo sin ceros a la izquierda (RN-21)
 */
export function generateWellName(
  campo: string | null,
  contratoNombre: string | null,
  denominacion: string,
  consecutivo: number,
): string {
  const prefijo = campo?.trim() || contratoNombre?.trim() || '';
  const denom   = denominacion.trim().toUpperCase();
  const consec  = consecutivo.toString();

  if (!prefijo || !denom || !consecutivo) return '';

  return `${prefijo.toUpperCase()}-${denom}-${consec}`;
}
