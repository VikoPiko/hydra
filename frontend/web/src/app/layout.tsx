import type { Metadata } from "next";
import "./globals.css";
import Link from "next/link";
import { auth, signOut } from "@/auth";

export const metadata: Metadata = { title: "DevOpsPlatform", description: "Operational topology and dependency impact" };

export default async function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  const session = await auth();
  return <html lang="en"><body>{session?.user && <header className="app-header"><div className="app-header-inner"><Link href="/projects" className="app-brand">DevOpsPlatform</Link><div className="app-account"><span>{session.user.name ?? session.user.email ?? "Signed in"}</span><form action={async () => { "use server"; await signOut({ redirectTo: "/signin" }); }}><button className="button" type="submit">Sign out</button></form></div></div></header>}{children}</body></html>;
}
