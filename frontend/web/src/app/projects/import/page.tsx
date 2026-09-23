"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import { DiscoveryPreview, getJson, ImportStart, postJson, RepositoryScan } from "@/lib/api";

export default function ImportProjectPage() {
  const router = useRouter();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [imported, setImported] = useState<ImportStart | null>(null);
  const [scan, setScan] = useState<RepositoryScan | null>(null);
  const [preview, setPreview] = useState<DiscoveryPreview | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!imported) return;
    let active = true;
    let timeout: ReturnType<typeof setTimeout>;
    const base = `/api/projects/${imported.projectId}/repository-connections/${imported.connectionId}/scans/${imported.scanId}`;
    async function poll() {
      try {
        const current = await getJson<RepositoryScan>(base);
        if (!active) return;
        setScan(current);
        if (current.status === "Succeeded" || current.status === "PartiallySucceeded") {
          setPreview(await getJson<DiscoveryPreview>(`${base}/preview`));
          return;
        }
        if (current.status === "Failed") return;
      } catch (cause) { if (active) setError(cause instanceof Error ? cause.message : "Unable to read scan status."); }
      if (active) timeout = setTimeout(() => void poll(), 1500);
    }
    void poll();
    return () => { active = false; clearTimeout(timeout); };
  }, [imported]);

  async function begin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError("");
    try {
      const result = await postJson<ImportStart>("/api/projects/import-demo",
        { name, description, slug: null, visibility: "Private" });
      setImported(result);
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to start import."); }
    finally { setBusy(false); }
  }
  async function apply() {
    if (!imported) return;
    setBusy(true); setError("");
    try {
      await postJson(`/api/projects/${imported.projectId}/repository-connections/${imported.connectionId}/scans/${imported.scanId}/apply`);
      router.push(`/projects/${imported.projectId}/topology?environmentId=${imported.environmentId}`);
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to apply discovery."); }
    finally { setBusy(false); }
  }

  return <main className="page form-page">
    <Link href="/projects" className="back-link">← Projects</Link><div className="eyebrow">REPOSITORY DISCOVERY</div>
    <h1>Import an existing project</h1>
    <p className="lede">Scan repository configuration, review the discovered architecture, then apply it to the project.</p>
    {!imported && <form className="panel form-panel" onSubmit={begin}>
      <h2>1 · Choose source</h2><label>Repository source<select defaultValue="demo"><option value="demo">Bundled Commerce System demo</option></select></label>
      <p className="muted">This first V3 source reads the bundled example only. It does not execute repository files. Azure DevOps and remote Git connections follow in later integration slices.</p>
      <h2>2 · Project details</h2><label>Project name<input required minLength={2} maxLength={120} value={name} onChange={event => setName(event.target.value)} placeholder="e.g. Imported Commerce System" /></label>
      <label>Description<textarea maxLength={2000} value={description} onChange={event => setDescription(event.target.value)} /></label>
      <p className="muted">Starting the scan creates a private project with an empty Production environment. Topology appears only after you review and apply the preview.</p>
      <div className="form-actions"><Link className="button" href="/projects">Cancel</Link><button className="button primary" disabled={busy}>{busy ? "Starting…" : "Create and scan"}</button></div>
    </form>}
    {imported && <section className="panel"><h2>3 · Repository scan</h2><p><strong>{scan?.status ?? "Queued"}</strong> · Bundled Commerce System</p>
      <p className="muted">{scan?.status === "Running" ? "Scanning safe configuration files and parsing Docker Compose." :
        scan?.status === "Failed" ? scan.errorMessage : scan?.completedAt ?
          `${scan.filesScanned} files scanned · ${scan.componentsDiscovered} components · ${scan.dependenciesDiscovered} dependencies · ${scan.warningsCount} warnings` : "Waiting for the background worker."}</p>
      <Link href={`/projects/${imported.projectId}/settings/integrations`}>View scan history →</Link></section>}
    {preview && <section className="panel"><h2>4 · Review discovered architecture</h2>
      <div className="drift-summary"><span>{preview.newComponents.length} new components</span><span>{preview.changedComponents.length} changed</span><span>{preview.missingComponents.length} missing</span><span>{preview.conflicts.length} conflicts</span></div>
      <h3>Components</h3>{preview.components.map(component => <div className="list-row" key={`${component.sourcePath}:${component.externalKey}`}><span className="version">{component.type}</span><div><strong>{component.name}</strong><small>{component.sourcePath} · {component.sourceLocation}</small></div></div>)}
      <h3>Dependencies</h3>{preview.dependencies.map((dependency, index) => <div className="list-row" key={`${dependency.sourcePath}:${index}`}><span className="version">{dependency.type}</span><div><strong>{dependency.sourceExternalKey} → {dependency.targetExternalKey}</strong><small>{dependency.sourcePath} · {dependency.sourceLocation}</small></div></div>)}
      {preview.missingComponents.map(component => <p className="muted" key={component.id}>Missing from this scan: {component.name}</p>)}
      {preview.conflicts.map(component => <p className="error" key={component.externalKey}>Name conflict: {component.name}. This candidate will be skipped on apply.</p>)}
      {preview.warnings.length > 0 && <><h3>Parser warnings</h3>{preview.warnings.map((warning, index) => <p className="error" key={index}>{warning.sourcePath}: {warning.message}</p>)}</>}
      <div className="form-actions"><Link className="button" href={`/projects/${imported!.projectId}`}>Review later</Link><button className="button primary" disabled={busy} onClick={() => void apply()}>{busy ? "Applying…" : "Apply scan"}</button></div>
    </section>}
    {error && <p className="error" role="alert">{error}</p>}
  </main>;
}
