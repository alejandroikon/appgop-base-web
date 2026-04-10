export interface AuthUserDTO {
  user_id: string;
  email: string;
  full_name: string;
  role_code: string;
  tenant_id: string;
  tenant_name: string;
}

export interface LoginCredentials {
  email: string;
  password: string;
}
