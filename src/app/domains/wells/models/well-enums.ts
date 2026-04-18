// ─── Tipos literales alineados con contract.yml V2.0 ─────────────────────────

export type WellStatus        = 'BORRADOR' | 'CREADO';
export type TipoTrayectoria   = 'ST' | 'P' | 'PR' | 'ML' | 'G' | 'O';
export type Clasificacion     = 'EXPLORATORIO' | 'DESARROLLO' | 'ESTRATIGRAFICO';
export type SubClasificacion  = 'A3' | 'A2a' | 'A2b' | 'A2c' | 'A1';
export type TipoUbicacion     = 'CONTINENTAL' | 'COSTA_FUERA';
export type TipoAngulo        = 'H' | 'V' | 'D';
export type TipoObjetivo      = 'PH' | 'I' | 'M' | 'D' | 'C' | 'GT' | 'O';
export type TipoTerminacion   = 'CD' | 'LC' | 'LR' | 'GP' | 'CC' | 'OH' | 'O';

// ─── Opciones para dropdowns ──────────────────────────────────────────────────

export const TIPO_TRAYECTORIA_OPTIONS: { label: string; value: TipoTrayectoria }[] = [
  { label: 'Original (O)',          value: 'O'  },
  { label: 'Side Track (ST)',       value: 'ST' },
  { label: 'Piloto (P)',            value: 'P'  },
  { label: 'Profundización (PR)',   value: 'PR' },
  { label: 'Multilateral (ML)',     value: 'ML' },
  { label: 'Gemelo (G)',            value: 'G'  },
];

export const CLASIFICACION_OPTIONS: { label: string; value: Clasificacion }[] = [
  { label: 'Exploratorio',   value: 'EXPLORATORIO'   },
  { label: 'Desarrollo',     value: 'DESARROLLO'     },
  { label: 'Estratigráfico', value: 'ESTRATIGRAFICO' },
];

export const SUB_CLASIFICACION_OPTIONS: { label: string; value: SubClasificacion }[] = [
  { label: 'A3 — Área nueva (wildcat)',               value: 'A3'  },
  { label: 'A2a — Yacimiento nuevo, campo nuevo',     value: 'A2a' },
  { label: 'A2b — Yacimiento nuevo, campo conocido',  value: 'A2b' },
  { label: 'A2c — Yacimiento conocido, campo conocido', value: 'A2c' },
  { label: 'A1 — Avanzada (appraisal)',               value: 'A1'  },
];

export const TIPO_UBICACION_OPTIONS: { label: string; value: TipoUbicacion }[] = [
  { label: 'Continental',  value: 'CONTINENTAL' },
  { label: 'Costa Fuera',  value: 'COSTA_FUERA' },
];

export const TIPO_ANGULO_OPTIONS: { label: string; value: TipoAngulo }[] = [
  { label: 'Horizontal (H)',  value: 'H' },
  { label: 'Vertical (V)',    value: 'V' },
  { label: 'Desviado (D)',    value: 'D' },
];

export const TIPO_OBJETIVO_OPTIONS: { label: string; value: TipoObjetivo }[] = [
  { label: 'Producción de Hidrocarburos (PH)', value: 'PH' },
  { label: 'Inyección (I)',                    value: 'I'  },
  { label: 'Monitoreo (M)',                    value: 'M'  },
  { label: 'Disposición (D)',                  value: 'D'  },
  { label: 'Captación (C)',                    value: 'C'  },
  { label: 'Geotérmico (GT)',                  value: 'GT' },
  { label: 'Otro (O)',                         value: 'O'  },
];

export const TIPO_TERMINACION_OPTIONS: { label: string; value: TipoTerminacion }[] = [
  { label: 'Casing y Cementación (CD)',  value: 'CD' },
  { label: 'Liner Cementado (LC)',        value: 'LC' },
  { label: 'Liner con Ranuras (LR)',      value: 'LR' },
  { label: 'Gravel Pack (GP)',            value: 'GP' },
  { label: 'Completación Compuesta (CC)', value: 'CC' },
  { label: 'Hoyo Abierto (OH)',           value: 'OH' },
  { label: 'Otro (O)',                    value: 'O'  },
];

export const WELL_STATUS_OPTIONS: { label: string; value: WellStatus }[] = [
  { label: 'Borrador', value: 'BORRADOR' },
  { label: 'Creado',   value: 'CREADO'   },
];
