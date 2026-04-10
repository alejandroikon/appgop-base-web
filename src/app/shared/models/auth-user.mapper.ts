import { AuthUser } from './auth-user.model';
import { AuthUserDTO } from './auth-user.dto';
import { UserRole } from './user-role.model';

export function mapAuthUserDTOToModel(dto: AuthUserDTO): AuthUser {
  return {
    id: dto.user_id,
    email: dto.email,
    name: dto.full_name,
    role: dto.role_code as UserRole,
    tenantId: dto.tenant_id,
    tenantName: dto.tenant_name,
  };
}
