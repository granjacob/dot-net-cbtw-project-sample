import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link } from "react-router-dom";
import { EmptyState, ErrorPanel, LoadingState } from "../components/Feedback";
import { PageHeader } from "../components/PageHeader";
import { api } from "../services/api";
import type { Notification } from "../types";
import { formatDateTime, formatRelative } from "../utils/format";

function notificationIcon(type: string): string {
  if (type.includes("Status")) return "↻";
  if (type.includes("Comment")) return "···";
  if (type.includes("Assigned")) return "◎";
  if (type.includes("Created")) return "+";
  return "◇";
}

export function NotificationsPage() {
  const [page, setPage] = useState(1);
  const [filter, setFilter] = useState<"all" | "unread">("all");
  const queryClient = useQueryClient();
  const notificationsQuery = useQuery({
    queryKey: ["notifications", page, filter],
    queryFn: () => api.getNotifications(page, 20, filter === "unread" ? false : undefined),
    placeholderData: (previous) => previous
  });
  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: ["notifications"] });
    await queryClient.invalidateQueries({ queryKey: ["unread-count"] });
  };
  const markRead = useMutation({ mutationFn: api.markNotificationRead, onSuccess: refresh });
  const markAll = useMutation({ mutationFn: api.markAllNotificationsRead, onSuccess: refresh });
  const data = notificationsQuery.data;
  const unreadOnPage = data?.items.filter((item) => !item.isRead).length ?? 0;

  const openNotification = (notification: Notification) => {
    if (!notification.isRead) markRead.mutate(notification.id);
  };

  return (
    <div className="page notifications-page">
      <PageHeader
        eyebrow="CENTRO DE ACTIVIDAD"
        title="Notificaciones"
        description="Los cambios en tus solicitudes aparecen aquí y se sincronizan en tiempo real."
        actions={unreadOnPage > 0 ? <button className="button button-secondary" onClick={() => markAll.mutate()} disabled={markAll.isPending}>✓ Marcar todas como leídas</button> : undefined}
      />
      <section className="panel notifications-panel">
        <div className="notification-tabs" role="tablist">
          <button className={filter === "all" ? "active" : ""} onClick={() => { setFilter("all"); setPage(1); }}>Todas</button>
          <button className={filter === "unread" ? "active" : ""} onClick={() => { setFilter("unread"); setPage(1); }}>Sin leer</button>
          <span>{data?.total ?? 0} registros</span>
        </div>
        {notificationsQuery.isLoading ? <LoadingState label="Cargando notificaciones…" /> : notificationsQuery.isError ? <ErrorPanel message={notificationsQuery.error.message} onRetry={() => void notificationsQuery.refetch()} /> : !data?.items.length ? (
          <EmptyState icon="◇" title={filter === "unread" ? "Estás al día" : "Aún no hay notificaciones"} description={filter === "unread" ? "No tienes actualizaciones pendientes por revisar." : "Cuando una solicitud cambie, verás la actividad aquí."} />
        ) : (
          <>
            <div className="notification-list">
              {data.items.map((notification) => {
                const content = (
                  <>
                    <span className={`notification-type type-${notification.type.toLowerCase()}`}>{notificationIcon(notification.type)}</span>
                    <div className="notification-copy"><div><strong>{notification.title}</strong>{!notification.isRead && <i className="unread-dot" />}</div><p>{notification.message}</p><span title={formatDateTime(notification.createdAt)}>{formatRelative(notification.createdAt)} · {notification.type}</span></div>
                    {!notification.isRead && <button className="mark-read" onClick={(event) => { event.preventDefault(); event.stopPropagation(); markRead.mutate(notification.id); }} title="Marcar como leída">✓</button>}
                    <span className="row-arrow">›</span>
                  </>
                );
                return notification.requestId ? <Link to={`/requests/${notification.requestId}`} onClick={() => openNotification(notification)} className={`notification-item ${notification.isRead ? "read" : ""}`} key={notification.id}>{content}</Link> : <article onClick={() => openNotification(notification)} className={`notification-item ${notification.isRead ? "read" : ""}`} key={notification.id}>{content}</article>;
              })}
            </div>
            <div className="pagination"><span>Página {data.page} de {Math.max(data.totalPages, 1)}</span><div><button disabled={data.page <= 1} onClick={() => setPage(data.page - 1)}>← Anterior</button><button disabled={data.page >= data.totalPages} onClick={() => setPage(data.page + 1)}>Siguiente →</button></div></div>
          </>
        )}
      </section>
    </div>
  );
}
