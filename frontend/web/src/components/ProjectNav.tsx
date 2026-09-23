import Link from "next/link";

export function ProjectNav({ projectId, active }: { projectId: string; active: string }) {
  const items = [
    ["Overview", `/projects/${projectId}`],
    ["Boards", `/projects/${projectId}/boards`],
    ["Components", `/projects/${projectId}/components`],
    ["Environments", `/projects/${projectId}/environments`],
    ["Topology", `/projects/${projectId}/topology`],
    ["Compare", `/projects/${projectId}/comparison`],
    ["Events", `/projects/${projectId}/events`],
    ["Deployments", `/projects/${projectId}/deployments`],
    ["Settings", `/projects/${projectId}/settings`]
  ];
  return <nav className="project-nav" aria-label="Project pages">{items.map(([label, href]) =>
    <Link className={label === active ? "active" : ""} href={href} key={href}>{label}</Link>)}</nav>;
}
