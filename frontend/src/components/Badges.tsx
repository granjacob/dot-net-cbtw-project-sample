import { priorityLabels, statusLabels, type RequestPriority, type RequestStatus } from "../types";

export function StatusBadge({ status }: { status: RequestStatus }) {
  return <span className={`badge status-${status.toLowerCase()}`}>{statusLabels[status] ?? status}</span>;
}

export function PriorityBadge({ priority }: { priority: RequestPriority }) {
  return (
    <span className={`priority priority-${priority.toLowerCase()}`}>
      <span className="priority-dot" />
      {priorityLabels[priority] ?? priority}
    </span>
  );
}
