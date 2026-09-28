export interface ManagedUser { id: number; email: string; displayName: string; role: string; }
export interface ManagedUserRequest { displayName: string; email: string; password?: string; }
