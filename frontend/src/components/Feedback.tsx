export function LoadingState({ label = "Cargando información…" }: { label?: string }) {
  return (
    <div className="state-panel" role="status">
      <span className="spinner" />
      <p>{label}</p>
    </div>
  );
}

export function EmptyState({
  icon = "⌁",
  title,
  description,
  action
}: {
  icon?: string;
  title: string;
  description: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="state-panel empty-state">
      <span className="empty-icon">{icon}</span>
      <h3>{title}</h3>
      <p>{description}</p>
      {action}
    </div>
  );
}

export function ErrorPanel({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="alert alert-error" role="alert">
      <span>!</span>
      <div>
        <strong>No pudimos cargar esta información</strong>
        <p>{message}</p>
      </div>
      {onRetry && (
        <button className="button button-ghost button-small" onClick={onRetry}>
          Reintentar
        </button>
      )}
    </div>
  );
}
