"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { DiscoveryPreview, getJson, postJson, Project, ProjectMember, RepositoryConnection, RepositoryScan } from "@/lib/api";
import { ProjectNav } from "@/components/ProjectNav";

type Review = { connectionId: string; preview: DiscoveryPreview };

export default function IntegrationsPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [project, setProject] = useState<Project | null>(null);
  const [connections, setConnections] = useState<RepositoryConnection[]>([]);
  const [scans, setScans] = useState<Record<string, RepositoryScan[]>>({});
  const [canManage, setCanManage] = useState(false);
  const [review, setReview] = useState<Review | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const load = useCallback(async () => {
    try {
      const [p, repositories, members, me] = await Promise.all([
        getJson<Project>(`/api/projects/${projectId}`),
        getJson<RepositoryConnection[]>(`/api/projects/${projectId}/repository-connections`),
        getJson<ProjectMember[]>(`/api/projects/${projectId}/members`), getJson<{ id: string }>("/api/me")
      ]);
      const histories = await Promise.all(repositories.map(async connection => [connection.id,
        await getJson<RepositoryScan[]>(`/api/projects/${projectId}/repository-connections/${connection.id}/scans?limit=25`)] as const));
      setProject(p); setConnections(repositories); setScans(Object.fromEntries(histories));
      setCanManage(members.some(member => member.userId === me.id && member.role === "Owner"));
      setError("");
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to load integrations."); }
  }, [projectId]);
  useEffect(() => { void load(); }, [load]);
  useEffect(() => {
    if (!Object.values(scans).some(history => history.some(scan => scan.status === "Pending" || scan.status === "Running"))) return;
    const timer = setInterval(() => void load(), 2000);
    return () => clearInterval(timer);
  }, [scans, load]);

  async function connectDemo() {
    setBusy(true); setError("");
    try {
      await postJson(`/api/projects/${projectId}/repository-connections`,
        { provider: "LocalDevelopment", name: "Bundled Commerce System", repositoryUrl: "demo://commerce-system", defaultBranch: "bundled" });
      setMessage("Repository connected. Start a scan to review its architecture."); await load();
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to connect repository."); }
    finally { setBusy(false); }
  }
  async function action(connectionId: string, operation: "scans" | "disconnect" | "reconnect") {
    setBusy(true); setError("");
    try {
      await postJson(`/api/projects/${projectId}/repository-connections/${connectionId}/${operation}`);
      setMessage(operation === "scans" ? "Scan queued. Review its preview when it completes." :
        operation === "disconnect" ? "Repository disconnected. Imported history and topology remain." : "Repository ready for a new scan.");
      await load();
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Repository action failed."); }
    finally { setBusy(false); }
  }
  async function openPreview(connectionId: string, scanId: string) {
    try {
      setReview({ connectionId, preview: await getJson<DiscoveryPreview>(
        `/api/projects/${projectId}/repository-connections/${connectionId}/scans/${scanId}/preview`) });
      setError("");
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to load scan preview."); }
  }
  async function applyReview() {
    if (!review) return;
    setBusy(true); setError("");
    try {
      await postJson(`/api/projects/${projectId}/repository-connections/${review.connectionId}/scans/${review.preview.id}/apply`);
      setMessage("Discovery applied. Open topology to inspect the evidence-backed graph.");
      setReview(null); await load();
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to apply scan."); }
    finally { setBusy(false); }
  }

  return <main className="page">
    <Link className="back-link" href={`/projects/${projectId}`}>← Project overview</Link><div className="eyebrow">EXTERNAL SOURCES</div>
    <h1>{project?.name ?? "Integrations"}</h1><p className="lede">Read-only connections keep the operational model tied to repository evidence.</p>
    <ProjectNav projectId={projectId} active="Integrations" />
    {error && <p className="error" role="alert">{error}</p>}{message && <p className="muted" role="status">{message}</p>}
    {connections.length === 0 && <section className="panel empty-state"><h2>No repository connected</h2>
      <p className="muted">Connect the bundled Commerce System to scan Docker Compose configuration. The project remains usable without integrations.</p>
      {canManage && <button className="button primary" disabled={busy} onClick={() => void connectDemo()}>Connect bundled demo</button>}
    </section>}
    {connections.map(connection => <section className="panel" key={connection.id}><div className="section-heading"><h2>{connection.name}</h2><span className="version">{connection.status}</span></div>
      <p className="muted">{connection.provider} · {connection.repositoryUrl} · branch {connection.defaultBranch}</p>
      <p className="muted">Last successful scan: {connection.lastSuccessfulSyncAt ? new Date(connection.lastSuccessfulSyncAt).toLocaleString() : "Never"}
        {connection.lastSyncError ? ` · ${connection.lastSyncError}` : ""}</p>
      {canManage && <div className="inline-form"><button className="button primary" disabled={busy || connection.status === "Disconnected"} onClick={() => void action(connection.id, "scans")}>Scan now</button>
        <button className="button" disabled={busy} onClick={() => void action(connection.id, connection.status === "Disconnected" ? "reconnect" : "disconnect")}>{connection.status === "Disconnected" ? "Reconnect" : "Disconnect"}</button></div>}
      <h3>Scan history</h3>{(scans[connection.id] ?? []).length === 0 && <p className="muted">No scans yet.</p>}
      {(scans[connection.id] ?? []).map(scan => <div className="list-row" key={scan.id}><span className="version">{scan.status}</span><div><strong>{new Date(scan.startedAt).toLocaleString()}</strong>
        <small>{scan.filesScanned} files · {scan.componentsDiscovered} components · {scan.dependenciesDiscovered} dependencies · {scan.warningsCount} warnings{scan.appliedAt ? " · Applied" : ""}</small>
        {scan.errorMessage && <small>{scan.errorMessage}</small>}</div>
        {scan.status === "Succeeded" || scan.status === "PartiallySucceeded" ? <button onClick={() => void openPreview(connection.id, scan.id)}>Review</button> : null}</div>)}
    </section>)}
    {review && <section className="panel"><div className="section-heading"><h2>Discovery preview</h2><button onClick={() => setReview(null)}>Close</button></div>
      <div className="drift-summary"><span>{review.preview.newComponents.length} new</span><span>{review.preview.changedComponents.length} changed</span><span>{review.preview.missingComponents.length} missing</span><span>{review.preview.conflicts.length} conflicts</span></div>
      {review.preview.components.map(component => <div className="list-row" key={`${component.sourcePath}:${component.externalKey}`}><span className="version">{component.type}</span><div><strong>{component.name}</strong><small>{component.sourcePath} · {component.sourceLocation}</small></div></div>)}
      <p className="muted">{review.preview.dependencies.length} dependency candidates. Applying a scan preserves manual name and type overrides.</p>
      {review.preview.warnings.map((warning, index) => <p className="error" key={index}>{warning.sourcePath}: {warning.message}</p>)}
      {review.preview.conflicts.map(component => <p className="error" key={component.externalKey}>Name conflict: {component.name}. It will be skipped.</p>)}
      {!review.preview.appliedAt && canManage && <button className="button primary" disabled={busy} onClick={() => void applyReview()}>{busy ? "Applying…" : "Apply scan"}</button>}
      {review.preview.appliedAt && <p className="muted">Applied {new Date(review.preview.appliedAt).toLocaleString()}.</p>}
    </section>}
  </main>;
}
