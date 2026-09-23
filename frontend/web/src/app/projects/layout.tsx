import { redirect } from "next/navigation";
import { auth, type BackendSession } from "@/auth";

export default async function ProjectsLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  const session = await auth() as BackendSession | null;
  if (!session || session.error) redirect("/signin");
  return children;
}
