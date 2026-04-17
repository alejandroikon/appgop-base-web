// DTO para el endpoint GET /api/v1/wells/preview-name
// Refleja exactamente el schema WellNamePreview del contract.yml (006-well-creation-form)

export interface WellNamePreviewDTO {
  /** Nombre calculado del pozo: {cuenca}-{denominación}-{consecutivo} */
  nombrePozo: string;
  /** true si no existe otro pozo con este nombre; false si ya está en uso */
  available: boolean;
}
