"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { ProjectNav } from "@/components/ProjectNav";
import { getJson, Iteration, postJson, Project, ProjectMember, sendJson, Tag, WorkItem, WorkItemActivity, WorkItemPriority, WorkItemState, WorkItemTag, WorkItemType } from "@/lib/api";

type Draft = { type: WorkItemType; title: string; description: string; priority: WorkItemPriority; assignedToUserId: string; parentId: string; storyPoints: string; originalEstimateHours: string; remainingWorkHours: string; iterationId: string; area: string; tagIds: string[] };
const states: WorkItemState[] = ["New", "Active", "Resolved", "Closed"];
const types: WorkItemType[] = ["UserStory", "Task", "Bug"];
const priorities: WorkItemPriority[] = ["Critical", "High", "Medium", "Low"];
const emptyDraft = (): Draft => ({ type: "Task", title: "", description: "", priority: "Medium", assignedToUserId: "", parentId: "", storyPoints: "", originalEstimateHours: "", remainingWorkHours: "", iterationId: "", area: "", tagIds: [] });
const numberOrNull = (value: string) => value.trim() === "" ? null : Number(value);

export default function BoardPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [project, setProject] = useState<Project | null>(null);
  const [items, setItems] = useState<WorkItem[]>([]);
  const [members, setMembers] = useState<ProjectMember[]>([]);
  const [iterations, setIterations] = useState<Iteration[]>([]);
  const [tags, setTags] = useState<Tag[]>([]);
  const [links, setLinks] = useState<WorkItemTag[]>([]);
  const [meId, setMeId] = useState("");
  const [view, setView] = useState<"board" | "backlog">("board");
  const [search, setSearch] = useState("");
  const [typeFilter, setTypeFilter] = useState("");
  const [stateFilter, setStateFilter] = useState("");
  const [priorityFilter, setPriorityFilter] = useState("");
  const [assigneeFilter, setAssigneeFilter] = useState("");
  const [iterationFilter, setIterationFilter] = useState("");
  const [editing, setEditing] = useState<WorkItem | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [activity, setActivity] = useState<WorkItemActivity[]>([]);
  const [newTag, setNewTag] = useState("");
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    try {
      const itemQuery = view === "backlog" && stateFilter === "Removed" ? "?state=Removed" : "";
      const [p, work, people, cycles, labels, attached, me] = await Promise.all([
        getJson<Project>(`/api/projects/${projectId}`), getJson<WorkItem[]>(`/api/projects/${projectId}/work-items${itemQuery}`),
        getJson<ProjectMember[]>(`/api/projects/${projectId}/members`), getJson<Iteration[]>(`/api/projects/${projectId}/iterations`),
        getJson<Tag[]>(`/api/projects/${projectId}/tags`), getJson<WorkItemTag[]>(`/api/projects/${projectId}/work-item-tags`),
        getJson<{ id: string }>("/api/me")
      ]);
      setProject(p); setItems(work); setMembers(people); setIterations(cycles); setTags(labels); setLinks(attached); setMeId(me.id); setError("");
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to load board."); }
    finally { setLoading(false); }
  }, [projectId, stateFilter, view]);
  useEffect(() => { void load(); }, [load]);

  const canEdit = members.some(member => member.userId === meId && member.role !== "Viewer");
  const memberName = (id: string | null) => members.find(member => member.userId === id)?.displayName ?? "Unassigned";
  const tagIdsFor = (itemId: string) => links.filter(link => link.workItemId === itemId).map(link => link.tagId);
  const tagNamesFor = (itemId: string) => tags.filter(tag => tagIdsFor(itemId).includes(tag.id));
  const filtered = useMemo(() => items.filter(item => {
    if (search && !`${item.title} ${item.description} ${item.sequenceNumber}`.toLowerCase().includes(search.toLowerCase())) return false;
    if (typeFilter && item.type !== typeFilter) return false;
    if (priorityFilter && item.priority !== priorityFilter) return false;
    if (view === "backlog" && stateFilter && item.state !== stateFilter) return false;
    if (assigneeFilter === "me" && item.assignedToUserId !== meId) return false;
    if (assigneeFilter === "unassigned" && item.assignedToUserId !== null) return false;
    if (assigneeFilter && !["me", "unassigned"].includes(assigneeFilter) && item.assignedToUserId !== assigneeFilter) return false;
    if (iterationFilter === "backlog" && item.iterationId !== null) return false;
    if (iterationFilter === "current" && !iterations.some(iteration => iteration.id === item.iterationId && iteration.state === "Current")) return false;
    if (iterationFilter && !["backlog", "current"].includes(iterationFilter) && item.iterationId !== iterationFilter) return false;
    return true;
  }), [items, search, typeFilter, priorityFilter, view, stateFilter, assigneeFilter, meId, iterationFilter, iterations]);

  function openNew() { setEditing(null); setDraft(emptyDraft()); setActivity([]); setDrawerOpen(true); }
  function openItem(item: WorkItem) {
    setEditing(item);
    setDraft({ type: item.type, title: item.title, description: item.description, priority: item.priority,
      assignedToUserId: item.assignedToUserId ?? "", parentId: item.parentId ?? "", storyPoints: item.storyPoints?.toString() ?? "",
      originalEstimateHours: item.originalEstimateHours?.toString() ?? "", remainingWorkHours: item.remainingWorkHours?.toString() ?? "",
      iterationId: item.iterationId ?? "", area: item.area ?? "", tagIds: tagIdsFor(item.id) });
    setActivity([]); setDrawerOpen(true);
    void getJson<WorkItemActivity[]>(`/api/work-items/${item.id}/activity`).then(setActivity).catch(() => setActivity([]));
  }
  function changeDraft<K extends keyof Draft>(key: K, value: Draft[K]) { setDraft(current => ({ ...current, [key]: value })); }
  async function syncTags(itemId: string, desired: string[]) {
    const current = tagIdsFor(itemId);
    for (const id of desired.filter(id => !current.includes(id))) await sendJson<void>("POST", `/api/work-items/${itemId}/tags/${id}`);
    for (const id of current.filter(id => !desired.includes(id))) await sendJson<void>("DELETE", `/api/work-items/${itemId}/tags/${id}`);
  }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError("");
    const payload = { type: draft.type, title: draft.title, description: draft.description, priority: draft.priority,
      assignedToUserId: draft.assignedToUserId || null, parentId: draft.parentId || null, storyPoints: numberOrNull(draft.storyPoints),
      originalEstimateHours: numberOrNull(draft.originalEstimateHours), remainingWorkHours: numberOrNull(draft.remainingWorkHours),
      iterationId: draft.iterationId || null, area: draft.area || null };
    try {
      const saved = editing ? await sendJson<WorkItem>("PUT", `/api/work-items/${editing.id}`, payload)
        : await postJson<WorkItem>(`/api/projects/${projectId}/work-items`, payload);
      await syncTags(saved.id, draft.tagIds);
      setDrawerOpen(false); await load();
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to save work item."); }
    finally { setBusy(false); }
  }
  async function move(item: WorkItem, state: WorkItemState) {
    try { await sendJson<WorkItem>("PATCH", `/api/work-items/${item.id}/state`, { state }); await load(); }
    catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to move work item."); }
  }
  async function addTag() {
    if (!newTag.trim()) return;
    try {
      const tag = await postJson<Tag>(`/api/projects/${projectId}/tags`, { name: newTag.trim(), colorToken: "blue" });
      setTags(current => [...current, tag]); changeDraft("tagIds", [...draft.tagIds, tag.id]); setNewTag("");
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Unable to add tag."); }
  }
  const badge = (item: WorkItem) => <><span className={`work-type ${item.type.toLowerCase()}`}>{item.type === "UserStory" ? "Story" : item.type}</span><span className="work-number">#{item.sequenceNumber}</span></>;
  const itemTags = (item: WorkItem) => <div className="work-tags">{tagNamesFor(item.id).map(tag => <span className={`work-tag tag-${tag.colorToken ?? "slate"}`} key={tag.id}>{tag.name}</span>)}</div>;

  return <main className="page board-page">
    <Link className="back-link" href="/projects">← Projects</Link><div className="eyebrow">PROJECT WORK</div>
    <div className="page-heading"><div><h1>{project?.name ?? "Boards"}</h1><p className="lede">Plan and track work alongside operational context.</p></div>{canEdit && <button className="button primary" onClick={openNew}>New work item</button>}</div>
    <ProjectNav projectId={projectId} active="Boards" />
    <div className="board-toolbar"><div className="board-tabs"><button className={view === "board" ? "active" : ""} onClick={() => setView("board")}>Board</button><button className={view === "backlog" ? "active" : ""} onClick={() => setView("backlog")}>Backlog</button></div>
      <input aria-label="Search work items" placeholder="Search work items" value={search} onChange={event => setSearch(event.target.value)} />
      <select aria-label="Filter type" value={typeFilter} onChange={event => setTypeFilter(event.target.value)}><option value="">All types</option>{types.map(type => <option key={type}>{type}</option>)}</select>
      {view === "backlog" && <select aria-label="Filter state" value={stateFilter} onChange={event => setStateFilter(event.target.value)}><option value="">All states</option>{[...states, "Removed"].map(state => <option key={state}>{state}</option>)}</select>}
      <select aria-label="Filter priority" value={priorityFilter} onChange={event => setPriorityFilter(event.target.value)}><option value="">All priorities</option>{priorities.map(priority => <option key={priority}>{priority}</option>)}</select>
      <select aria-label="Filter assignee" value={assigneeFilter} onChange={event => setAssigneeFilter(event.target.value)}><option value="">All assignees</option><option value="me">Assigned to me</option><option value="unassigned">Unassigned</option>{members.map(member => <option value={member.userId} key={member.userId}>{member.displayName}</option>)}</select>
      <select aria-label="Filter iteration" value={iterationFilter} onChange={event => setIterationFilter(event.target.value)}><option value="">All iterations</option><option value="current">Current</option><option value="backlog">Backlog</option>{iterations.map(iteration => <option value={iteration.id} key={iteration.id}>{iteration.name}</option>)}</select>
    </div>
    {error && <p className="error" role="alert">{error}</p>}
    {loading && <p className="muted">Loading work items…</p>}
    {!loading && project && view === "board" && <div className="board-columns">{states.map(state => <section className="board-column" key={state}><h2>{state}<span>{filtered.filter(item => item.state === state).length}</span></h2><div className="board-cards">{filtered.filter(item => item.state === state).map(item => <article className="work-card" key={item.id}><div className="work-card-head">{badge(item)}<span className={`priority priority-${item.priority.toLowerCase()}`}>{item.priority}</span></div><button className="work-title" onClick={() => openItem(item)}>{item.title}</button>{itemTags(item)}<div className="work-card-foot"><span>{memberName(item.assignedToUserId)}</span>{item.storyPoints !== null && <span>{item.storyPoints} pts</span>}</div>{canEdit && <select aria-label={`Move ${item.title}`} value={item.state} onChange={event => void move(item, event.target.value as WorkItemState)}>{[...states, "Removed"].map(option => <option key={option}>{option}</option>)}</select>}</article>)}{filtered.every(item => item.state !== state) && <p className="muted board-empty">No items</p>}</div></section>)}</div>}
    {!loading && project && view === "backlog" && <div className="panel table-panel"><table><thead><tr><th>ID</th><th>Type</th><th>Title</th><th>State</th><th>Priority</th><th>Assignee</th><th>Iteration</th><th>Tags</th></tr></thead><tbody>{filtered.map(item => <tr key={item.id}><td>#{item.sequenceNumber}</td><td>{item.type}</td><td><button className="table-link" onClick={() => openItem(item)}>{item.title}</button></td><td>{item.state}</td><td>{item.priority}</td><td>{memberName(item.assignedToUserId)}</td><td>{iterations.find(iteration => iteration.id === item.iterationId)?.name ?? "Backlog"}</td><td>{itemTags(item)}</td></tr>)}</tbody></table>{filtered.length === 0 && <p className="muted">No work items match these filters.</p>}</div>}
    {!loading && project && items.length === 0 && view === "board" && <div className="panel empty-state"><h2>No work items yet</h2><p className="muted">Create a task, story, or bug to start the board.</p>{canEdit && <button className="button primary" onClick={openNew}>Create work item</button>}</div>}
    {drawerOpen && <div className="drawer-backdrop" onClick={() => setDrawerOpen(false)}><aside className="work-drawer" role="dialog" aria-modal="true" aria-label={editing ? `Edit work item ${editing.sequenceNumber}` : "New work item"} onClick={event => event.stopPropagation()} onKeyDown={event => { if (event.key === "Escape") setDrawerOpen(false); }}><div className="drawer-heading"><div><div className="eyebrow">{editing ? `WORK ITEM #${editing.sequenceNumber}` : "NEW WORK ITEM"}</div><h2>{editing ? "Work item details" : "Create work item"}</h2></div><button aria-label="Close work item" onClick={() => setDrawerOpen(false)}>×</button></div><form onSubmit={submit} className="form-panel drawer-form"><fieldset disabled={!canEdit || busy}><div className="form-row"><label>Type<select value={draft.type} onChange={event => changeDraft("type", event.target.value as WorkItemType)}>{types.map(type => <option key={type}>{type}</option>)}</select></label><label>Priority<select value={draft.priority} onChange={event => changeDraft("priority", event.target.value as WorkItemPriority)}>{priorities.map(priority => <option key={priority}>{priority}</option>)}</select></label></div><label>Title<input required minLength={2} maxLength={240} value={draft.title} onChange={event => changeDraft("title", event.target.value)} /></label><label>Description<textarea maxLength={10000} value={draft.description} onChange={event => changeDraft("description", event.target.value)} /></label><div className="form-row"><label>Assignee<select value={draft.assignedToUserId} onChange={event => changeDraft("assignedToUserId", event.target.value)}><option value="">Unassigned</option>{members.map(member => <option value={member.userId} key={member.userId}>{member.displayName}</option>)}</select></label><label>Iteration<select value={draft.iterationId} onChange={event => changeDraft("iterationId", event.target.value)}><option value="">Backlog</option>{iterations.map(iteration => <option value={iteration.id} key={iteration.id}>{iteration.name}</option>)}</select></label></div><div className="form-row"><label>Story points<input type="number" min="0" value={draft.storyPoints} onChange={event => changeDraft("storyPoints", event.target.value)} /></label><label>Area<input maxLength={120} value={draft.area} onChange={event => changeDraft("area", event.target.value)} /></label></div><div className="form-row"><label>Original estimate (hours)<input type="number" min="0" step="0.25" value={draft.originalEstimateHours} onChange={event => changeDraft("originalEstimateHours", event.target.value)} /></label><label>Remaining work (hours)<input type="number" min="0" step="0.25" value={draft.remainingWorkHours} onChange={event => changeDraft("remainingWorkHours", event.target.value)} /></label></div><label>Parent work item<select value={draft.parentId} onChange={event => changeDraft("parentId", event.target.value)}><option value="">None</option>{items.filter(item => item.id !== editing?.id).map(item => <option value={item.id} key={item.id}>#{item.sequenceNumber} {item.title}</option>)}</select></label><div className="tag-picker"><strong>Tags</strong><div>{tags.map(tag => <label key={tag.id}><input type="checkbox" checked={draft.tagIds.includes(tag.id)} onChange={event => changeDraft("tagIds", event.target.checked ? [...draft.tagIds, tag.id] : draft.tagIds.filter(id => id !== tag.id))} /><span className={`work-tag tag-${tag.colorToken ?? "slate"}`}>{tag.name}</span></label>)}</div><div className="inline-form"><input aria-label="New tag name" maxLength={60} placeholder="New tag" value={newTag} onChange={event => setNewTag(event.target.value)} /><button type="button" onClick={() => void addTag()}>Add tag</button></div></div></fieldset>{error && <p className="error" role="alert">{error}</p>}{canEdit && <div className="form-actions"><button type="button" onClick={() => setDrawerOpen(false)}>Cancel</button><button className="button primary" disabled={busy}>{busy ? "Saving…" : "Save work item"}</button></div>}</form>{editing && <section className="activity-list"><h3>Activity</h3>{activity.map(entry => <p key={entry.id}><strong>{entry.type}</strong> {entry.fieldName && `${entry.fieldName}: `}{entry.oldValue && `${entry.fieldName === "Tag" ? tags.find(tag => tag.id === entry.oldValue)?.name ?? entry.oldValue : entry.oldValue} → `}{entry.fieldName === "Tag" ? tags.find(tag => tag.id === entry.newValue)?.name ?? entry.newValue : entry.newValue} <small>{new Date(entry.occurredAt).toLocaleString()}</small></p>)}</section>}</aside></div>}
  </main>;
}
