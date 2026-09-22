# ADR 0005: Use React Flow for topology

## Context
The primary interaction is selecting components and inspecting a dependency graph.

## Decision
Render topology with `@xyflow/react`, using text health labels and dependency provenance on edges.

## Consequences
Graph interaction is available without a custom canvas renderer. Layout remains intentionally simple for the MVP.
