import type { Edge, Node } from "@xyflow/react";
import type { Impact, Topology } from "./api";

export function mapTopology(topology: Topology, selectedId: string | null, impact: Impact | null, search: string, selectedEdgeId: string | null = null): { nodes: Node[]; edges: Edge[] } {
  const incoming = new Set(topology.edges.map(edge => edge.target));
  const roots = topology.nodes.filter(node => !incoming.has(node.id)).map(node => node.id);
  const depths = new Map<string, number>();
  const queue = roots.map(id => ({ id, depth: 0 }));
  while (queue.length) {
    const item = queue.shift()!;
    if (depths.has(item.id)) continue;
    depths.set(item.id, item.depth);
    for (const edge of topology.edges.filter(edge => edge.source === item.id)) queue.push({ id: edge.target, depth: item.depth + 1 });
  }
  for (const node of topology.nodes) if (!depths.has(node.id)) depths.set(node.id, 0);
  const levels = new Map<number, string[]>();
  for (const node of topology.nodes) {
    const depth = depths.get(node.id)!;
    levels.set(depth, [...(levels.get(depth) ?? []), node.id]);
  }
  const direct = new Set(impact?.directDependents.map(x => x.id) ?? []);
  const indirect = new Set(impact?.indirectDependents.map(x => x.id) ?? []);
  const query = search.trim().toLowerCase();
  const nodes: Node[] = topology.nodes.map(component => {
    const depth = depths.get(component.id)!;
    const row = levels.get(depth)!;
    const index = row.indexOf(component.id);
    const impactKind = direct.has(component.id) ? "direct" : indirect.has(component.id) ? "indirect" : "";
    const unrelated = !!impact && component.id !== impact.componentId && !impactKind;
    const filtered = !!query && !component.name.toLowerCase().includes(query);
    return { id: component.id, type: "component", position: { x: index * 250 + (5 - row.length) * 125, y: depth * 130 },
      data: { component, selected: component.id === selectedId, impactKind, dimmed: unrelated || filtered } };
  });
  const edges: Edge[] = topology.edges.map(edge => ({ id: edge.id, source: edge.source, target: edge.target,
    label: `${edge.type} · ${edge.sourceKind}`, type: "smoothstep", animated: false,
    style: { stroke: selectedEdgeId === edge.id || selectedId && (edge.source === selectedId || edge.target === selectedId) ? "var(--color-primary)" : "var(--color-border-strong)", strokeWidth: selectedEdgeId === edge.id ? 3 : 1.5 },
    labelStyle: { fill: "var(--color-text-secondary)", fontSize: 10 }, labelBgStyle: { fill: "var(--color-surface)", fillOpacity: 0.95 } }));
  return { nodes, edges };
}
