import { redirect } from "next/navigation";
import { auth } from "@/auth";

export default async function ProjectsLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  if (!await auth()) redirect("/signin");
  return children;
}
