import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import { StatusBadge, PriorityBadge } from "../components/Badges";
import { EmptyState, ErrorPanel, LoadingState } from "../components/Feedback";
import { useAuth } from "../contexts/AuthContext";
import { api } from "../services/api";
import {
  categoryLabels,
  priorityLabels,
  statusLabels,
  type RequestStatus
} from "../types";
import { formatDateTime, getApiMessage, getInitials } from "../utils/format";

const allowedTransitions: Record<RequestStatus, RequestStatus[]> = {
  Open: ["Pending", "InProgress", "Closed"],
  Pending: ["Open", "InProgress", "Closed"],
  InProgress: ["Pending", "Resolved", "Closed"],
  Resolved: ["InProgress", "Closed"],
  Closed: []
};

export function RequestDetailPage() {
  const { id: idParam } = useParams();
  const id = Number(idParam);
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const location = useLocation();
  const [comment, setComment] = useState("");
  const [assignedTo, setAssignedTo] = useState("");
  const [editing, setEditing] = useState(false);
  const [editValues, setEditValues] = useState({ title: "", description: "", category: "TechnicalSupport", priority: "Medium" });
  const [feedback, setFeedback] = useState<string | null>((location.state as { created?: boolean } | null)?.created ? "Solicitud creada correctamente." : null);
  const canManage = user?.role === "Agent" || user?.role === "Administrator";

  const requestQuery = useQuery({
    queryKey: ["request", id],
    queryFn: () => api.getRequest(id),
    enabled: Number.isFinite(id) && id > 0
  });
  const historyQuery = useQuery({
    queryKey: ["history", id],
    queryFn: () => api.getHistory(id),
    enabled: Number.isFinite(id) && id > 0
  });

  const refresh = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["request", id] }),
      queryClient.invalidateQueries({ queryKey: ["history", id] }),
      queryClient.invalidateQueries({ queryKey: ["requests"] })
    ]);
  };

  const statusMutation = useMutation({
    mutationFn: (status: RequestStatus) => api.changeStatus(id, status),
    onSuccess: async () => { setFeedback("Estado actualizado y evento publicado."); await refresh(); },
    onError: (error) => setFeedback(getApiMessage(error))
  });
  const assignmentMutation = useMutation({
    mutationFn: (email: string) => api.assignRequest(id, email),
    onSuccess: async () => { setFeedback("Responsable asignado correctamente."); setAssignedTo(""); await refresh(); },
    onError: (error) => setFeedback(getApiMessage(error))
  });
  const commentMutation = useMutation({
    mutationFn: (content: string) => api.addComment(id, content),
    onSuccess: async () => { setComment(""); setFeedback("Comentario agregado."); await refresh(); },
    onError: (error) => setFeedback(getApiMessage(error))
  });
  const updateMutation = useMutation({
    mutationFn: api.updateRequest.bind(null, id),
    onSuccess: async () => { setEditing(false); setFeedback("Información actualizada."); await refresh(); },
    onError: (error) => setFeedback(getApiMessage(error))
  });

  const request = requestQuery.data;
  const sla = useMemo(() => {
    if (!request) return null;
    const due = new Date(request.dueAt).getTime();
    const remaining = due - Date.now();
    const hours = Math.ceil(Math.abs(remaining) / 3_600_000);
    return { overdue: remaining < 0 && !["Resolved", "Closed"].includes(request.status), text: remaining < 0 ? `Venció hace ${hours} h` : `${hours} h restantes` };
  }, [request]);

  if (!Number.isFinite(id) || id <= 0) return <ErrorPanel message="El identificador de la solicitud no es válido." />;
  if (requestQuery.isLoading) return <LoadingState label="Abriendo la solicitud…" />;
  if (requestQuery.isError) return <ErrorPanel message={requestQuery.error.message} onRetry={() => void requestQuery.refetch()} />;
  if (!request) return <EmptyState title="Solicitud no encontrada" description="Es posible que haya sido eliminada o no tengas acceso." action={<Link className="button button-secondary" to="/requests">Volver al listado</Link>} />;

  const openEdit = () => {
    setEditValues({ title: request.title, description: request.description, category: request.category, priority: request.priority });
    setEditing(true);
  };
  const statusOptions = [request.status, ...allowedTransitions[request.status]];

  return (
    <div className="page detail-page">
      <Link to="/requests" className="back-link">← Volver a solicitudes</Link>
      {feedback && <div className="toast-inline" role="status"><span>✓</span>{feedback}<button onClick={() => setFeedback(null)}>×</button></div>}
      <header className="detail-header">
        <div>
          <span className="eyebrow">SF-{String(request.id).padStart(4, "0")}</span>
          <h1>{request.title}</h1>
          <div className="detail-badges"><StatusBadge status={request.status} /><PriorityBadge priority={request.priority} /><span className="category-chip">{categoryLabels[request.category] ?? request.category}</span></div>
        </div>
        {canManage && <button className="button button-secondary" onClick={openEdit}>✎ Editar información</button>}
      </header>

      <div className="detail-layout">
        <div className="detail-main">
          <section className="panel description-panel">
            <div className="panel-header"><div><span className="eyebrow">CONTEXTO</span><h2>Descripción</h2></div></div>
            <p className="request-description">{request.description}</p>
          </section>

          {canManage && (
            <section className="panel manage-panel">
              <div className="panel-header"><div><span className="eyebrow">GESTIÓN</span><h2>Actualizar solicitud</h2></div></div>
              <div className="manage-grid">
                <label className="field"><span>Cambiar estado</span><div className="inline-control"><select value={request.status} disabled={statusMutation.isPending} onChange={(event) => statusMutation.mutate(event.target.value as RequestStatus)}>{statusOptions.map((status) => <option value={status} key={status}>{statusLabels[status]}</option>)}</select>{statusMutation.isPending && <span className="spinner small" />}</div></label>
                <label className="field"><span>Asignar responsable</span><div className="inline-control"><input value={assignedTo} onChange={(event) => setAssignedTo(event.target.value)} placeholder="agente@empresa.com" type="email" /><button className="button button-secondary" disabled={!assignedTo.trim() || assignmentMutation.isPending} onClick={() => assignmentMutation.mutate(assignedTo.trim())}>Asignar</button></div></label>
              </div>
            </section>
          )}

          <section className="panel comments-panel">
            <div className="panel-header"><div><span className="eyebrow">CONVERSACIÓN</span><h2>Comentarios <span>{request.comments?.length ?? 0}</span></h2></div></div>
            <form className="comment-form" onSubmit={(event) => { event.preventDefault(); if (comment.trim().length >= 2) commentMutation.mutate(comment.trim()); }}>
              <span className="avatar">{getInitials(user?.name ?? "Yo")}</span>
              <div><textarea value={comment} onChange={(event) => setComment(event.target.value)} placeholder="Escribe una actualización o agrega contexto…" rows={3} maxLength={2000} /><div><small>{comment.length}/2000</small><button className="button button-primary button-small" disabled={comment.trim().length < 2 || commentMutation.isPending}>{commentMutation.isPending ? "Publicando…" : "Publicar comentario"}</button></div></div>
            </form>
            <div className="comments-list">
              {[...(request.comments ?? [])].reverse().map((item) => (
                <article className="comment" key={item.id}>
                  <span className="avatar">{getInitials(item.authorId)}</span>
                  <div><div><strong>{item.authorId}</strong><time>{formatDateTime(item.createdAt)}</time></div><p>{item.content}</p></div>
                </article>
              ))}
              {!request.comments?.length && <div className="inline-empty">Todavía no hay comentarios. Inicia la conversación.</div>}
            </div>
          </section>
        </div>

        <aside className="detail-aside">
          <section className="panel detail-meta">
            <span className="eyebrow">DETALLES</span>
            <dl>
              <div><dt>Creada por</dt><dd><span className="mini-avatar">{request.createdBy[0]?.toUpperCase()}</span>{request.createdBy}</dd></div>
              <div><dt>Responsable</dt><dd>{request.assignedTo ? <><span className="mini-avatar mint">{request.assignedTo[0].toUpperCase()}</span>{request.assignedTo}</> : <span className="unassigned">Sin asignar</span>}</dd></div>
              <div><dt>Creada</dt><dd>{formatDateTime(request.createdAt)}</dd></div>
              <div><dt>Última actualización</dt><dd>{formatDateTime(request.updatedAt)}</dd></div>
            </dl>
          </section>
          <section className={`sla-card ${sla?.overdue ? "sla-overdue" : ""}`}>
            <div><span>◷</span><div><small>ACUERDO DE SERVICIO</small><strong>{sla?.text}</strong></div></div>
            <p>Fecha objetivo: {formatDateTime(request.dueAt)}</p>
          </section>
          <section className="panel history-panel">
            <div className="panel-header"><div><span className="eyebrow">TRAZABILIDAD</span><h2>Historial</h2></div></div>
            {historyQuery.isLoading ? <LoadingState label="Cargando historial…" /> : historyQuery.isError ? <p className="inline-error">{historyQuery.error.message}</p> : (
              <ol className="timeline">
                {[...(historyQuery.data ?? [])].reverse().map((item) => (
                  <li key={item.id}><i /><div><strong>{item.previousStatus ? `${statusLabels[item.previousStatus]} → ` : "Creada como "}{statusLabels[item.newStatus]}</strong><span>{item.changedBy}</span><time>{formatDateTime(item.changedAt)}</time></div></li>
                ))}
                {!historyQuery.data?.length && <li><i /><div><strong>Solicitud creada</strong><time>{formatDateTime(request.createdAt)}</time></div></li>}
              </ol>
            )}
          </section>
        </aside>
      </div>

      {editing && (
        <div className="modal-backdrop" role="presentation" onMouseDown={() => setEditing(false)}>
          <div className="modal" role="dialog" aria-modal="true" aria-labelledby="edit-title" onMouseDown={(event) => event.stopPropagation()}>
            <div className="modal-header"><div><span className="eyebrow">SF-{String(id).padStart(4, "0")}</span><h2 id="edit-title">Editar solicitud</h2></div><button onClick={() => setEditing(false)} aria-label="Cerrar">×</button></div>
            <form onSubmit={(event) => { event.preventDefault(); updateMutation.mutate(editValues); }} className="form-stack">
              <label className="field"><span>Título</span><input value={editValues.title} minLength={5} maxLength={160} required onChange={(event) => setEditValues((value) => ({ ...value, title: event.target.value }))} /></label>
              <label className="field"><span>Descripción</span><textarea value={editValues.description} minLength={20} maxLength={4000} required rows={6} onChange={(event) => setEditValues((value) => ({ ...value, description: event.target.value }))} /></label>
              <div className="form-grid-2">
                <label className="field"><span>Categoría</span><select value={editValues.category} onChange={(event) => setEditValues((value) => ({ ...value, category: event.target.value }))}>{Object.entries(categoryLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
                <label className="field"><span>Prioridad</span><select value={editValues.priority} onChange={(event) => setEditValues((value) => ({ ...value, priority: event.target.value }))}>{Object.entries(priorityLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
              </div>
              <div className="form-actions"><button type="button" className="button button-ghost" onClick={() => setEditing(false)}>Cancelar</button><button className="button button-primary" disabled={updateMutation.isPending}>{updateMutation.isPending ? "Guardando…" : "Guardar cambios"}</button></div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
