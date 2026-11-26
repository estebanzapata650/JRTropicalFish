"use client";

import { useState } from "react";
import { Campo, CampoAncho, RejillaCampos } from "@/components/campos";
import { SelectorMaestro } from "@/components/SelectorMaestro";
import { ESTADOS_CLIENTE, LICENCIAS, TIPOS_CLIENTE, type Cliente } from "@/lib/crm";
import { botonPrimarioClass, botonSecundarioClass, inputClass, textareaClass } from "@/lib/estilos";
import { SelectMapa } from "../comun";

export type DatosCliente = {
  nombreEmpresa: string;
  contactoPrincipal: string;
  email: string;
  telefonoWhatsapp: string;
  pais: string;
  ciudad: string;
  aeropuertoIata: string;
  tipoCliente: string;
  licenciaImportacion: string;
  estado: string;
  notas: string;
};

const vacio: DatosCliente = {
  nombreEmpresa: "",
  contactoPrincipal: "",
  email: "",
  telefonoWhatsapp: "",
  pais: "",
  ciudad: "",
  aeropuertoIata: "",
  tipoCliente: "b2b_internacional",
  licenciaImportacion: "no_aplica",
  estado: "prospecto",
  notas: "",
};

/** Formulario de cliente (crear y editar). Controlado: un error de la API no borra lo escrito. */
export function FormularioCliente({
  cliente,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  cliente: Cliente | null;
  guardando: boolean;
  error: string | null;
  onGuardar: (datos: DatosCliente) => void;
  onCancelar: () => void;
}) {
  const [d, setD] = useState<DatosCliente>(() =>
    cliente
      ? {
          nombreEmpresa: cliente.nombreEmpresa,
          contactoPrincipal: cliente.contactoPrincipal ?? "",
          email: cliente.email ?? "",
          telefonoWhatsapp: cliente.telefonoWhatsapp ?? "",
          pais: cliente.pais,
          ciudad: cliente.ciudad ?? "",
          aeropuertoIata: cliente.aeropuertoIata ?? "",
          tipoCliente: cliente.tipoCliente,
          licenciaImportacion: cliente.licenciaImportacion,
          estado: cliente.estado,
          notas: cliente.notas ?? "",
        }
      : vacio,
  );
  const set = (campo: keyof DatosCliente) => (valor: string) => setD((x) => ({ ...x, [campo]: valor }));

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar(d);
      }}
      className="flex flex-col gap-6"
    >
      <RejillaCampos>
        <CampoAncho>
          <Campo label="Nombre de la empresa" htmlFor="nombreEmpresa">
            <input id="nombreEmpresa" value={d.nombreEmpresa} onChange={(e) => set("nombreEmpresa")(e.target.value)} maxLength={200} className={inputClass} />
          </Campo>
        </CampoAncho>
        <Campo label="Contacto principal (opcional)" htmlFor="contactoPrincipal">
          <input id="contactoPrincipal" value={d.contactoPrincipal} onChange={(e) => set("contactoPrincipal")(e.target.value)} maxLength={150} className={inputClass} />
        </Campo>
        <Campo label="Teléfono / WhatsApp (opcional)" htmlFor="telefonoWhatsapp" hint="Con indicativo del país, ej. +81 3 1234 5678">
          <input id="telefonoWhatsapp" type="tel" value={d.telefonoWhatsapp} onChange={(e) => set("telefonoWhatsapp")(e.target.value)} maxLength={40} className={inputClass} />
        </Campo>
        <Campo label="Correo (opcional)" htmlFor="email">
          <input id="email" type="email" value={d.email} onChange={(e) => set("email")(e.target.value)} maxLength={320} className={inputClass} />
        </Campo>
        <Campo label="País" htmlFor="pais">
          <input id="pais" value={d.pais} onChange={(e) => set("pais")(e.target.value)} maxLength={100} className={inputClass} />
        </Campo>
        <Campo label="Ciudad (opcional)" htmlFor="ciudad">
          <input id="ciudad" value={d.ciudad} onChange={(e) => set("ciudad")(e.target.value)} maxLength={100} className={inputClass} />
        </Campo>
        <Campo label="Aeropuerto de destino (opcional)" htmlFor="aeropuertoIata" hint="Si no está, agrégalo en Administración → Maestros → Aeropuertos.">
          <SelectorMaestro
            id="aeropuertoIata"
            clave="aeropuertos"
            guardar="codigo"
            valor={d.aeropuertoIata}
            onChange={set("aeropuertoIata")}
            vacio="Sin aeropuerto"
            placeholder="Código o ciudad: HKG, Tokio…"
          />
        </Campo>
        <Campo label="Tipo de cliente" htmlFor="tipoCliente">
          <SelectMapa id="tipoCliente" valor={d.tipoCliente} onChange={set("tipoCliente")} mapa={TIPOS_CLIENTE} />
        </Campo>
        <Campo label="Licencia de importación" htmlFor="licenciaImportacion">
          <SelectMapa id="licenciaImportacion" valor={d.licenciaImportacion} onChange={set("licenciaImportacion")} mapa={LICENCIAS} />
        </Campo>
        <Campo label="Estado" htmlFor="estado">
          <SelectMapa id="estado" valor={d.estado} onChange={set("estado")} mapa={ESTADOS_CLIENTE} />
        </Campo>
        <CampoAncho>
          <Campo label="Notas (opcional)" htmlFor="notas">
            <textarea id="notas" value={d.notas} onChange={(e) => set("notas")(e.target.value)} maxLength={2000} className={textareaClass} />
          </Campo>
        </CampoAncho>
      </RejillaCampos>

      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button type="button" onClick={onCancelar} className={botonSecundarioClass}>
          Cancelar
        </button>
        <button type="submit" disabled={guardando} className={botonPrimarioClass}>
          {guardando ? "Guardando…" : "Guardar"}
        </button>
      </div>
    </form>
  );
}
