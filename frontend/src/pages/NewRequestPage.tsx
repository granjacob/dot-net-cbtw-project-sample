import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate } from "react-router-dom";
import { z } from "zod";
import { PageHeader } from "../components/PageHeader";
import { api } from "../services/api";
import { categoryLabels, priorityLabels, type RequestCategory, type RequestPriority } from "../types";
import { getApiMessage } from "../utils/format";

export const requestSchema = z.object({
  title: z.string().trim().min(5, "Describe el asunto en al menos 5 caracteres.").max(160, "Usa máximo 160 caracteres."),
  description: z.string().trim().min(20, "Incluye al menos 20 caracteres para que podamos ayudarte.").max(4_000, "Usa máximo 4.000 caracteres."),
  category: z.enum(["TechnicalSupport", "Maintenance", "SystemAccess", "Purchasing", "OperationalIncident"]),
  priority: z.enum(["Low", "Medium", "High", "Critical"])
});

type RequestForm = z.infer<typeof requestSchema>;

export function NewRequestPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [serverError, setServerError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors }
  } = useForm<RequestForm>({
    resolver: zodResolver(requestSchema),
    defaultValues: { title: "", description: "", category: "TechnicalSupport", priority: "Medium" }
  });
  const description = watch("description");
  const priority = watch("priority");

  const mutation = useMutation({
    mutationFn: api.createRequest,
    onSuccess: async (created) => {
      await queryClient.invalidateQueries({ queryKey: ["requests"] });
      navigate(`/requests/${created.id}`, { replace: true, state: { created: true } });
    },
    onError: (error) => setServerError(getApiMessage(error))
  });

  const onSubmit = handleSubmit((values) => {
    setServerError(null);
    mutation.mutate(values);
  });

  return (
    <div className="page form-page">
      <PageHeader eyebrow="NUEVO CASO" title="Crear solicitud" description="Cuéntanos qué necesitas. Una descripción clara nos ayuda a responder mejor." />
      <div className="form-layout">
        <form className="panel request-form" onSubmit={onSubmit} noValidate>
          {serverError && <div className="alert alert-error compact"><span>!</span><p>{serverError}</p></div>}
          <div className="form-section-heading"><span>01</span><div><h2>Información principal</h2><p>Identifica el motivo de tu solicitud.</p></div></div>
          <label className="field">
            <span>Título <b>*</b></span>
            <input {...register("title")} placeholder="Ej. No puedo acceder al portal de proveedores" autoFocus />
            {errors.title && <small className="field-error">{errors.title.message}</small>}
          </label>
          <div className="form-grid-2">
            <label className="field"><span>Categoría <b>*</b></span><select {...register("category")}>{Object.entries(categoryLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>{errors.category && <small className="field-error">{errors.category.message}</small>}</label>
            <label className="field"><span>Prioridad <b>*</b></span><select {...register("priority")}>{Object.entries(priorityLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>{errors.priority && <small className="field-error">{errors.priority.message}</small>}</label>
          </div>
          <label className="field">
            <span>Descripción <b>*</b></span>
            <textarea {...register("description")} rows={8} placeholder="Explica el contexto, qué intentaste y cuál es el resultado esperado…" />
            <div className="field-meta">{errors.description ? <small className="field-error">{errors.description.message}</small> : <small>Evita incluir contraseñas o información confidencial.</small>}<small>{description.length}/4000</small></div>
          </label>

          <div className="form-actions">
            <Link to="/requests" className="button button-ghost">Cancelar</Link>
            <button className="button button-primary button-large" type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? <><span className="spinner small" /> Creando…</> : <>Crear solicitud <span>→</span></>}
            </button>
          </div>
        </form>

        <aside className="form-aside">
          <div className={`priority-guide guide-${priority.toLowerCase()}`}>
            <span className="eyebrow">PRIORIDAD SELECCIONADA</span>
            <h3>{priorityLabels[priority as RequestPriority]}</h3>
            <p>{priority === "Critical" ? "Impacto total o riesgo operativo. Atención inmediata." : priority === "High" ? "Impacto importante, sin alternativa práctica disponible." : priority === "Medium" ? "Afecta el trabajo, pero existe una alternativa temporal." : "Consulta o mejora que no bloquea la operación."}</p>
          </div>
          <div className="help-panel panel">
            <h3>Una buena solicitud incluye</h3>
            <ul><li><span>✓</span> Qué estabas intentando hacer</li><li><span>✓</span> Qué ocurrió exactamente</li><li><span>✓</span> Desde cuándo sucede</li><li><span>✓</span> A cuántas personas afecta</li></ul>
          </div>
          <div className="category-note">Categorías disponibles: {Object.keys(categoryLabels).length} · Seleccionada: <strong>{categoryLabels[watch("category") as RequestCategory]}</strong></div>
        </aside>
      </div>
    </div>
  );
}
