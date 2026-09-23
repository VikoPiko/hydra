"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { postJson, Project } from "@/lib/api";

export default function NewProjectPage() {
  const router = useRouter();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [slug, setSlug] = useState("");
  const [visibility, setVisibility] = useState<"Private" | "Internal">("Private");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setBusy(true);
    try {
      const project = await postJson<Project>("/api/projects", { name, description, slug: slug || null, visibility });
      router.push(`/projects/${project.id}`);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Unable to create project.");
    } finally { setBusy(false); }
  }
  return <main className="page form-page">
    <Link href="/projects" className="back-link">← Projects</Link>
    <div className="eyebrow">NEW WORKSPACE</div>
    <h1>Create a project</h1>
    <p className="lede">Start with a name and description. You can add environments and components when you are ready.</p>
    <form className="panel form-panel" onSubmit={submit}>
      <label>Project name<input required minLength={2} maxLength={120} value={name} onChange={e => setName(e.target.value)} placeholder="e.g. Commerce Platform" /></label>
      <label>Slug<input value={slug} maxLength={80} pattern="[a-z0-9]+(-[a-z0-9]+)*" onChange={e => setSlug(e.target.value.toLowerCase())} placeholder="Generated from the name if blank" /><small>Lowercase letters, numbers, and hyphens. Used as a stable identifier.</small></label>
      <label>Description<textarea maxLength={2000} value={description} onChange={e => setDescription(e.target.value)} placeholder="What does this project contain?" /></label>
      <label>Visibility<select value={visibility} onChange={e => setVisibility(e.target.value as "Private" | "Internal")}><option value="Private">Private</option><option value="Internal">Internal</option></select></label>
      {error && <p className="error" role="alert">{error}</p>}
      <div className="form-actions"><Link className="button" href="/projects">Cancel</Link><button className="button primary" disabled={busy}>{busy ? "Creating…" : "Create project"}</button></div>
    </form>
  </main>;
}
