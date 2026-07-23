import { useEffect, useState, useSyncExternalStore } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, NavLink, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { api } from "../services/api";
import { eventBus, realtimeEventNames } from "../services/eventBus";
import { realtimeService, type ConnectionState } from "../services/signalR";
import { requestStore } from "../stores/requestStore";
import { getInitials } from "../utils/format";

const navItems = [
  { to: "/", label: "Dashboard", icon: "▦", end: true },
  { to: "/requests", label: "Solicitudes", icon: "▤" },
  { to: "/notifications", label: "Notificaciones", icon: "◉" }
];

const pageTitles: Record<string, string> = {
  "/": "Resumen operativo",
  "/requests": "Solicitudes",
  "/requests/new": "Nueva solicitud",
  "/notifications": "Notificaciones"
};

export function AppLayout() {
  const { session, user, logout } = useAuth();
  const queryClient = useQueryClient();
  const location = useLocation();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [connectionState, setConnectionState] = useState<ConnectionState>("disconnected");
  const realtimeSnapshot = useSyncExternalStore(requestStore.subscribe, requestStore.getSnapshot);

  useEffect(() => realtimeService.subscribeState(setConnectionState), []);

  useEffect(() => {
    if (session?.token) void realtimeService.connect(session.token);
    return () => {
      void realtimeService.disconnect();
    };
  }, [session?.token]);

  useEffect(() => {
    const unsubscribers = realtimeEventNames.map((eventName) =>
      eventBus.subscribe(eventName, () => {
        void queryClient.invalidateQueries({ queryKey: ["requests"] });
        void queryClient.invalidateQueries({ queryKey: ["request"] });
        void queryClient.invalidateQueries({ queryKey: ["history"] });
        void queryClient.invalidateQueries({ queryKey: ["notifications"] });
        void queryClient.invalidateQueries({ queryKey: ["unread-count"] });
      })
    );
    return () => unsubscribers.forEach((unsubscribe) => unsubscribe());
  }, [queryClient]);

  useEffect(() => {
    if (connectionState !== "connected") return;
    void queryClient.invalidateQueries({ queryKey: ["requests"] });
    void queryClient.invalidateQueries({ queryKey: ["request"] });
    void queryClient.invalidateQueries({ queryKey: ["history"] });
    void queryClient.invalidateQueries({ queryKey: ["notifications"] });
    void queryClient.invalidateQueries({ queryKey: ["unread-count"] });
  }, [connectionState, queryClient]);

  useEffect(() => setSidebarOpen(false), [location.pathname]);

  const unreadQuery = useQuery({
    queryKey: ["unread-count"],
    queryFn: api.getUnreadCount,
    refetchInterval: connectionState === "connected" ? false : 30_000,
    retry: 1
  });

  const rawCount = unreadQuery.data;
  const unreadCount = typeof rawCount === "number" ? rawCount : rawCount?.count ?? 0;
  const title = location.pathname.startsWith("/requests/")
    ? location.pathname === "/requests/new"
      ? pageTitles["/requests/new"]
      : "Detalle de solicitud"
    : pageTitles[location.pathname] ?? "ServiceFlow";

  return (
    <div className="app-shell">
      {sidebarOpen && <button className="sidebar-overlay" aria-label="Cerrar menú" onClick={() => setSidebarOpen(false)} />}
      <aside className={`sidebar ${sidebarOpen ? "sidebar-open" : ""}`}>
        <Link to="/" className="brand" aria-label="ServiceFlow - Inicio">
          <span className="brand-mark"><i /><i /><i /></span>
          <span><strong>Service</strong>Flow</span>
        </Link>

        <div className="workspace-label">ESPACIO DE TRABAJO</div>
        <nav className="main-nav" aria-label="Navegación principal">
          {navItems.map((item) => (
            <NavLink key={item.to} to={item.to} end={item.end}>
              <span className="nav-icon">{item.icon}</span>
              <span>{item.label}</span>
              {item.to === "/notifications" && unreadCount > 0 && (
                <span className="nav-count">{unreadCount > 99 ? "99+" : unreadCount}</span>
              )}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-card">
          <div className="sidebar-card-icon">↗</div>
          <strong>¿Necesitas ayuda?</strong>
          <p>Crea una solicitud y nuestro equipo te acompañará.</p>
          <Link to="/requests/new">Nueva solicitud</Link>
        </div>

        <div className="sidebar-footer">
          <div className="avatar">{getInitials(user?.name ?? "SF")}</div>
          <div className="sidebar-user">
            <strong>{user?.name}</strong>
            <span>{user?.role}</span>
          </div>
          <button className="icon-button" onClick={logout} aria-label="Cerrar sesión" title="Cerrar sesión">↪</button>
        </div>
      </aside>

      <div className="content-shell">
        <header className="topbar">
          <button className="mobile-menu" onClick={() => setSidebarOpen(true)} aria-label="Abrir menú">☰</button>
          <div>
            <span className="topbar-kicker">SERVICEFLOW</span>
            <strong>{title}</strong>
          </div>
          <div className="topbar-actions">
            <span className={`connection-pill connection-${connectionState}`} title="Estado de actualización en tiempo real">
              <i /> {connectionState === "connected" ? "En tiempo real" : connectionState === "reconnecting" ? "Reconectando" : "Conectando"}
            </span>
            <Link className="notification-button" to="/notifications" aria-label={`${unreadCount} notificaciones sin leer`}>
              ♢
              {unreadCount > 0 && <span>{unreadCount > 99 ? "99+" : unreadCount}</span>}
            </Link>
            <div className="topbar-avatar avatar">{getInitials(user?.name ?? "SF")}</div>
          </div>
        </header>

        <main className="main-content">
          <Outlet />
        </main>
      </div>

      {realtimeSnapshot.version > 0 && (
        <div className="live-indicator" key={realtimeSnapshot.version} role="status">
          <span>✓</span>
          <div><strong>Actualización recibida</strong><small>La información se sincronizó automáticamente.</small></div>
        </div>
      )}
    </div>
  );
}
