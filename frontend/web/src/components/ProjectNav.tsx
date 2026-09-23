import Link from "next/link";

export function ProjectNav({ projectId, active }: { projectId: string; active: string }) {
  const items = [
    ["Overview", `/projects/${projectId}`],
    ["Boards", `/projects/${projectId}/boards`],
    ["Topology", `/projects/${projectId}/topology`],
    ["Events", `/projects/${projectId}/events`],
    ["Deployments", `/projects/${projectId}/deployments`]
  ];
  return <nav className="project-nav" aria-label="Project pages">{items.map(([label, href]) =>
    <Link className={label === active ? "active" : ""} href={href} key={href}>{label}</Link>)}</nav>;
}
