"use client";
import { useParams } from "next/navigation";
import { TopologyWorkspace } from "@/components/TopologyWorkspace";
export default function TopologyPage() { const { projectId } = useParams<{ projectId: string }>(); return <TopologyWorkspace projectId={projectId} />; }
