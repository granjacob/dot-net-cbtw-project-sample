import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PriorityBadge, StatusBadge } from "./Badges";

describe("badges de solicitud", () => {
  it("renderiza etiquetas de negocio en español", () => {
    render(<><StatusBadge status="InProgress" /><PriorityBadge priority="Critical" /></>);

    expect(screen.getByText("En progreso")).toBeInTheDocument();
    expect(screen.getByText("Crítica")).toBeInTheDocument();
  });
});
