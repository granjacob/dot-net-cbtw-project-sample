import { describe, expect, it } from "vitest";
import { requestSchema } from "./NewRequestPage";

describe("validación de nueva solicitud", () => {
  it("rechaza títulos y descripciones sin suficiente contexto", () => {
    const result = requestSchema.safeParse({
      title: "Error",
      description: "No sirve",
      category: "TechnicalSupport",
      priority: "Medium"
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.map((issue) => issue.path[0])).toContain("description");
    }
  });

  it("acepta una solicitud completa", () => {
    expect(
      requestSchema.safeParse({
        title: "No puedo acceder al portal",
        description: "Desde esta mañana el portal muestra un error de autorización al iniciar sesión.",
        category: "SystemAccess",
        priority: "High"
      }).success
    ).toBe(true);
  });
});
