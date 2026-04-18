// Algoritmo de generación de UWI Fiscalizado (PPDM)
// Referencia: docs/features/creacion-pozo/uwi-algorithm.md
// Reglas: RN-27 a RN-36 (spec.md)

export interface UwiParams {
  codigoDaneDpto:  string;
  codigoDaneMpio:  string;
  denominacion:    string;
  consecutivo:     number;
  clusterNombre:   string | null;
  clusterAbreviatura?: string | null;
  tipoAngulo:      string;
  tipoTrayectoria: string;
  tipoObjetivo:    string;
  tipoTerminacion: string;
  isAnh:           boolean;
}

export interface UwiComponents {
  dptoCode:        string;
  mpioCode:        string;
  sigla:           string;
  numero:          string;
  clusterCode:     string;
  anguloCode:      string;
  trayectoriaCode: string;
  objetivoCode:    string;
  terminacionCode: string;
}

export interface UwiResult {
  uwi:        string;
  components: UwiComponents;
}

// ─── Segmento 1: DptoDANE — 2 dígitos (RN-28) ────────────────────────────────

/** Extrae los 2 primeros dígitos del código DANE del departamento. */
function buildDptoCode(codigoDaneDpto: string): string {
  return codigoDaneDpto.slice(0, 2).padStart(2, '0');
}

// ─── Segmento 2: MpioDANE — 3 dígitos (RN-29) ────────────────────────────────

/**
 * Extrae la parte municipal del código DANE (3 últimos dígitos de los 5 totales).
 * Si codigoDaneMpio ya tiene 3 chars lo usa directamente.
 * Si tiene 5 chars (e.g. "50568") toma los 3 últimos.
 */
function buildMpioCode(codigoDaneMpio: string): string {
  const cleaned = codigoDaneMpio.replace(/\D/g, '');
  if (cleaned.length === 5) return cleaned.slice(2).padStart(3, '0');
  return cleaned.slice(-3).padStart(3, '0');
}

// ─── Segmento 3: Sigla — 4 chars MAYÚSCULAS (RN-30) ──────────────────────────

/**
 * Determina palabras significativas (ignora artículos y preposiciones).
 * "de", "del", "la", "el", "los", "las", "y" se filtran como no-significativas.
 */
const STOP_WORDS = new Set(['de', 'del', 'la', 'el', 'los', 'las', 'y', 'en', 'al', 'un', 'una']);

function getSignificantWords(denominacion: string): string[] {
  return denominacion
    .trim()
    .split(/\s+/)
    .filter((w) => w.length > 0)
    .map((w) => w.replace(/[^A-Za-záéíóúÁÉÍÓÚñÑ]/g, ''))
    .filter((w) => w.length > 0)
    .filter((w) => !STOP_WORDS.has(w.toLowerCase()));
}

/**
 * Genera la sigla de 4 caracteres para el UWI (RN-30).
 * - 1 palabra → primeras 4 letras, padding con 'X'
 * - 2+ palabras → 2 letras de p1 + 2 letras de p2
 * - ANH → 'ANH' + primera letra de denominación
 */
export function computeSigla(denominacion: string, isAnh: boolean): string {
  const upper = denominacion.toUpperCase().replace(/[^A-ZÁÉÍÓÚÑ\s]/g, '');
  const words = getSignificantWords(upper);

  if (isAnh) {
    const firstChar = words[0]?.[0] ?? 'X';
    return `ANH${firstChar}`;
  }

  if (words.length === 1) {
    return words[0].slice(0, 4).padEnd(4, 'X');
  }

  const p1 = words[0].slice(0, 2);
  const p2 = words[1].slice(0, 2);
  return (p1 + p2).padEnd(4, 'X');
}

// ─── Segmento 4: Número — 4 dígitos zero-padding (RN-31) ─────────────────────

function buildNumero(consecutivo: number): string {
  return consecutivo.toString().padStart(4, '0');
}

// ─── Segmento 5: Cluster — 2 alfa + 4 num (RN-32) ────────────────────────────

/**
 * Genera el código de cluster de 6 chars.
 * - Con cluster: abreviatura (2 alpha) + número secuencial (4 digits)
 * - Sin cluster: 'CX' + '0000'
 */
export function computeClusterCode(
  clusterNombre: string | null,
  clusterAbreviatura?: string | null,
): string {
  if (!clusterNombre) return 'CX0000';

  let abrev: string;
  if (clusterAbreviatura && clusterAbreviatura.length >= 2) {
    abrev = clusterAbreviatura.toUpperCase().slice(0, 2);
  } else {
    // Fallback: primeras 2 letras del nombre del cluster
    abrev = clusterNombre
      .toUpperCase()
      .replace(/[^A-ZÁÉÍÓÚÑ]/g, '')
      .slice(0, 2)
      .padEnd(2, 'X');
  }

  // Extraer número del cluster (si aparece al final) — ej. "Cluster Norte 3" → 0003
  const numMatch = clusterNombre.match(/\d+$/);
  const num = numMatch ? parseInt(numMatch[0], 10).toString().padStart(4, '0') : '0000';

  return abrev + num;
}

// ─── Segmento 6: Ángulo — 1 char (RN-33) ──────────────────────────────────────

function buildAnguloCode(tipoAngulo: string): string {
  return tipoAngulo.toUpperCase();
}

// ─── Segmento 7: Trayectoria — variable (RN-34) ───────────────────────────────

/**
 * Construye el segmento de trayectoria.
 * Original (O) → vacío. Resto → código directo (ST, P, PR, ML, G).
 */
function buildTrayectoriaCode(tipoTrayectoria: string): string {
  return tipoTrayectoria === 'O' ? '' : tipoTrayectoria;
}

// ─── Segmento 8: Objetivo — variable (RN-35) ──────────────────────────────────

function buildObjetivoCode(tipoObjetivo: string): string {
  return tipoObjetivo;
}

// ─── Segmento 9: Terminación — con guión (RN-36) ─────────────────────────────

function buildTerminacionCode(tipoTerminacion: string): string {
  return tipoTerminacion;
}

// ─── Función principal ────────────────────────────────────────────────────────

/**
 * Genera el preview del UWI completo a partir de los parámetros del formulario.
 * Implementa el algoritmo PPDM completo (RN-27 a RN-36).
 */
export function generateUwiPreview(params: UwiParams): UwiResult {
  const dptoCode        = buildDptoCode(params.codigoDaneDpto);
  const mpioCode        = buildMpioCode(params.codigoDaneMpio);
  const sigla           = computeSigla(params.denominacion, params.isAnh);
  const numero          = buildNumero(params.consecutivo);
  const clusterCode     = computeClusterCode(params.clusterNombre, params.clusterAbreviatura);
  const anguloCode      = buildAnguloCode(params.tipoAngulo);
  const trayectoriaCode = buildTrayectoriaCode(params.tipoTrayectoria);
  const objetivoCode    = buildObjetivoCode(params.tipoObjetivo);
  const terminacionCode = buildTerminacionCode(params.tipoTerminacion);

  const uwi = [
    dptoCode,
    mpioCode,
    sigla,
    numero,
    clusterCode,
    anguloCode,
    trayectoriaCode,
    objetivoCode,
    '-',
    terminacionCode,
  ].join('');

  return {
    uwi,
    components: {
      dptoCode,
      mpioCode,
      sigla,
      numero,
      clusterCode,
      anguloCode,
      trayectoriaCode,
      objetivoCode,
      terminacionCode,
    },
  };
}
