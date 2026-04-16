// DTOs de catálogos: reflejan el contrato OpenAPI contract.yml

export interface ContratoItemDTO {
  id: number;
  nombre: string;
  tipo: string;
  cuenca: string;
}

export interface CampoItemDTO {
  id: number;
  nombre: string;
  contratoId: number;
}

export interface DepartamentoItemDTO {
  id: number;
  nombre: string;
  codigoDane: string;
}

export interface MunicipioItemDTO {
  id: number;
  nombre: string;
  departamentoId: number;
  codigoDane: string;
}

export interface ClusterItemDTO {
  id: number;
  nombre: string;
  campoId: number;
}
