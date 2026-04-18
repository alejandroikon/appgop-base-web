// Validadores puros del formulario de creación de pozo
// Referencia: spec.md RN-07, RN-08, RN-11..RN-15

export interface ValidationResult {
  valid:   boolean;
  error?:  string;
}

// ─── Denominación (RN-07) ──────────────────────────────────────────────────────

/** Solo letras (A-Za-z incluyendo caracteres con tilde), espacios y guiones. Máx 50 chars. */
const DENOMINACION_REGEX = /^[A-Za-záéíóúÁÉÍÓÚñÑ\s\-]{1,50}$/;

export function validateDenominacion(value: string): ValidationResult {
  if (!value || value.trim().length === 0) {
    return { valid: false, error: 'required' };
  }
  if (value.length > 50) {
    return { valid: false, error: 'maxlength' };
  }
  if (!DENOMINACION_REGEX.test(value)) {
    return { valid: false, error: 'pattern' };
  }
  return { valid: true };
}

// ─── Consecutivo (RN-08) ──────────────────────────────────────────────────────

/** Número entero entre 1 y 9999. */
export function validateConsecutivo(value: number): ValidationResult {
  if (value === null || value === undefined || isNaN(value)) {
    return { valid: false, error: 'required' };
  }
  if (!Number.isInteger(value) || value < 1 || value > 9999) {
    return { valid: false, error: 'range' };
  }
  return { valid: true };
}

// ─── ANH: solo estratigráfico (RN-15) ─────────────────────────────────────────

/**
 * Si la operadora es ANH, solo se permite Clasificación = ESTRATIGRAFICO.
 */
export function isClasificacionValidForAnh(clasificacion: string, isAnh: boolean): boolean {
  if (!isAnh) return true;
  return clasificacion === 'ESTRATIGRAFICO';
}

// ─── Campo requerido según clasificación (RN-11, RN-12, RN-13) ───────────────

/**
 * Determina si el campo "Campo" es obligatorio según la clasificación.
 * - DESARROLLO → obligatorio
 * - EXPLORATORIO | ESTRATIGRAFICO → opcional
 */
export function isCampoRequired(clasificacion: string): boolean {
  return clasificacion === 'DESARROLLO';
}
