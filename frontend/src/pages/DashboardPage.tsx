import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { StatusBadge, PriorityBadge } from "../components/Badges";
import { ErrorPanel, LoadingState } from "../components/Feedback";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../contexts/AuthContext";
import { api } from "../services/api";
import { statusLabels, type RequestStatus, type ServiceRequest } from "../types";
import { formatRelative } from "../utils/format";

const statuses: RequestStatus[] = ["Open", "InProgress", "Pending", "Resolved", "Closed"];

function metricDelta(requests: ServiceRequest[]): string {
  const lastSevenDays = Date.now() - 7 * 86_400_000;
  const recent = requests.filter((item) => new Date(item.createdAt).getTime() >= lastSevenDays).length;
  return recent > 0 ? `+${recent} esta semana` : "Sin nuevas esta semana";
}

export function DashboardPage() {
  const { user } = useAuth();
  const requestsQuery = useQuery({
    queryKey: ["requests", "dashboard"],
    queryFn: () => api.getRequests({ page: 1, pageSize: 100, sortBy: "updatedAt", sortDirection: "desc" })
  });

  if (requestsQuery.isLoading) return <LoadingState label="Preparando tu tablero…" />;
  if (requestsQuery.isError) return <ErrorPanel message={requestsQuery.error.message} onRetry={() => void requestsQuery.refetch()} />;

  const requests = requestsQuery.data?.items ?? [];
  const open = requests.filter((request) => !["Resolved", "Closed"].includes(request.status)).length;
  const critical = requests.filter((request) => request.priority === "Critical" && request.status !== "Closed").length;
  const resolved = requests.filter((request) => request.status === "Resolved" || request.status === "Closed").length;
  const assigned = requests.filter((request) => request.assignedTo === user?.email).length;
  const maxStatus = Math.max(...statuses.map((status) => requests.filter((request) => request.status === status).length), 1);
  const firstName = user?.name.split(" ")[0] ?? "equipo";

  return (
    <div className="page dashboard-page">
      <PageHeader
        eyebrow="PANORAMA DE HOY"
        title={`Hola, ${firstName}`}
        description="Aquí tienes el pulso de las solicitudes y la actividad más reciente."
        actions={<Link to="/requests/new" className="button button-primary"><span>＋</span> Nueva solicitud</Link>}
      />

      <section className="metrics-grid" aria-label="Métricas generales">
        <article className="metric-card metric-navy">
          <div className="metric-icon">▤</div><span>Solicitudes abiertas</span><strong>{open}</strong><small>{metricDelta(requests)}</small>
          <i className="metric-accent" />
        </article>
        <article className="metric-card">
          <div className="metric-icon amber">!</div><span>Prioridad crítica</span><strong>{critical}</strong><small>{critical ? "Requieren atención" : "Todo bajo control"}</small>
          <i className="metric-accent amber" />
        </article>
        <article className="metric-card">
          <div className="metric-icon mint">✓</div><span>Resueltas</span><strong>{resolved}</strong><small>{requests.length ? `${Math.round((resolved / requests.length) * 100)}% del total` : "Sin datos aún"}</small>
          <i className="metric-accent mint" />
        </article>
        <article className="metric-card">
          <div className="metric-icon blue">◎</div><span>Asignadas a ti</span><strong>{assigned}</strong><small>{assigned ? "En tu bandeja" : "Bandeja despejada"}</small>
          <i className="metric-accent blue" />
        </article>
      </section>

      <section className="dashboard-grid">
        <article className="panel activity-panel">
          <div className="panel-header"><div><span className="eyebrow">ACTIVIDAD</span><h2>Solicitudes recientes</h2></div><Link to="/requests">Ver todas →</Link></div>
          <div className="request-list-compact">
            {requests.slice(0, 6).map((request) => (
              <Link to={`/requests/${request.id}`} key={request.id} className="request-row">
                <div className={`request-symbol priority-bg-${request.priority.toLowerCase()}`}>SF</div>
                <div className="request-main"><strong>{request.title}</strong><span>SF-{String(request.id).padStart(4, "0")} · {request.createdBy}</span></div>
                <PriorityBadge priority={request.priority} />
                <StatusBadge status={request.status} />
                <time>{formatRelative(request.updatedAt)}</time>
                <span className="row-arrow">›</span>
              </Link>
            ))}
            {requests.length === 0 && <div className="inline-empty">Aún no hay solicitudes. Crea la primera para comenzar.</div>}
          </div>
        </article>

        <article className="panel distribution-panel">
          <div className="panel-header"><div><span className="eyebrow">DISTRIBUCIÓN</span><h2>Estado actual</h2></div><span className="panel-total">{requests.length} total</span></div>
          <div className="status-chart">
            {statuses.map((status) => {
              const count = requests.filter((request) => request.status === status).length;
              return (
                <div className="chart-row" key={status}>
                  <div><span>{statusLabels[status]}</span><strong>{count}</strong></div>
                  <div className="chart-track"><i className={`chart-${status.toLowerCase()}`} style={{ width: `${Math.max((count / maxStatus) * 100, count ? 8 : 0)}%` }} /></div>
                </div>
              );
            })}
          </div>
          <div className="resolution-summary">
            <span className="resolution-ring" style={{ "--progress": `${requests.length ? (resolved / requests.length) * 360 : 0}deg` } as React.CSSProperties}><i>{requests.length ? Math.round((resolved / requests.length) * 100) : 0}%</i></span>
            <div><strong>Tasa de resolución</strong><small>{resolved} de {requests.length} solicitudes completadas</small></div>
          </div>
        </article>
      </section>
    </div>
  );
}
