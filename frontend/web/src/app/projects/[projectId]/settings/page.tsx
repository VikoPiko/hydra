"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import { ProjectNav } from "@/components/ProjectNav";
import { getJson, postJson, Project, ProjectMember, sendJson } from "@/lib/api";

export default function SettingsPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const router = useRouter();
  const [project, setProject] = useState<Project | null>(null);
  const [members, setMembers] = useState<ProjectMember[]>([]);
  const [canManage, setCanManage] = useState(false);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [slug, setSlug] = useState("");
  const [visibility, setVisibility] = useState<"Private" | "Internal">("Private");
  const [memberEmail, setMemberEmail] = useState("");
  const [memberRole, setMemberRole] = useState<"Contributor" | "Viewer">("Contributor");
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  useEffect(() => {
    Promise.all([getJson<Project>(`/api/projects/${projectId}`), getJson<ProjectMember[]>(`/api/projects/${projectId}/members`),
      getJson<{ id: string }>("/api/me")])
      .then(([p, people, me]) => { setProject(p); setMembers(people); setName(p.name); setDescription(p.description);
        setSlug(p.slug); setVisibility(p.visibility); setCanManage(people.some(member => member.userId === me.id && member.role === "Owner")); })
      .catch(cause => setError(cause instanceof Error ? cause.message : "Unable to load settings."));
  }, [projectId]);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    try {
      const updated = await sendJson<Project>("PUT", `/api/projects/${projectId}`, { name, description, slug, visibility });
      setProject(updated); setError(""); setMessage("Project settings saved.");
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to save settings."); }
  }
  async function addMember(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    try {
      await postJson(`/api/projects/${projectId}/members/by-email`, { email: memberEmail.trim(), role: memberRole });
      setMembers(await getJson<ProjectMember[]>(`/api/projects/${projectId}/members`));
      setMemberEmail(""); setError(""); setMessage("Member added.");
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to add member."); }
  }
  async function archive() {
    try {
      await postJson(`/api/projects/${projectId}/archive`);
      router.push("/projects");
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to archive project."); }
  }

  return <main className="page form-page">
    <Link className="back-link" href={`/projects/${projectId}`}>← Project overview</Link><div className="eyebrow">PROJECT ADMINISTRATION</div>
    <h1>{project?.name ?? "Settings"}</h1><p className="lede">Manage identity, membership, and the project record.</p>
    <ProjectNav projectId={projectId} active="Settings" />
    {error && <p className="error" role="alert">{error}</p>}{message && <p className="muted" role="status">{message}</p>}
    {project && <section className="panel"><h2>Project details</h2><form className="form-panel compact-form" onSubmit={save}>
      <label>Name<input required minLength={2} maxLength={120} disabled={!canManage} value={name} onChange={event => setName(event.target.value)} /></label>
      <label>Slug<input required minLength={2} maxLength={80} pattern="[a-z0-9]+(-[a-z0-9]+)*" disabled={!canManage} value={slug} onChange={event => setSlug(event.target.value.toLowerCase())} /><small>Lowercase letters, numbers, and hyphens.</small></label>
      <label>Description<textarea maxLength={2000} disabled={!canManage} value={description} onChange={event => setDescription(event.target.value)} /></label>
      <label>Visibility<select disabled={!canManage} value={visibility} onChange={event => setVisibility(event.target.value as "Private" | "Internal")}><option>Private</option><option>Internal</option></select></label>
      {canManage && <button className="button primary">Save settings</button>}
    </form></section>}
    <section className="panel"><h2>Members</h2>{members.map(member => <div className="list-row" key={member.userId}><span className="version">{member.role}</span><div><strong>{member.displayName}</strong><small>{member.email ?? "No email provided"}</small></div></div>)}
      {canManage && <><p className="muted">A person must sign in once before you can add their email.</p><form className="inline-form" onSubmit={addMember}><label>Email<input required type="email" maxLength={320} value={memberEmail} onChange={event => setMemberEmail(event.target.value)} placeholder="teammate@example.com" /></label><label>Role<select value={memberRole} onChange={event => setMemberRole(event.target.value as "Contributor" | "Viewer")}><option>Contributor</option><option>Viewer</option></select></label><button className="button primary">Add member</button></form></>}
    </section>
    {canManage && !project?.archivedAt && <section className="panel"><h2>Archive project</h2><p className="muted">Archiving hides this project from the active list. Its environments, work, deployments, and events stay in PostgreSQL.</p><button onClick={() => void archive()}>Archive project</button></section>}
  </main>;
}
