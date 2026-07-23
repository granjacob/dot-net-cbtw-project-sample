import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Link } from "react-router-dom";
import { StatusBadge, PriorityBadge } from "../components/Badges";
import { EmptyState, ErrorPanel, LoadingState } from "../components/Feedback";
import { PageHeader } from "../components/PageHeader";
import { useDebounce } from "../hooks/useDebounce";
import { api } from "../services/api";
import { categoryLabels, priorityLabels, statusLabels, type RequestFilters } from "../types";
import { formatDateTime } from "../utils/format";

export function RequestsPage() {
  const [search, setSearch] = useState("");
  const [filters, setFilters] = useState<RequestFilters>({
    page: 1,
    pageSize: 10,
    status: "",
    priority: "",
    category: "",
    sortBy: "updatedAt",
    sortDirection: "desc"
  });
  const debouncedSearch = useDebounce(search);
  const activeFilters = { ...filters, search: debouncedSearch };
  const requestsQuery = useQuery({
    queryKey: ["requests", activeFilters],
    queryFn: () => api.getRequests(activeFilters),
    placeholderData: (previous) => previous
  });

  const updateFilter = (key: keyof RequestFilters, value: string | number) =>
    setFilters((current) => ({ ...current, [key]: value, page: key === "page" ? Number(value) : 1 }));

  const clearFilters = () => {
    setSearch("");
    setFilters({ page: 1, pageSize: 10, status: "", priority: "", category: "", sortBy: "updatedAt", sortDirection: "desc" });
  };

  const data = requestsQuery.data;
  const hasFilters = Boolean(search || filters.status || filters.priority || filters.category);

  return (
    <div className="page requests-page">
      <PageHeader
        eyebrow="GESTIÓN CENTRALIZADA"
        title="Solicitudes"
        description="Consulta, filtra y acompaña cada caso desde un solo lugar."
        actions={<Link to="/requests/new" className="button button-primary"><span>＋</span> Nueva solicitud</Link>}
      />

      <section className="panel filters-panel">
        <div className="search-field">
          <span>⌕</span>
          <input value={search} onChange={(event) => { setSearch(event.target.value); setFilters((current) => ({ ...current, page: 1 })); }} placeholder="Buscar por título, descripción o código…" aria-label="Buscar solicitudes" />
          {search && <button onClick={() => setSearch("")} aria-label="Limpiar búsqueda">×</button>}
        </div>
        <div className="filter-selects">
          <label><span>Estado</span><select value={filters.status} onChange={(event) => updateFilter("status", event.target.value)}><option value="">Todos</option>{Object.entries(statusLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
          <label><span>Prioridad</span><select value={filters.priority} onChange={(event) => updateFilter("priority", event.target.value)}><option value="">Todas</option>{Object.entries(priorityLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
          <label><span>Categoría</span><select value={filters.category} onChange={(event) => updateFilter("category", event.target.value)}><option value="">Todas</option>{Object.entries(categoryLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
          <label><span>Ordenar</span><select value={`${filters.sortBy}:${filters.sortDirection}`} onChange={(event) => { const [sortBy, sortDirection] = event.target.value.split(":"); setFilters((current) => ({ ...current, sortBy, sortDirection: sortDirection as "asc" | "desc", page: 1 })); }}><option value="updatedAt:desc">Actualización reciente</option><option value="createdAt:desc">Más nuevas</option><option value="createdAt:asc">Más antiguas</option><option value="priority:desc">Mayor prioridad</option><option value="title:asc">Título A–Z</option></select></label>
        </div>
        {hasFilters && <button className="clear-filters" onClick={clearFilters}>× Limpiar filtros</button>}
      </section>

      <section className="panel table-panel">
        <div className="table-summary">
          <div><strong>{data?.total ?? 0}</strong> solicitudes encontradas</div>
          {requestsQuery.isFetching && !requestsQuery.isLoading && <span className="sync-label"><i /> Actualizando</span>}
        </div>

        {requestsQuery.isLoading ? (
          <LoadingState label="Buscando solicitudes…" />
        ) : requestsQuery.isError ? (
          <ErrorPanel message={requestsQuery.error.message} onRetry={() => void requestsQuery.refetch()} />
        ) : !data?.items.length ? (
          <EmptyState
            icon="▤"
            title={hasFilters ? "No encontramos coincidencias" : "Todavía no hay solicitudes"}
            description={hasFilters ? "Prueba cambiando o limpiando los filtros." : "Crea la primera solicitud para poner el flujo en marcha."}
            action={hasFilters ? <button className="button button-secondary" onClick={clearFilters}>Limpiar filtros</button> : <Link className="button button-primary" to="/requests/new">Nueva solicitud</Link>}
          />
        ) : (
          <>
            <div className="table-scroll">
              <table className="requests-table">
                <thead><tr><th>Solicitud</th><th>Categoría</th><th>Prioridad</th><th>Estado</th><th>Responsable</th><th>Actualizada</th><th><span className="sr-only">Abrir</span></th></tr></thead>
                <tbody>
                  {data.items.map((request) => (
                    <tr key={request.id}>
                      <td><Link to={`/requests/${request.id}`} className="table-title"><strong>{request.title}</strong><span>SF-{String(request.id).padStart(4, "0")}</span></Link></td>
                      <td><span className="category-cell">{categoryLabels[request.category] ?? request.category}</span></td>
                      <td><PriorityBadge priority={request.priority} /></td>
                      <td><StatusBadge status={request.status} /></td>
                      <td>{request.assignedTo ? <span className="assignee"><i>{request.assignedTo[0].toUpperCase()}</i>{request.assignedTo}</span> : <span className="unassigned">Sin asignar</span>}</td>
                      <td><time>{formatDateTime(request.updatedAt)}</time></td>
                      <td><Link to={`/requests/${request.id}`} className="table-open" aria-label={`Abrir ${request.title}`}>›</Link></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="pagination">
              <span>Página {data.page} de {Math.max(data.totalPages, 1)}</span>
              <div>
                <button disabled={data.page <= 1} onClick={() => updateFilter("page", data.page - 1)}>← Anterior</button>
                {Array.from({ length: Math.min(data.totalPages, 5) }, (_, index) => {
                  const start = Math.max(1, Math.min(data.page - 2, data.totalPages - 4));
                  const page = start + index;
                  return page <= data.totalPages ? <button key={page} className={page === data.page ? "active" : ""} onClick={() => updateFilter("page", page)}>{page}</button> : null;
                })}
                <button disabled={data.page >= data.totalPages} onClick={() => updateFilter("page", data.page + 1)}>Siguiente →</button>
              </div>
            </div>
          </>
        )}
      </section>
    </div>
  );
}
