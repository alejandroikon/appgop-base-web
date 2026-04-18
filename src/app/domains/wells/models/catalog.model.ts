// Modelos de dominio para catálogos del dominio wells (V2.0)

export interface Contrato {
  id:               number;
  nombre:           string;
  tipo:             string;
  cuenca:           string;
  ubicacionDefault?: 'CONTINENTAL' | 'COSTA_FUERA';
}

export interface Campo {
  id:         number;
  nombre:     string;
  contratoId: number;
}

export interface Departamento {
  id:         number;
  nombre:     string;
  codigoDane: string;
}

export interface Municipio {
  id:             number;
  nombre:         string;
  departamentoId: number;
  codigoDane:     string;
}

export interface Cluster {
  id:          number;
  nombre:      string;
  abreviatura?: string;
  campoId:     number;
}
