// ─── Tipos literales alineados con contract.yml ───────────────────────────────

export type WellStatus = 'BORRADOR' | 'PENDING_UWI' | 'READY_FISCAL' | 'FISCALIZADO';
export type TipoTrayectoria = 'ST' | 'P' | 'PR' | 'ML' | 'G' | 'O';
export type Clasificacion = 'EXPLORATORIO' | 'DESARROLLO' | 'ESTRATIGRAFICO';
export type TipoUbicacion = 'CONTINENTAL' | 'COSTA_FUERA';
export type TipoAngulo = 'H' | 'V' | 'D';
export type TipoObjetivo = 'PH' | 'I' | 'M' | 'D';
export type TipoTerminacion = 'CD' | 'LC' | 'LR' | 'GP' | 'CC' | 'OH' | 'O';

// ─── Opciones para dropdowns ──────────────────────────────────────────────────

export const TIPO_TRAYECTORIA_OPTIONS: { label: string; value: TipoTrayectoria }[] = [
  { label: 'Vertical simple (ST)', value: 'ST' },
  { label: 'Pozo en S (P)',        value: 'P'  },
  { label: 'Pozo en S inversa (PR)', value: 'PR' },
  { label: 'Multi-lateral (ML)',   value: 'ML' },
  { label: 'Geotérmico (G)',       value: 'G'  },
  { label: 'Otro (O)',             value: 'O'  },
];

export const CLASIFICACION_OPTIONS: { label: string; value: Clasificacion }[] = [
  { label: 'Exploratorio',   value: 'EXPLORATORIO'   },
  { label: 'Desarrollo',     value: 'DESARROLLO'     },
  { label: 'Estratigráfico', value: 'ESTRATIGRAFICO' },
];

export const TIPO_UBICACION_OPTIONS: { label: string; value: TipoUbicacion }[] = [
  { label: 'Continental',  value: 'CONTINENTAL' },
  { label: 'Costa Fuera',  value: 'COSTA_FUERA' },
];

export const TIPO_ANGULO_OPTIONS: { label: string; value: TipoAngulo }[] = [
  { label: 'Horizontal (H)',  value: 'H' },
  { label: 'Vertical (V)',    value: 'V' },
  { label: 'Direccional (D)', value: 'D' },
];

export const TIPO_OBJETIVO_OPTIONS: { label: string; value: TipoObjetivo }[] = [
  { label: 'Primario de Hidrocarburos (PH)', value: 'PH' },
  { label: 'Inyección (I)',                  value: 'I'  },
  { label: 'Monitoreo (M)',                  value: 'M'  },
  { label: 'Disposición (D)',                value: 'D'  },
];

export const TIPO_TERMINACION_OPTIONS: { label: string; value: TipoTerminacion }[] = [
  { label: 'Casing y Cementación (CD)', value: 'CD' },
  { label: 'Liner Cementado (LC)',       value: 'LC' },
  { label: 'Liner con Ranuras (LR)',     value: 'LR' },
  { label: 'Gravel Pack (GP)',           value: 'GP' },
  { label: 'Completación Compuesta (CC)', value: 'CC' },
  { label: 'Hoyo Abierto (OH)',          value: 'OH' },
  { label: 'Otro (O)',                   value: 'O'  },
];

export const WELL_STATUS_OPTIONS: { label: string; value: WellStatus }[] = [
  { label: 'Borrador',       value: 'BORRADOR'      },
  { label: 'Pendiente UWI',  value: 'PENDING_UWI'   },
  { label: 'Listo Fiscal',   value: 'READY_FISCAL'  },
  { label: 'Fiscalizado',    value: 'FISCALIZADO'   },
];
