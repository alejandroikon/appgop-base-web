import { AuthUser } from './auth-user.model';
import { UserProfileDTO } from './auth-user.dto';
import { UserRole } from './user-role.model';

export function mapUserProfileDTOToModel(dto: UserProfileDTO): AuthUser {
  return {
    id: dto.id,
    email: dto.email,
    name: dto.name,
    role: dto.role as UserRole,
    tenantId: dto.tenantId,
    tenantName: dto.tenantName,
  };
}
