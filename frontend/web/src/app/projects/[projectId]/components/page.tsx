"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import { ProjectNav } from "@/components/ProjectNav";
import { Component, Dependency, getJson, postJson, Project, ProjectMember } from "@/lib/api";

const componentTypes = ["Frontend", "Service", "Database", "MessageBroker", "Cache", "ExternalService", "Gateway", "Worker", "Other"];
const dependencyTypes = ["Http", "Database", "Messaging", "Cache", "Infrastructure", "External", "Other"];

export default function ComponentsPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [project, setProject] = useState<Project | null>(null);
  const [components, setComponents] = useState<Component[]>([]);
  const [dependencies, setDependencies] = useState<Dependency[]>([]);
  const [canEdit, setCanEdit] = useState(false);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [type, setType] = useState("Service");
  const [owner, setOwner] = useState("");
  const [repositoryUrl, setRepositoryUrl] = useState("");
  const [sourceId, setSourceId] = useState("");
  const [targetId, setTargetId] = useState("");
  const [dependencyType, setDependencyType] = useState("Http");
  const [error, setError] = useState("");

  useEffect(() => {
    Promise.all([getJson<Project>(`/api/projects/${projectId}`), getJson<Component[]>(`/api/projects/${projectId}/components`),
      getJson<Dependency[]>(`/api/projects/${projectId}/dependencies`), getJson<ProjectMember[]>(`/api/projects/${projectId}/members`),
      getJson<{ id: string }>("/api/me")])
      .then(([p, nodes, edges, members, me]) => { setProject(p); setComponents(nodes); setDependencies(edges);
        setCanEdit(members.some(member => member.userId === me.id && member.role !== "Viewer")); })
      .catch(cause => setError(cause instanceof Error ? cause.message : "Unable to load components."));
  }, [projectId]);
  const componentName = (id: string) => components.find(component => component.id === id)?.name ?? "Unknown component";

  async function createComponent(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    try {
      const component = await postJson<Component>(`/api/projects/${projectId}/components`,
        { name, description, type, owner: owner || null, repositoryUrl: repositoryUrl || null });
      setComponents(current => [...current, component].sort((a, b) => a.name.localeCompare(b.name)));
      setName(""); setDescription(""); setOwner(""); setRepositoryUrl(""); setError("");
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to create component."); }
  }
  async function createDependency(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    try {
      const dependency = await postJson<Dependency>(`/api/projects/${projectId}/dependencies`,
        { sourceComponentId: sourceId, targetComponentId: targetId, type: dependencyType, source: "Declared" });
      setDependencies(current => [...current, dependency]); setSourceId(""); setTargetId(""); setError("");
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to create dependency."); }
  }
  return <main className="page">
    <Link className="back-link" href={`/projects/${projectId}`}>← Project overview</Link><div className="eyebrow">PROJECT STRUCTURE</div>
    <h1>{project?.name ?? "Components"}</h1><p className="lede">Register system components and the dependencies you know about.</p>
    <ProjectNav projectId={projectId} active="Components" />
    {error && <p className="error" role="alert">{error}</p>}
    <section className="panel"><div className="section-heading"><h2>Components</h2><Link href={`/projects/${projectId}/topology`}>Open topology →</Link></div>
      {components.map(component => <div className="list-row" key={component.id}><span className="version">{component.type}</span><div><strong>{component.name}</strong><small>{component.owner ?? "No owner"}{component.description ? ` · ${component.description}` : ""}</small></div></div>)}
      {project && components.length === 0 && <p className="muted">No components yet. Add a service, database, frontend, or other system component to start the topology.</p>}</section>
    {canEdit && <section className="panel"><h2>Add component</h2><form className="form-panel compact-form" onSubmit={createComponent}>
      <div className="form-row"><label>Name<input required minLength={2} maxLength={120} value={name} onChange={event => setName(event.target.value)} placeholder="Payment Service" /></label><label>Type<select value={type} onChange={event => setType(event.target.value)}>{componentTypes.map(option => <option key={option}>{option}</option>)}</select></label></div>
      <label>Description<textarea maxLength={2000} value={description} onChange={event => setDescription(event.target.value)} /></label>
      <div className="form-row"><label>Owner<input maxLength={120} value={owner} onChange={event => setOwner(event.target.value)} /></label><label>Repository URL<input type="url" maxLength={500} value={repositoryUrl} onChange={event => setRepositoryUrl(event.target.value)} placeholder="https://…" /></label></div>
      <button className="button primary">Add component</button></form></section>}
    <section className="panel"><h2>Dependencies</h2>{dependencies.map(dependency => <div className="list-row" key={dependency.id}><span className="version">{dependency.type}</span><div><strong>{componentName(dependency.sourceComponentId)} → {componentName(dependency.targetComponentId)}</strong><small>{dependency.source}</small></div></div>)}{project && dependencies.length === 0 && <p className="muted">No dependencies recorded yet.</p>}</section>
    {canEdit && components.length >= 2 && <section className="panel"><h2>Declare a dependency</h2><p className="muted">Source depends on target. Observed edges require actual supplied observations and are not created by this form.</p><form className="inline-form" onSubmit={createDependency}>
      <label>Source<select required value={sourceId} onChange={event => setSourceId(event.target.value)}><option value="">Choose component</option>{components.map(component => <option key={component.id} value={component.id}>{component.name}</option>)}</select></label>
      <label>Target<select required value={targetId} onChange={event => setTargetId(event.target.value)}><option value="">Choose component</option>{components.map(component => <option key={component.id} value={component.id}>{component.name}</option>)}</select></label>
      <label>Type<select value={dependencyType} onChange={event => setDependencyType(event.target.value)}>{dependencyTypes.map(option => <option key={option}>{option}</option>)}</select></label>
      <button className="button primary">Add dependency</button></form></section>}
  </main>;
}
