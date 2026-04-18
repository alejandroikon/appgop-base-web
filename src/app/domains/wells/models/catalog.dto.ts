// DTOs de catálogos V2.0: reflejan el contrato OpenAPI

export interface ContratoItemDTO {
  id:               number;
  nombre:           string;
  tipo:             string;
  cuenca:           string;
  ubicacionDefault?: string;
}

export interface CampoItemDTO {
  id:         number;
  nombre:     string;
  contratoId: number;
}

export interface DepartamentoItemDTO {
  id:         number;
  nombre:     string;
  codigoDane: string;
}

export interface MunicipioItemDTO {
  id:             number;
  nombre:         string;
  departamentoId: number;
  codigoDane:     string;
}

export interface ClusterItemDTO {
  id:           number;
  nombre:       string;
  abreviatura?: string;
  campoId:      number;
}

export interface CreateClusterRequestDTO {
  nombre:  string;
  campoId: number;
}
