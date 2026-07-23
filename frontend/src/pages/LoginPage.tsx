import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { z } from "zod";
import { useAuth } from "../contexts/AuthContext";
import { getApiMessage } from "../utils/format";

const loginSchema = z.object({
  email: z.email("Ingresa un correo válido."),
  password: z.string().min(8, "La contraseña debe tener al menos 8 caracteres.")
});

type LoginValues = z.infer<typeof loginSchema>;

const demoUsers = [
  { label: "Empleado", email: "employee@serviceflow.local", password: "Employee123!", color: "mint" },
  { label: "Agente", email: "agent@serviceflow.local", password: "Agent123!", color: "blue" },
  { label: "Administrador", email: "admin@serviceflow.local", password: "Admin123!", color: "violet" }
] as const;

export function LoginPage() {
  const { login, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [serverError, setServerError] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting }
  } = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "employee@serviceflow.local", password: "Employee123!" }
  });

  if (isAuthenticated) return <Navigate to="/" replace />;

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null);
    try {
      await login(values.email, values.password);
      const destination = (location.state as { from?: string } | null)?.from ?? "/";
      navigate(destination, { replace: true });
    } catch (error) {
      setServerError(getApiMessage(error));
    }
  });

  const selectDemo = (email: string, password: string) => {
    setValue("email", email, { shouldValidate: true });
    setValue("password", password, { shouldValidate: true });
    setServerError(null);
  };

  return (
    <main className="login-page">
      <section className="login-visual">
        <div className="login-grid" />
        <div className="login-visual-content">
          <div className="brand brand-light">
            <span className="brand-mark"><i /><i /><i /></span>
            <span><strong>Service</strong>Flow</span>
          </div>
          <div className="visual-copy">
            <span className="eyebrow eyebrow-light">OPERACIONES CONECTADAS</span>
            <h1>El trabajo fluye mejor cuando todos ven lo mismo.</h1>
            <p>Centraliza solicitudes, coordina responsables y sigue cada actualización en tiempo real.</p>
          </div>
          <div className="flow-preview">
            <div className="preview-top"><span>Actividad en vivo</span><i>● En línea</i></div>
            <div className="preview-event">
              <span className="preview-avatar">MS</span>
              <div><strong>María actualizó una solicitud</strong><small>SF-0142 · En progreso → Resuelta</small></div>
              <time>ahora</time>
            </div>
            <div className="preview-event muted">
              <span className="preview-avatar amber">JR</span>
              <div><strong>Juan agregó un comentario</strong><small>SF-0139 · Acceso a plataforma</small></div>
              <time>2 min</time>
            </div>
          </div>
        </div>
        <div className="login-visual-footer">SEGURO · TRAZABLE · EN TIEMPO REAL</div>
      </section>

      <section className="login-panel">
        <div className="login-card">
          <div className="mobile-brand brand">
            <span className="brand-mark"><i /><i /><i /></span>
            <span><strong>Service</strong>Flow</span>
          </div>
          <span className="eyebrow">BIENVENIDO DE NUEVO</span>
          <h2>Inicia sesión en tu espacio</h2>
          <p className="login-intro">Usa tus credenciales para continuar.</p>

          {serverError && <div className="alert alert-error compact" role="alert"><span>!</span><p>{serverError}</p></div>}

          <form onSubmit={onSubmit} className="form-stack" noValidate>
            <label className="field">
              <span>Correo electrónico</span>
              <div className="input-with-icon"><i>@</i><input type="email" autoComplete="email" placeholder="tu@empresa.com" {...register("email")} /></div>
              {errors.email && <small className="field-error">{errors.email.message}</small>}
            </label>
            <label className="field">
              <span>Contraseña</span>
              <div className="input-with-icon"><i>⌁</i><input type={showPassword ? "text" : "password"} autoComplete="current-password" {...register("password")} /><button type="button" onClick={() => setShowPassword((value) => !value)} aria-label={showPassword ? "Ocultar contraseña" : "Mostrar contraseña"}>{showPassword ? "Ocultar" : "Ver"}</button></div>
              {errors.password && <small className="field-error">{errors.password.message}</small>}
            </label>
            <button className="button button-primary button-large" type="submit" disabled={isSubmitting}>
              {isSubmitting ? <><span className="spinner small" /> Ingresando…</> : <>Ingresar <span>→</span></>}
            </button>
          </form>

          <div className="demo-divider"><span>CUENTAS DE DEMOSTRACIÓN</span></div>
          <div className="demo-users">
            {demoUsers.map((demo) => (
              <button key={demo.email} type="button" onClick={() => selectDemo(demo.email, demo.password)}>
                <span className={`demo-dot ${demo.color}`} />
                <span><strong>{demo.label}</strong><small>{demo.email}</small></span>
                <i>→</i>
              </button>
            ))}
          </div>
          <p className="login-security">⌾ Conexión protegida con autenticación JWT</p>
        </div>
      </section>
    </main>
  );
}
