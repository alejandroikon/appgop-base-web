// Modelos de dominio para catálogos del dominio wells

export interface Contrato {
  id: number;
  nombre: string;
  tipo: string;
  cuenca: string;
}

export interface Campo {
  id: number;
  nombre: string;
  contratoId: number;
}

export interface Departamento {
  id: number;
  nombre: string;
  codigoDane: string;
}

export interface Municipio {
  id: number;
  nombre: string;
  departamentoId: number;
  codigoDane: string;
}

export interface Cluster {
  id: number;
  nombre: string;
  campoId: number;
}
