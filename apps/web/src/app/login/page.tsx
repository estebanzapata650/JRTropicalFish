import type { Metadata } from "next";
import { LoginForm } from "@/components/LoginForm";
import { loginAction } from "./actions";

export const metadata: Metadata = {
  title: "Ingresar",
};

export default function LoginPage() {
  return <LoginForm action={loginAction} titulo="JR Tropical Fish" subtitulo="Inventario, importaciones y exportaciones" />;
}
