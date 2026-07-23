import { clearSession, getAccessToken } from "./authStorage";
import type {
  ApiProblem,
  AuthSession,
  Notification,
  PagedResult,
  RequestFilters,
  RequestHistory,
  RequestStatus,
  ServiceRequest
} from "../types";

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly problem?: ApiProblem
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getAccessToken();
  const headers = new Headers(init.headers);
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  if (token) headers.set("Authorization", `Bearer ${token}`);
  headers.set("X-Correlation-ID", crypto.randomUUID());

  const response = await fetch(path, { ...init, headers });
  if (response.status === 401 && token) {
    clearSession();
    window.dispatchEvent(new Event("serviceflow:unauthorized"));
  }

  if (!response.ok) {
    let problem: ApiProblem | undefined;
    try {
      problem = (await response.json()) as ApiProblem;
    } catch {
      problem = undefined;
    }
    const validationMessage = problem?.errors
      ? Object.values(problem.errors).flat().join(" ")
      : undefined;
    throw new ApiError(
      validationMessage || problem?.detail || problem?.title || "No fue posible completar la operación.",
      response.status,
      problem
    );
  }

  if (response.status === 204) return undefined as T;
  const contentType = response.headers.get("content-type");
  return contentType?.includes("application/json") ? ((await response.json()) as T) : (undefined as T);
}

function toQuery(params: Record<string, string | number | undefined>): string {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== "") query.set(key, String(value));
  });
  const value = query.toString();
  return value ? `?${value}` : "";
}

export const api = {
  login: (email: string, password: string) =>
    request<AuthSession>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password })
    }),

  getRequests: (filters: RequestFilters = {}) =>
    request<PagedResult<ServiceRequest>>(
      `/api/requests${toQuery({
        page: filters.page,
        pageSize: filters.pageSize,
        search: filters.search,
        status: filters.status,
        priority: filters.priority,
        category: filters.category,
        assignedTo: filters.assignedTo,
        sortBy: filters.sortBy,
        sortDirection: filters.sortDirection
      })}`
    ),

  getRequest: (id: number) => request<ServiceRequest>(`/api/requests/${id}`),

  createRequest: (input: {
    title: string;
    description: string;
    category: string;
    priority: string;
  }) => request<ServiceRequest>("/api/requests", { method: "POST", body: JSON.stringify(input) }),

  updateRequest: (
    id: number,
    input: { title: string; description: string; category: string; priority: string }
  ) => request<ServiceRequest>(`/api/requests/${id}`, { method: "PUT", body: JSON.stringify(input) }),

  changeStatus: (id: number, status: RequestStatus) =>
    request<ServiceRequest>(`/api/requests/${id}/status`, {
      method: "PATCH",
      body: JSON.stringify({ status })
    }),

  assignRequest: (id: number, assignedTo: string) =>
    request<ServiceRequest>(`/api/requests/${id}/assignment`, {
      method: "PATCH",
      body: JSON.stringify({ assignedTo })
    }),

  addComment: (id: number, content: string) =>
    request<ServiceRequest>(`/api/requests/${id}/comments`, {
      method: "POST",
      body: JSON.stringify({ content })
    }),

  getHistory: (id: number) => request<RequestHistory[]>(`/api/requests/${id}/history`),

  getNotifications: (page = 1, pageSize = 20, isRead?: boolean) =>
    request<PagedResult<Notification>>(
      `/api/notifications${toQuery({ page, pageSize, isRead: isRead === undefined ? undefined : String(isRead) })}`
    ),

  getUnreadNotifications: () => request<PagedResult<Notification>>("/api/notifications/unread"),
  getUnreadCount: () => request<{ count: number } | number>("/api/notifications/unread-count"),
  markNotificationRead: (id: string) =>
    request<void>(`/api/notifications/${id}/read`, { method: "PATCH" }),
  markAllNotificationsRead: () => request<void>("/api/notifications/read-all", { method: "PATCH" })
};
