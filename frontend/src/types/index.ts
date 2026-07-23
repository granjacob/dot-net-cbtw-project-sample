export type UserRole = "Employee" | "Agent" | "Administrator";

export interface User {
  email: string;
  name: string;
  role: UserRole;
}

export interface AuthSession {
  token: string;
  expiresAt: string;
  user: User;
}

export type RequestCategory =
  | "TechnicalSupport"
  | "Maintenance"
  | "SystemAccess"
  | "Purchasing"
  | "OperationalIncident";

export type RequestPriority = "Low" | "Medium" | "High" | "Critical";
export type RequestStatus = "Open" | "InProgress" | "Pending" | "Resolved" | "Closed";

export interface ServiceRequest {
  id: number;
  title: string;
  description: string;
  category: RequestCategory;
  priority: RequestPriority;
  status: RequestStatus;
  createdBy: string;
  assignedTo: string | null;
  dueAt: string;
  createdAt: string;
  updatedAt: string;
  comments?: RequestComment[];
}

export interface RequestComment {
  id: number;
  requestId: number;
  authorId: string;
  content: string;
  createdAt: string;
}

export interface RequestHistory {
  id: number;
  requestId: number;
  previousStatus: RequestStatus | null;
  newStatus: RequestStatus;
  changedBy: string;
  changedAt: string;
}

export interface Notification {
  id: string;
  userId: string;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
  eventId: string;
  requestId?: number;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface RequestFilters {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  priority?: string;
  category?: string;
  assignedTo?: string;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}

export interface RealtimeEvent {
  eventId?: string;
  eventType?: string;
  requestId?: number;
  userId?: string;
  title?: string;
  message?: string;
  occurredAt?: string;
  data?: Record<string, unknown>;
  [key: string]: unknown;
}

export interface ApiProblem {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

export const categoryLabels: Record<RequestCategory, string> = {
  TechnicalSupport: "Soporte técnico",
  Maintenance: "Mantenimiento",
  SystemAccess: "Acceso a sistemas",
  Purchasing: "Compras y suministros",
  OperationalIncident: "Incidente operativo"
};

export const priorityLabels: Record<RequestPriority, string> = {
  Low: "Baja",
  Medium: "Media",
  High: "Alta",
  Critical: "Crítica"
};

export const statusLabels: Record<RequestStatus, string> = {
  Open: "Abierta",
  InProgress: "En progreso",
  Pending: "Pendiente",
  Resolved: "Resuelta",
  Closed: "Cerrada"
};
