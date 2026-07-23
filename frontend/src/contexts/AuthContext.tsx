import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { api } from "../services/api";
import { clearSession, readSession, saveSession } from "../services/authStorage";
import { realtimeService } from "../services/signalR";
import { requestStore } from "../stores/requestStore";
import type { AuthSession, User } from "../types";

interface AuthContextValue {
  session: AuthSession | null;
  user: User | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(() => readSession());
  const queryClient = useQueryClient();

  const logout = useCallback(() => {
    clearSession();
    setSession(null);
    queryClient.clear();
    requestStore.reset();
    void realtimeService.disconnect();
  }, [queryClient]);

  useEffect(() => {
    const handleUnauthorized = () => logout();
    window.addEventListener("serviceflow:unauthorized", handleUnauthorized);
    return () => window.removeEventListener("serviceflow:unauthorized", handleUnauthorized);
  }, [logout]);

  const login = useCallback(async (email: string, password: string) => {
    const nextSession = await api.login(email, password);
    queryClient.clear();
    requestStore.reset();
    saveSession(nextSession);
    setSession(nextSession);
  }, [queryClient]);

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      user: session?.user ?? null,
      isAuthenticated: Boolean(session),
      login,
      logout
    }),
    [session, login, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth debe usarse dentro de AuthProvider");
  return context;
}
