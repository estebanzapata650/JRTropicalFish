import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { scriptTemaInicial } from "@/components/TemaProvider";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: { default: "JR Tropical Fish", template: "%s · JR Tropical Fish" },
  description: "Inventario, importaciones y exportaciones de peces tropicales",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html
      lang="es"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
      // El script aplica el tema guardado antes del primer pintado; sin esto la página
      // parpadea en claro antes de pasar a oscuro.
      suppressHydrationWarning
    >
      <head>
        <script dangerouslySetInnerHTML={{ __html: scriptTemaInicial }} />
      </head>
      <body className="flex min-h-full flex-col bg-fondo text-texto">{children}</body>
    </html>
  );
}
