import { redirect } from "next/navigation";

/** «Compras» no tiene tablero propio: arranca en lo que falta comprar. */
export default function ComprasPage() {
  redirect("/compras/necesidades");
}
