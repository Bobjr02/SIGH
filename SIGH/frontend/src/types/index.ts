// Definicao basica de tipos do SIGH (Foundation)

export interface UserSession {
  id: string;
  name: string;
  email: string;
  role: string;
}

export interface ApiResponse<T> {
  data: T;
  message?: string;
  success: boolean;
  errors?: string[];
}

export interface BaseEntityDto {
  id: string;
  createdAt: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
}
