/** Refleja contract.yml → schemas/UserProfile */
export interface UserProfileDTO {
  id: string;
  email: string;
  name: string;
  role: string;
  tenantId: string;
  tenantName: string;
}

/** Refleja contract.yml → schemas/TokenResponse */
export interface TokenResponseDTO {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: UserProfileDTO;
}

/** Refleja contract.yml → schemas/LoginRequest */
export interface LoginRequestDTO {
  email: string;
  password: string;
}

/** Refleja contract.yml → schemas/RefreshTokenRequest */
export interface RefreshTokenRequestDTO {
  refreshToken: string;
}

/** Alias para compatibilidad con el store (forma idéntica a LoginRequestDTO) */
export interface LoginCredentials {
  email: string;
  password: string;
}
