"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { getJson, Project } from "@/lib/api";

export default function ProjectsPage() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [error, setError] = useState("");
  useEffect(() => { getJson<Project[]>("/api/projects").then(setProjects).catch(e => setError(e.message)); }, []);
  return <main className="page">
    <div className="eyebrow">DEVOPS PLATFORM / SYSTEMS</div>
    <h1>Projects</h1><p className="lede">One view of lifecycle metadata, runtime health, and dependency impact.</p>
    {error && <p className="error">{error}. Check that the API is running.</p>}
    <div className="project-grid">{projects.map(project => <Link className="project-card" href={`/projects/${project.id}`} key={project.id}>
      <div className="card-mark">{project.name.slice(0, 1)}</div><div><h2>{project.name}</h2><p>{project.description}</p><span>Open workspace →</span></div>
    </Link>)}</div>
    {!error && projects.length === 0 && <p className="muted">Loading projects…</p>}
  </main>;
}
