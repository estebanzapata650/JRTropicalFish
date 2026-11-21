using JRTropicalFish.Infrastructure.Persistence.Sembrado;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Funciones de exportaciones (documentos comerciales del despacho) y su sembrado inicial con los valores
    /// de la hoja «Parametros» del libro de formatos: cajas, bolsas, cargos y tarifas de flete.
    ///
    /// <para>
    /// Los datos de la empresa se siembran solo con lo que va impreso en los documentos públicos (razón social,
    /// NIT, dirección, contacto de ventas); los datos bancarios y el firmante se llenan en Configuración.
    /// La numeración de facturas sigue la del Excel: la próxima es la 12187 (consecutivo <c>FACT</c>).
    /// </para>
    ///
    /// <para>El orden de columnas de cada RETURNS TABLE = el record en <c>Application/Exportaciones/ExportacionesModels.cs</c>.</para>
    /// </summary>
    public partial class FuncionesExportaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SembradoPermisos.Aplicar(migrationBuilder);
            Sembrar(migrationBuilder);
            CrearConfiguracion(migrationBuilder);
            CrearEmbarques(migrationBuilder);
            CrearEscrituraEmbarques(migrationBuilder);
        }

        private static void Sembrar(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO datos_empresa (id, razon_social, nit, direccion, ciudad_pais, telefono, email, sitio_web)
                VALUES (1, 'JR TROPICAL FISH LTDA', '830.058.328-1', 'Carrera 108 N° 23 - 20', 'Bogotá D.C., Colombia - South America',
                        '+57 601 415 1910', 'sales_jrtropicalfishcolombia@hotmail.com', 'www.jrtropicalfish.com')
                ON CONFLICT DO NOTHING;

                INSERT INTO tarifas_flete (id, tarifa_kg_jr, tarifa_kg_cliente, fuel_kg, jp_fee_kg, vsc_kg, vsc_minimo, dbc_porcentaje,
                                           dbc_minimo, df_porcentaje, df_minimo, due_agent_jr, due_agent_cliente, due_carrier)
                VALUES (1, 6.5, 6.7, 0, 1, 0.13, 15, 0, 0, 0, 0, 140, 250, 55)
                ON CONFLICT DO NOTHING;

                INSERT INTO tipos_caja (id, nombre, descripcion_factura, costo_cop, precio_usd, kg_por_caja, pisos, orden, activo, creado_en)
                VALUES ('01a1ff00-0000-7000-8000-000000000001', 'Caja Holandesa (doble)', 'CardBoard + Styrofoam Box Packing Double', 61117.82, 22, 20, 1, 1, true, now()),
                       ('01a1ff00-0000-7000-8000-000000000002', 'Caja Europea (sencilla)', 'CardBoard + Styrofoam Box Packing Single', 35705.56, 13, 10, 1, 2, true, now()),
                       ('01a1ff00-0000-7000-8000-000000000003', 'Caja Mexicana', 'CardBoard + Styrofoam Sheets', 28795.91, 11, 16, 1, 3, true, now()),
                       ('01a1ff00-0000-7000-8000-000000000004', 'Caja Doble Piso', 'CardBoard + Styrofoam Box 2 Level Floor', 105189.33, 38, 32, 2, 4, true, now()),
                       ('01a1ff00-0000-7000-8000-000000000005', 'Caja Triple Piso', 'CardBoard + Styrofoam Box 3 Level Floor', 0, 40, 45, 3, 5, true, now())
                ON CONFLICT DO NOTHING;

                INSERT INTO tipos_bolsa (id, nombre, tamano, porcion, costo_cop, orden, activo, creado_en)
                SELECT ('01a1ff00-0000-7000-8000-0000000001' || lpad(o::text, 2, '0'))::uuid, n, t, p, c, o, true, now()
                FROM (VALUES ('Export', '25,0"', 1.0, 3490, 1), ('Dolphin', '20,0"', 0.666667, 3490, 2), ('1/2', '18,0"', 0.5, 2440, 3),
                             ('1/3', '14,5"', 0.333333, 1895, 4), ('1/4 Alta', '11,0"', 0.25, 1500, 5), ('1/4 Baja', '11,5"', 0.25, 1550, 6),
                             ('1/6 (10")', '10,0"', 0.166667, 1612, 7), ('1/8 (8")', '8,0"', 0.125, 1380, 8), ('10" Recta', '10,0"', 0.111111, 1190, 9),
                             ('1/10 (7")', '7,0"', 0.1, 1380, 10), ('8" Recta', '8,0"', 0.083333, 1080, 11), ('1/16 (6")', '6,0"', 0.0625, 1320, 12),
                             ('1/20 (5")', '5,0"', 0.05, 1250, 13), ('1/30 (4")', '4,0"', 0.033333, 1250, 14)) AS b(n, t, p, c, o)
                ON CONFLICT DO NOTHING;

                INSERT INTO cargos_exportacion (codigo, nombre, precio_usd, orden)
                VALUES ('zoosanitario', 'Zoosanitary certificate', 50, 1), ('origen', 'Certificate of Origin', 20, 2),
                       ('cites', 'CITES', 160, 3), ('airport_fee', 'Airport fee', 40, 4), ('heat_pack', 'Heat Pack', 1.6, 5)
                ON CONFLICT DO NOTHING;

                INSERT INTO consecutivos (prefijo, ultimo) VALUES ('EXP', 0), ('FACT', 12186) ON CONFLICT DO NOTHING;

                -- Siguiente número entero de un consecutivo (las facturas van sin prefijo: 12187).
                CREATE FUNCTION fn_siguiente_numero(p_prefijo text) RETURNS integer
                LANGUAGE sql AS $$
                    INSERT INTO consecutivos (prefijo, ultimo) VALUES (p_prefijo, 1)
                    ON CONFLICT (prefijo) DO UPDATE SET ultimo = consecutivos.ultimo + 1
                    RETURNING ultimo;
                $$;
                """);
        }

        private static void CrearConfiguracion(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_datos_empresa()
                RETURNS TABLE (razon_social text, nit text, direccion text, ciudad_pais text, telefono text, email text, sitio_web text,
                               banco text, swift text, cuenta_bancaria text, beneficiario text, direccion_banco text, firmante_nombre text,
                               firmante_cargo text, firmante_telefono text, logo_clave text, actualizado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT razon_social::text, nit::text, direccion::text, ciudad_pais::text, telefono::text, email::text, sitio_web::text,
                           banco::text, swift::text, cuenta_bancaria::text, beneficiario::text, direccion_banco::text, firmante_nombre::text,
                           firmante_cargo::text, firmante_telefono::text, logo_clave::text, actualizado_en
                    FROM datos_empresa WHERE id = 1;
                $$;

                CREATE PROCEDURE sp_datos_empresa_actualizar(
                    p_razon_social text, p_nit text, p_direccion text, p_ciudad_pais text, p_telefono text, p_email text, p_sitio_web text,
                    p_banco text, p_swift text, p_cuenta text, p_beneficiario text, p_direccion_banco text, p_firmante_nombre text,
                    p_firmante_cargo text, p_firmante_telefono text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE datos_empresa SET razon_social = p_razon_social, nit = p_nit, direccion = p_direccion, ciudad_pais = p_ciudad_pais,
                           telefono = p_telefono, email = p_email, sitio_web = p_sitio_web, banco = p_banco, swift = p_swift,
                           cuenta_bancaria = p_cuenta, beneficiario = p_beneficiario, direccion_banco = p_direccion_banco,
                           firmante_nombre = p_firmante_nombre, firmante_cargo = p_firmante_cargo, firmante_telefono = p_firmante_telefono,
                           actualizado_en = p_fecha
                    WHERE id = 1;
                $$;

                CREATE PROCEDURE sp_datos_empresa_logo(p_clave text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE datos_empresa SET logo_clave = p_clave, actualizado_en = p_fecha WHERE id = 1;
                $$;

                CREATE FUNCTION fn_tarifas_flete()
                RETURNS TABLE (tarifa_kg_jr numeric, tarifa_kg_cliente numeric, fuel_kg numeric, jp_fee_kg numeric, vsc_kg numeric,
                               vsc_minimo numeric, dbc_porcentaje numeric, dbc_minimo numeric, df_porcentaje numeric, df_minimo numeric,
                               due_agent_jr numeric, due_agent_cliente numeric, due_carrier numeric)
                LANGUAGE sql STABLE AS $$
                    SELECT tarifa_kg_jr, tarifa_kg_cliente, fuel_kg, jp_fee_kg, vsc_kg, vsc_minimo, dbc_porcentaje, dbc_minimo,
                           df_porcentaje, df_minimo, due_agent_jr, due_agent_cliente, due_carrier
                    FROM tarifas_flete WHERE id = 1;
                $$;

                CREATE PROCEDURE sp_tarifas_flete_actualizar(
                    p_tarifa_kg_jr numeric, p_tarifa_kg_cliente numeric, p_fuel_kg numeric, p_jp_fee_kg numeric, p_vsc_kg numeric,
                    p_vsc_minimo numeric, p_dbc_porcentaje numeric, p_dbc_minimo numeric, p_df_porcentaje numeric, p_df_minimo numeric,
                    p_due_agent_jr numeric, p_due_agent_cliente numeric, p_due_carrier numeric, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE tarifas_flete SET tarifa_kg_jr = p_tarifa_kg_jr, tarifa_kg_cliente = p_tarifa_kg_cliente, fuel_kg = p_fuel_kg,
                           jp_fee_kg = p_jp_fee_kg, vsc_kg = p_vsc_kg, vsc_minimo = p_vsc_minimo, dbc_porcentaje = p_dbc_porcentaje,
                           dbc_minimo = p_dbc_minimo, df_porcentaje = p_df_porcentaje, df_minimo = p_df_minimo, due_agent_jr = p_due_agent_jr,
                           due_agent_cliente = p_due_agent_cliente, due_carrier = p_due_carrier, actualizado_en = p_fecha
                    WHERE id = 1;
                $$;

                CREATE FUNCTION fn_tipos_caja_listar()
                RETURNS TABLE (id uuid, nombre text, descripcion_factura text, costo_cop numeric, precio_usd numeric, kg_por_caja numeric,
                               pisos integer, orden integer, activo boolean)
                LANGUAGE sql STABLE AS $$
                    SELECT id, nombre::text, descripcion_factura::text, costo_cop, precio_usd, kg_por_caja, pisos, orden, activo
                    FROM tipos_caja ORDER BY orden, nombre;
                $$;

                CREATE PROCEDURE sp_tipo_caja_guardar(
                    p_id uuid, p_nombre text, p_descripcion text, p_costo numeric, p_precio numeric, p_kg numeric, p_pisos integer,
                    p_activo boolean, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO tipos_caja (id, nombre, descripcion_factura, costo_cop, precio_usd, kg_por_caja, pisos, orden, activo, creado_en)
                    VALUES (p_id, p_nombre, p_descripcion, p_costo, p_precio, p_kg, p_pisos,
                            (SELECT coalesce(max(orden), 0) + 1 FROM tipos_caja), p_activo, p_fecha)
                    ON CONFLICT (id) DO UPDATE SET nombre = p_nombre, descripcion_factura = p_descripcion, costo_cop = p_costo,
                           precio_usd = p_precio, kg_por_caja = p_kg, pisos = p_pisos, activo = p_activo, actualizado_en = p_fecha;
                $$;

                CREATE FUNCTION fn_tipos_bolsa_listar()
                RETURNS TABLE (id uuid, nombre text, tamano text, porcion numeric, costo_cop numeric, orden integer, activo boolean)
                LANGUAGE sql STABLE AS $$
                    SELECT id, nombre::text, tamano::text, porcion, costo_cop, orden, activo FROM tipos_bolsa ORDER BY orden, nombre;
                $$;

                CREATE PROCEDURE sp_tipo_bolsa_guardar(
                    p_id uuid, p_nombre text, p_tamano text, p_porcion numeric, p_costo numeric, p_activo boolean, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO tipos_bolsa (id, nombre, tamano, porcion, costo_cop, orden, activo, creado_en)
                    VALUES (p_id, p_nombre, p_tamano, p_porcion, p_costo, (SELECT coalesce(max(orden), 0) + 1 FROM tipos_bolsa), p_activo, p_fecha)
                    ON CONFLICT (id) DO UPDATE SET nombre = p_nombre, tamano = p_tamano, porcion = p_porcion, costo_cop = p_costo,
                           activo = p_activo, actualizado_en = p_fecha;
                $$;

                CREATE FUNCTION fn_cargos_exportacion_listar()
                RETURNS TABLE (codigo text, nombre text, precio_usd numeric, orden integer)
                LANGUAGE sql STABLE AS $$
                    SELECT codigo::text, nombre::text, precio_usd, orden FROM cargos_exportacion ORDER BY orden;
                $$;

                CREATE PROCEDURE sp_cargo_exportacion_actualizar(p_codigo text, p_nombre text, p_precio numeric)
                LANGUAGE sql AS $$
                    UPDATE cargos_exportacion SET nombre = p_nombre, precio_usd = p_precio WHERE codigo = p_codigo;
                $$;

                CREATE FUNCTION fn_cliente_exportacion(p_cliente_id uuid)
                RETURNS TABLE (cliente_id uuid, direccion text, eori text, formato_factura text, lista_precios_id uuid, descuento_peces numeric)
                LANGUAGE sql STABLE AS $$
                    SELECT cliente_id, direccion::text, eori::text, formato_factura::text, lista_precios_id, descuento_peces
                    FROM clientes_exportacion WHERE cliente_id = p_cliente_id;
                $$;

                CREATE PROCEDURE sp_cliente_exportacion_guardar(
                    p_cliente_id uuid, p_direccion text, p_eori text, p_formato text, p_lista_id uuid, p_descuento numeric, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO clientes_exportacion (cliente_id, direccion, eori, formato_factura, lista_precios_id, descuento_peces, actualizado_en)
                    VALUES (p_cliente_id, p_direccion, p_eori, p_formato, p_lista_id, p_descuento, p_fecha)
                    ON CONFLICT (cliente_id) DO UPDATE SET direccion = p_direccion, eori = p_eori, formato_factura = p_formato,
                           lista_precios_id = p_lista_id, descuento_peces = p_descuento, actualizado_en = p_fecha;
                $$;
                """);
        }

        private static void CrearEmbarques(MigrationBuilder migrationBuilder)
        {
            const string columnas =
                """
                RETURNS TABLE (id uuid, codigo text, venta_id uuid, venta_codigo text, pedido_cliente_id uuid, pedido_codigo text,
                               cliente_id uuid, cliente_nombre text, comercial_id uuid, comercial_nombre text, estado text,
                               lista_precios_id uuid, lista_nombre text, descuento_peces numeric, aerolinea text, ruta text, awb text,
                               destino text, salida_vuelo timestamptz, llegada_vuelo timestamptz, fecha_despacho date,
                               preinvoice_numero text, preinvoice_fecha date, formato_factura text, factura_numero integer,
                               factura_fecha date, nota_credito_fecha date, awb_valor numeric, anticipo_recibido numeric, tarifas text,
                               cargos text, cajas_estimadas text, cajas_reales text, notas text, lineas integer, unidades integer,
                               cajas_empacadas integer, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT e.id, e.codigo::text, e.venta_id, v.codigo::text, e.pedido_cliente_id, pc.codigo::text, e.cliente_id,
                           c.nombre_empresa::text, e.comercial_id, u.nombre::text, e.estado::text, e.lista_precios_id, lp.nombre::text,
                           e.descuento_peces, e.aerolinea::text, e.ruta::text, e.awb::text, e.destino::text, e.salida_vuelo,
                           e.llegada_vuelo, e.fecha_despacho, e.preinvoice_numero::text, e.preinvoice_fecha, e.formato_factura::text,
                           e.factura_numero, e.factura_fecha, e.nota_credito_fecha, e.awb_valor, e.anticipo_recibido, e.tarifas::text,
                           e.cargos::text, e.cajas_estimadas::text, e.cajas_reales::text, e.notas::text,
                           (SELECT count(*) FROM embarque_lineas WHERE embarque_id = e.id)::integer,
                           coalesce((SELECT sum(cantidad) FROM embarque_lineas WHERE embarque_id = e.id), 0)::integer,
                           (SELECT count(DISTINCT numero_caja) FROM embarque_empaque WHERE embarque_id = e.id)::integer,
                           e.creado_en
                    FROM embarques e
                    JOIN ventas v ON v.id = e.venta_id
                    JOIN clientes c ON c.id = e.cliente_id
                    JOIN usuarios u ON u.id = e.comercial_id
                    LEFT JOIN pedidos_cliente pc ON pc.id = e.pedido_cliente_id
                    LEFT JOIN listas_precios lp ON lp.id = e.lista_precios_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_embarques_listar(p_comercial_id uuid)
                {columnas}
                    WHERE p_comercial_id IS NULL OR e.comercial_id = p_comercial_id
                    ORDER BY e.creado_en DESC;
                $$;

                CREATE FUNCTION fn_embarque_por_id(p_id uuid)
                {columnas}
                    WHERE e.id = p_id;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_embarque_id_por_venta(p_venta_id uuid) RETURNS uuid
                LANGUAGE sql STABLE AS $$ SELECT id FROM embarques WHERE venta_id = p_venta_id; $$;

                CREATE FUNCTION fn_embarque_lineas(p_embarque_id uuid)
                RETURNS TABLE (id uuid, orden integer, producto_id uuid, producto_codigo text, nombre_cientifico text, nombre_comun text,
                               nombre_espanol text, talla text, cantidad integer, precio numeric, fraccion_caja numeric, empaque text,
                               nota text)
                LANGUAGE sql STABLE AS $$
                    SELECT l.id, l.orden, l.producto_id, p.codigo::text, p.nombre_cientifico::text, p.nombre_comun::text,
                           p.nombre_espanol::text,
                           coalesce(p.talla_etiqueta::text,
                                    CASE WHEN p.talla_min_cm IS NOT NULL
                                         THEN trim_scale(p.talla_min_cm)::text || coalesce(' - ' || trim_scale(p.talla_max_cm)::text, '') END),
                           l.cantidad, l.precio, l.fraccion_caja, l.empaque::text, l.nota::text
                    FROM embarque_lineas l JOIN productos p ON p.id = l.producto_id
                    WHERE l.embarque_id = p_embarque_id
                    ORDER BY l.orden;
                $$;

                CREATE FUNCTION fn_embarque_empaque(p_embarque_id uuid)
                RETURNS TABLE (id uuid, numero_caja integer, orden integer, producto_id uuid, producto_codigo text, nombre_cientifico text,
                               nombre_comun text, nombre_factura text, tipo_bolsa_id uuid, tipo_bolsa_nombre text, tipo_bolsa_porcion numeric,
                               tipo_bolsa_costo numeric, bolsas integer, peces_por_bolsa integer, precio numeric, doa integer,
                               doa_adicional integer, doa_final integer, nota text)
                LANGUAGE sql STABLE AS $$
                    SELECT f.id, f.numero_caja, f.orden, f.producto_id, p.codigo::text, p.nombre_cientifico::text, p.nombre_comun::text,
                           f.nombre_factura::text, f.tipo_bolsa_id, b.nombre::text, b.porcion, b.costo_cop, f.bolsas, f.peces_por_bolsa,
                           f.precio, f.doa, f.doa_adicional, f.doa_final, f.nota::text
                    FROM embarque_empaque f
                    JOIN productos p ON p.id = f.producto_id
                    LEFT JOIN tipos_bolsa b ON b.id = f.tipo_bolsa_id
                    WHERE f.embarque_id = p_embarque_id
                    ORDER BY f.numero_caja, f.orden;
                $$;

                CREATE FUNCTION fn_embarque_documentos(p_embarque_id uuid)
                RETURNS TABLE (id uuid, tipo text, numero text, archivo_clave text, generado_por text, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT id, tipo::text, numero::text, archivo_clave::text, generado_por::text, creado_en
                    FROM embarque_documentos WHERE embarque_id = p_embarque_id ORDER BY creado_en DESC;
                $$;

                CREATE FUNCTION fn_embarque_envios(p_embarque_id uuid)
                RETURNS TABLE (id uuid, tipo_documento text, para text, asunto text, proveedor text, exitoso boolean,
                               llego_al_destinatario boolean, error text, enviado_por text, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT id, tipo_documento::text, para::text, asunto::text, proveedor::text, exitoso, llego_al_destinatario,
                           error::text, enviado_por::text, creado_en
                    FROM embarque_envios WHERE embarque_id = p_embarque_id ORDER BY creado_en DESC;
                $$;
                """);
        }

        private static void CrearEscrituraEmbarques(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_embarque_crear(
                    p_id uuid, p_codigo text, p_venta_id uuid, p_pedido_id uuid, p_cliente_id uuid, p_comercial_id uuid,
                    p_lista_id uuid, p_descuento numeric, p_aerolinea text, p_awb text, p_formato text, p_tarifas jsonb,
                    p_cargos jsonb, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO embarques (id, codigo, venta_id, pedido_cliente_id, cliente_id, comercial_id, estado, lista_precios_id,
                                           descuento_peces, aerolinea, awb, formato_factura, anticipo_recibido, tarifas, cargos,
                                           cajas_estimadas, cajas_reales, creado_en)
                    VALUES (p_id, p_codigo, p_venta_id, p_pedido_id, p_cliente_id, p_comercial_id, 'borrador', p_lista_id, p_descuento,
                            p_aerolinea, p_awb, p_formato, 0, p_tarifas, p_cargos, '[]'::jsonb, '[]'::jsonb, p_fecha);
                $$;

                CREATE PROCEDURE sp_embarque_actualizar(
                    p_id uuid, p_lista_id uuid, p_descuento numeric, p_aerolinea text, p_ruta text, p_awb text, p_destino text,
                    p_salida timestamptz, p_llegada timestamptz, p_fecha_despacho date, p_formato text, p_awb_valor numeric,
                    p_anticipo numeric, p_tarifas jsonb, p_cargos jsonb, p_cajas_estimadas jsonb, p_cajas_reales jsonb, p_notas text,
                    p_pedido_id uuid, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE embarques SET lista_precios_id = p_lista_id, descuento_peces = p_descuento, aerolinea = p_aerolinea,
                           ruta = p_ruta, awb = p_awb, destino = p_destino, salida_vuelo = p_salida, llegada_vuelo = p_llegada,
                           fecha_despacho = p_fecha_despacho, formato_factura = p_formato, awb_valor = p_awb_valor,
                           anticipo_recibido = p_anticipo, tarifas = p_tarifas, cargos = p_cargos, cajas_estimadas = p_cajas_estimadas,
                           cajas_reales = p_cajas_reales, notas = p_notas, pedido_cliente_id = p_pedido_id, actualizado_en = p_fecha
                    WHERE id = p_id;
                $$;

                -- Líneas: se reemplazan completas (el orden de la lista es el de la orden de cargue).
                CREATE PROCEDURE sp_embarque_lineas_guardar(p_id uuid, p_lineas jsonb, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    DELETE FROM embarque_lineas WHERE embarque_id = p_id;
                    INSERT INTO embarque_lineas (id, embarque_id, orden, producto_id, cantidad, precio, fraccion_caja, empaque, nota)
                    SELECT (x->>'id')::uuid, p_id, (x->>'orden')::integer, (x->>'productoId')::uuid, (x->>'cantidad')::integer,
                           (x->>'precio')::numeric, (x->>'fraccionCaja')::numeric, x->>'empaque', x->>'nota'
                    FROM jsonb_array_elements(p_lineas) AS x;
                    UPDATE embarques SET actualizado_en = p_fecha WHERE id = p_id;
                END;
                $$;

                -- Empaque real: se reemplaza completo (antes de facturar; el DOA se registra después).
                CREATE PROCEDURE sp_embarque_empaque_guardar(p_id uuid, p_filas jsonb, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    DELETE FROM embarque_empaque WHERE embarque_id = p_id;
                    INSERT INTO embarque_empaque (id, embarque_id, numero_caja, orden, producto_id, nombre_factura, tipo_bolsa_id, bolsas,
                                                  peces_por_bolsa, precio, doa, doa_adicional, doa_final, nota)
                    SELECT (x->>'id')::uuid, p_id, (x->>'numeroCaja')::integer, (x->>'orden')::integer, (x->>'productoId')::uuid,
                           x->>'nombreFactura', (x->>'tipoBolsaId')::uuid, (x->>'bolsas')::integer, (x->>'pecesPorBolsa')::integer,
                           (x->>'precio')::numeric, 0, 0, 0, x->>'nota'
                    FROM jsonb_array_elements(p_filas) AS x;
                    UPDATE embarques SET actualizado_en = p_fecha WHERE id = p_id;
                END;
                $$;

                CREATE PROCEDURE sp_embarque_doa_guardar(p_id uuid, p_filas jsonb, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    UPDATE embarque_empaque f SET doa = (x->>'doa')::integer, doa_adicional = (x->>'doaAdicional')::integer,
                           doa_final = (x->>'doaFinal')::integer
                    FROM jsonb_array_elements(p_filas) AS x
                    WHERE f.id = (x->>'id')::uuid AND f.embarque_id = p_id;
                    UPDATE embarques SET actualizado_en = p_fecha WHERE id = p_id;
                END;
                $$;

                -- Cambio de estado "comparar y escribir": si otro lo cambió entretanto, JR001.
                CREATE PROCEDURE sp_embarque_cambiar_estado(p_id uuid, p_actual text, p_nuevo text, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    UPDATE embarques SET estado = p_nuevo, actualizado_en = p_fecha WHERE id = p_id AND estado = p_actual;
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'El embarque cambió de estado mientras tanto. Recarga la página.' USING ERRCODE = 'JR001';
                    END IF;
                END;
                $$;

                CREATE PROCEDURE sp_embarque_preinvoice(p_id uuid, p_numero text, p_fecha_doc date, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE embarques SET preinvoice_numero = p_numero, preinvoice_fecha = p_fecha_doc, actualizado_en = p_fecha
                    WHERE id = p_id AND preinvoice_numero IS NULL;
                $$;

                -- Emite la factura: asigna el siguiente número (FACT) y pasa a facturado, solo desde el estado esperado.
                CREATE FUNCTION fn_embarque_facturar(p_id uuid, p_estado_actual text, p_fecha_factura date, p_fecha timestamptz)
                RETURNS integer
                LANGUAGE plpgsql AS $$
                DECLARE v_numero integer;
                BEGIN
                    PERFORM 1 FROM embarques WHERE id = p_id AND estado = p_estado_actual AND factura_numero IS NULL FOR UPDATE;
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'El embarque cambió mientras tanto o ya está facturado. Recarga la página.' USING ERRCODE = 'JR001';
                    END IF;
                    v_numero := fn_siguiente_numero('FACT');
                    UPDATE embarques SET factura_numero = v_numero, factura_fecha = p_fecha_factura, estado = 'facturado',
                           actualizado_en = p_fecha
                    WHERE id = p_id;
                    RETURN v_numero;
                END;
                $$;

                CREATE PROCEDURE sp_embarque_nota_credito(p_id uuid, p_fecha_nota date, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE embarques SET nota_credito_fecha = p_fecha_nota, actualizado_en = p_fecha WHERE id = p_id;
                $$;

                -- Lleva al CRM lo que dice la factura: la venta (total USD, cajas, aerolínea, AWB) y su tracking (factura,
                -- AWB, cajas, FOB, flete y DOA de la nota crédito). Así la torre de vuelos y el dashboard no se re-digitan.
                CREATE PROCEDURE sp_embarque_sincronizar_crm(
                    p_id uuid, p_total numeric, p_cajas integer, p_fob numeric, p_flete numeric, p_doa numeric, p_nota_credito text,
                    p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                DECLARE e embarques%ROWTYPE;
                BEGIN
                    SELECT * INTO e FROM embarques WHERE id = p_id;
                    UPDATE ventas SET total = CASE WHEN moneda = 'USD' THEN p_total ELSE total END, total_cajas = p_cajas,
                           transportadora_aerolinea = coalesce(e.aerolinea, transportadora_aerolinea),
                           no_guia_awb = coalesce(e.awb, no_guia_awb), actualizado_en = p_fecha
                    WHERE id = e.venta_id;
                    UPDATE tracking_vuelos SET n_factura_invoice = e.factura_numero::text, no_awb = coalesce(e.awb, no_awb),
                           aerolinea = coalesce(e.aerolinea, aerolinea), fecha_despacho_vuelo = coalesce(e.fecha_despacho, fecha_despacho_vuelo),
                           cajas_reales = p_cajas, monto_fob_usd = p_fob, flete_aereo_usd = p_flete, doa_reportado_usd = p_doa,
                           nota_credito_no = p_nota_credito, actualizado_en = p_fecha
                    WHERE venta_id = e.venta_id;
                END;
                $$;

                CREATE PROCEDURE sp_documento_embarque_registrar(
                    p_id uuid, p_embarque_id uuid, p_tipo text, p_numero text, p_clave text, p_generado_por text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO embarque_documentos (id, embarque_id, tipo, numero, archivo_clave, generado_por, creado_en)
                    VALUES (p_id, p_embarque_id, p_tipo, p_numero, p_clave, p_generado_por, p_fecha);
                $$;

                CREATE PROCEDURE sp_envio_documento_registrar(
                    p_id uuid, p_embarque_id uuid, p_tipo text, p_para text, p_asunto text, p_proveedor text, p_exitoso boolean,
                    p_llego boolean, p_mensaje_id text, p_error text, p_enviado_por text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO embarque_envios (id, embarque_id, tipo_documento, para, asunto, proveedor, exitoso, llego_al_destinatario,
                                                 mensaje_id, error, enviado_por, creado_en)
                    VALUES (p_id, p_embarque_id, p_tipo, p_para, p_asunto, p_proveedor, p_exitoso, p_llego, p_mensaje_id, p_error,
                            p_enviado_por, p_fecha);
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var objeto in new[]
                     {
                         "PROCEDURE sp_envio_documento_registrar", "PROCEDURE sp_documento_embarque_registrar",
                         "PROCEDURE sp_embarque_sincronizar_crm", "PROCEDURE sp_embarque_nota_credito", "FUNCTION fn_embarque_facturar",
                         "PROCEDURE sp_embarque_preinvoice", "PROCEDURE sp_embarque_cambiar_estado", "PROCEDURE sp_embarque_doa_guardar",
                         "PROCEDURE sp_embarque_empaque_guardar", "PROCEDURE sp_embarque_lineas_guardar", "PROCEDURE sp_embarque_actualizar",
                         "PROCEDURE sp_embarque_crear", "FUNCTION fn_embarque_envios", "FUNCTION fn_embarque_documentos",
                         "FUNCTION fn_embarque_empaque", "FUNCTION fn_embarque_lineas", "FUNCTION fn_embarque_id_por_venta",
                         "FUNCTION fn_embarque_por_id", "FUNCTION fn_embarques_listar", "PROCEDURE sp_cliente_exportacion_guardar",
                         "FUNCTION fn_cliente_exportacion", "PROCEDURE sp_cargo_exportacion_actualizar", "FUNCTION fn_cargos_exportacion_listar",
                         "PROCEDURE sp_tipo_bolsa_guardar", "FUNCTION fn_tipos_bolsa_listar", "PROCEDURE sp_tipo_caja_guardar",
                         "FUNCTION fn_tipos_caja_listar", "PROCEDURE sp_tarifas_flete_actualizar", "FUNCTION fn_tarifas_flete",
                         "PROCEDURE sp_datos_empresa_logo", "PROCEDURE sp_datos_empresa_actualizar", "FUNCTION fn_datos_empresa",
                         "FUNCTION fn_siguiente_numero",
                     })
            {
                var partes = objeto.Split(' ');
                migrationBuilder.Sql($"DROP {partes[0]} IF EXISTS {partes[1]};");
            }

            migrationBuilder.Sql(
                """
                DELETE FROM consecutivos WHERE prefijo IN ('EXP', 'FACT');
                DELETE FROM cargos_exportacion; DELETE FROM tipos_bolsa; DELETE FROM tipos_caja;
                DELETE FROM tarifas_flete; DELETE FROM datos_empresa;
                """);
        }
    }
}
