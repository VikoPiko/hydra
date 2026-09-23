"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { ProjectNav } from "@/components/ProjectNav";
import { Environment, EnvironmentComparison, getJson, Project } from "@/lib/api";

export default function ComparisonPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [project, setProject] = useState<Project | null>(null);
  const [environments, setEnvironments] = useState<Environment[]>([]);
  const [left, setLeft] = useState("");
  const [right, setRight] = useState("");
  const [comparison, setComparison] = useState<EnvironmentComparison | null>(null);
  const [differencesOnly, setDifferencesOnly] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    Promise.all([getJson<Project>(`/api/projects/${projectId}`), getJson<Environment[]>(`/api/projects/${projectId}/environments`)])
      .then(([p, envs]) => { setProject(p); setEnvironments(envs); setLeft(envs[0]?.id ?? ""); setRight(envs[1]?.id ?? ""); })
      .catch(cause => setError(cause instanceof Error ? cause.message : "Unable to load environments."));
  }, [projectId]);
  useEffect(() => {
    if (!left || !right || left === right) { setComparison(null); return; }
    let current = true;
    getJson<EnvironmentComparison>(`/api/projects/${projectId}/environment-comparison?leftEnvironmentId=${left}&rightEnvironmentId=${right}`)
      .then(data => { if (current) { setComparison(data); setError(""); } })
      .catch(cause => { if (current) setError(cause instanceof Error ? cause.message : "Unable to compare environments."); });
    return () => { current = false; };
  }, [projectId, left, right]);

  const rows = comparison?.components.filter(component => !differencesOnly || component.different) ?? [];
  return <main className="page">
    <Link className="back-link" href={`/projects/${projectId}`}>← Project overview</Link><div className="eyebrow">ENVIRONMENT COMPARISON</div>
    <h1>{project?.name ?? "Compare environments"}</h1><p className="lede">Compare recorded versions and health across two environments.</p>
    <ProjectNav projectId={projectId} active="Compare" />
    {environments.length < 2 && project && <div className="panel empty-state"><h2>Two environments are needed</h2><p className="muted">Add another environment from the project overview, then return here.</p><Link className="button primary" href={`/projects/${projectId}`}>Project overview</Link></div>}
    {environments.length >= 2 && <div className="toolbar"><label>Left <select value={left} onChange={event => setLeft(event.target.value)}>{environments.map(environment => <option value={environment.id} key={environment.id}>{environment.name}</option>)}</select></label>
      <label>Right <select value={right} onChange={event => setRight(event.target.value)}>{environments.map(environment => <option value={environment.id} key={environment.id}>{environment.name}</option>)}</select></label>
      <label><input type="checkbox" checked={differencesOnly} onChange={event => setDifferencesOnly(event.target.checked)} /> Differences only</label></div>}
    {left === right && left && <p className="muted">Choose two different environments.</p>}
    {error && <p className="error" role="alert">{error}</p>}
    {comparison && <section className="panel table-panel"><p className="muted">Presence means a deployment or runtime record exists. No record does not prove a component is absent. {comparison.declaredDependencyCount} declared dependencies are project-wide and therefore shared by both environments.</p>
      <table><thead><tr><th>Component</th><th>{comparison.leftEnvironmentName}</th><th>{comparison.rightEnvironmentName}</th><th>Comparison</th></tr></thead><tbody>{rows.map(component => <tr key={component.componentId}><td>{component.name}</td><td>{component.leftHasData ? "Recorded" : "No record"} · {component.leftVersion ?? "No version"} · {component.leftHealth ?? "Unknown health"}</td><td>{component.rightHasData ? "Recorded" : "No record"} · {component.rightVersion ?? "No version"} · {component.rightHealth ?? "Unknown health"}</td><td>{component.different ? "Different" : "Same recorded state"}</td></tr>)}</tbody></table>{rows.length === 0 && <p className="muted">{differencesOnly ? "No recorded differences." : "No components yet."}</p>}</section>}
  </main>;
}
