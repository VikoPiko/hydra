import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = { title: "DevOpsPlatform", description: "Operational topology and dependency impact" };

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body>{children}</body></html>;
}
