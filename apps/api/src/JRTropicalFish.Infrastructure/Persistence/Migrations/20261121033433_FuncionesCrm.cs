using JRTropicalFish.Infrastructure.Persistence.Sembrado;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Funciones y procedimientos del CRM, sembrado de consecutivos, parámetros comerciales y permisos.
    ///
    /// <para>
    /// <b>Orden de columnas</b>: cada <c>RETURNS TABLE</c> coincide con el record de
    /// <c>Application/Crm/CrmModels.cs</c> que la lee (Dapper construye por posición).
    /// </para>
    ///
    /// <para>
    /// <b>Concurrencia</b>: los cambios de etapa, ganar una oportunidad y cambiar el estado de una
    /// tarea son "comparar y escribir": el <c>UPDATE</c> exige que la etapa siga siendo la que el
    /// usuario vio. Si otro la movió antes, el procedimiento lanza <c>SQLSTATE JR001</c> y el
    /// repositorio lo traduce a un conflicto (409). Así dos clics simultáneos nunca hacen retroceder
    /// una oportunidad ni crean dos ventas.
    /// </para>
    /// </summary>
    public partial class FuncionesCrm : Migration
    {
        /// <summary>Error de concurrencia que los repositorios traducen a 409.</summary>
        public const string SqlStateConcurrencia = "JR001";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SembradoPermisos.Aplicar(migrationBuilder);

            CrearConsecutivos(migrationBuilder);
            CrearFuncionesClientes(migrationBuilder);
            CrearFuncionesOportunidades(migrationBuilder);
            CrearFuncionesVentas(migrationBuilder);
            CrearFuncionesTracking(migrationBuilder);
            CrearFuncionesTareas(migrationBuilder);
            CrearFuncionesMetas(migrationBuilder);
        }

        private static void CrearConsecutivos(MigrationBuilder migrationBuilder)
        {
            // Los arranques siguen la numeración del Excel de control comercial: OPT-101, VTA-801, TRK-501.
            migrationBuilder.Sql(
                """
                INSERT INTO consecutivos (prefijo, ultimo) VALUES
                    ('CLI', 0), ('OPT', 100), ('VTA', 800), ('TRK', 500), ('TAR', 0)
                ON CONFLICT (prefijo) DO NOTHING;
                """);

            // El UPDATE bloquea la fila del prefijo: dos peticiones simultáneas nunca reciben el mismo número.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_siguiente_consecutivo(p_prefijo text)
                RETURNS text
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_numero integer;
                BEGIN
                    INSERT INTO consecutivos (prefijo, ultimo) VALUES (p_prefijo, 1)
                    ON CONFLICT (prefijo) DO UPDATE SET ultimo = consecutivos.ultimo + 1
                    RETURNING ultimo INTO v_numero;
                    RETURN p_prefijo || '-' || lpad(v_numero::text, 3, '0');
                END;
                $$;
                """);

            // Al importar un código explícito (CLI-047) el consecutivo sube para no repetirlo después.
            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_consecutivo_ajustar(p_prefijo text, p_numero integer)
                LANGUAGE sql AS $$
                    INSERT INTO consecutivos (prefijo, ultimo) VALUES (p_prefijo, p_numero)
                    ON CONFLICT (prefijo) DO UPDATE SET ultimo = greatest(consecutivos.ultimo, EXCLUDED.ultimo);
                $$;
                """);
        }

        private static void CrearFuncionesClientes(MigrationBuilder migrationBuilder)
        {
            const string columnas =
                """
                RETURNS TABLE (id uuid, codigo text, nombre_empresa text, contacto_principal text, email text,
                               telefono_whatsapp text, pais text, ciudad text, aeropuerto_iata text,
                               tipo_cliente text, licencia_importacion text, estado text, notas text,
                               creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT c.id, c.codigo::text, c.nombre_empresa::text, c.contacto_principal::text, c.email::text,
                           c.telefono_whatsapp::text, c.pais::text, c.ciudad::text, c.aeropuerto_iata::text,
                           c.tipo_cliente::text, c.licencia_importacion::text, c.estado::text, c.notas::text,
                           c.creado_en
                    FROM clientes c
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_clientes_listar(p_busqueda text)
                {columnas}
                    WHERE c.eliminado_en IS NULL
                      AND (p_busqueda IS NULL OR p_busqueda = ''
                           OR c.nombre_empresa ILIKE '%' || p_busqueda || '%'
                           OR c.codigo ILIKE '%' || p_busqueda || '%'
                           OR c.contacto_principal ILIKE '%' || p_busqueda || '%'
                           OR c.pais ILIKE '%' || p_busqueda || '%'
                           OR c.ciudad ILIKE '%' || p_busqueda || '%'
                           OR c.aeropuerto_iata ILIKE '%' || p_busqueda || '%')
                    ORDER BY c.nombre_empresa;
                $$;
                """);

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_cliente_por_id(p_id uuid)
                {columnas}
                    WHERE c.id = p_id AND c.eliminado_en IS NULL;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_cliente_valor_existe(p_campo text, p_valor text, p_excluir_id uuid)
                RETURNS boolean
                LANGUAGE sql STABLE AS $$
                    SELECT EXISTS (
                        SELECT 1 FROM clientes
                        WHERE eliminado_en IS NULL
                          AND (p_excluir_id IS NULL OR id <> p_excluir_id)
                          AND CASE p_campo
                                  WHEN 'codigo' THEN upper(codigo) = upper(btrim(p_valor))
                                  WHEN 'nombre' THEN lower(nombre_empresa) = lower(btrim(p_valor))
                              END);
                $$;
                """);

            // Un cliente con oportunidades, ventas o tareas no se elimina: se marca Inactivo.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_cliente_tiene_movimientos(p_id uuid)
                RETURNS boolean
                LANGUAGE sql STABLE AS $$
                    SELECT EXISTS (SELECT 1 FROM oportunidades WHERE cliente_id = p_id AND eliminado_en IS NULL)
                        OR EXISTS (SELECT 1 FROM ventas WHERE cliente_id = p_id)
                        OR EXISTS (SELECT 1 FROM tareas WHERE cliente_id = p_id AND eliminado_en IS NULL);
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_cliente_crear(
                    p_id uuid, p_codigo text, p_nombre_empresa text, p_contacto_principal text, p_email text,
                    p_telefono_whatsapp text, p_pais text, p_ciudad text, p_aeropuerto_iata text, p_tipo_cliente text,
                    p_licencia_importacion text, p_estado text, p_notas text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO clientes (id, codigo, nombre_empresa, contacto_principal, email, telefono_whatsapp,
                                          pais, ciudad, aeropuerto_iata, tipo_cliente, licencia_importacion, estado,
                                          notas, creado_en)
                    VALUES (p_id, p_codigo, p_nombre_empresa, p_contacto_principal, lower(p_email), p_telefono_whatsapp,
                            p_pais, p_ciudad, upper(p_aeropuerto_iata), p_tipo_cliente, p_licencia_importacion, p_estado,
                            p_notas, p_fecha);
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_cliente_actualizar(
                    p_id uuid, p_nombre_empresa text, p_contacto_principal text, p_email text,
                    p_telefono_whatsapp text, p_pais text, p_ciudad text, p_aeropuerto_iata text, p_tipo_cliente text,
                    p_licencia_importacion text, p_estado text, p_notas text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE clientes
                    SET nombre_empresa = p_nombre_empresa, contacto_principal = p_contacto_principal,
                        email = lower(p_email), telefono_whatsapp = p_telefono_whatsapp, pais = p_pais,
                        ciudad = p_ciudad, aeropuerto_iata = upper(p_aeropuerto_iata), tipo_cliente = p_tipo_cliente,
                        licencia_importacion = p_licencia_importacion, estado = p_estado, notas = p_notas,
                        actualizado_en = p_fecha
                    WHERE id = p_id AND eliminado_en IS NULL;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_cliente_eliminar(p_id uuid, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE clientes SET eliminado_en = p_fecha WHERE id = p_id AND eliminado_en IS NULL;
                $$;
                """);

            // Comerciales: usuarios activos cuyo perfil (activo) tiene crm:vender.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_comerciales_listar()
                RETURNS TABLE (id uuid, nombre text, email text)
                LANGUAGE sql STABLE AS $$
                    SELECT u.id, u.nombre::text, u.email::text
                    FROM usuarios u
                    JOIN perfiles pf ON pf.id = u.perfil_id AND pf.activo
                    JOIN perfil_permisos pp ON pp.perfil_id = pf.id
                    JOIN permisos pm ON pm.id = pp.permiso_id AND pm.codigo = 'crm:vender'
                    WHERE u.activo
                    ORDER BY u.nombre;
                $$;
                """);
        }

        private static void CrearFuncionesOportunidades(MigrationBuilder migrationBuilder)
        {
            // Los usuarios y clientes se unen sin filtrar estado: la oportunidad conserva sus nombres.
            const string columnas =
                """
                RETURNS TABLE (id uuid, codigo text, cliente_id uuid, cliente_codigo text, cliente_nombre text,
                               tipo_venta text, comercial_id uuid, comercial_nombre text, fecha_contacto_inicial date,
                               etapa text, moneda text, monto_estimado numeric, cajas_estimadas integer,
                               fecha_pago_confirmado date, motivo_perdida text, notas_seguimiento text,
                               etapa_cambiada_en timestamptz, venta_id uuid, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT o.id, o.codigo::text, o.cliente_id, c.codigo::text, c.nombre_empresa::text,
                           o.tipo_venta::text, o.comercial_id, u.nombre::text, o.fecha_contacto_inicial,
                           o.etapa::text, o.moneda::text, o.monto_estimado, o.cajas_estimadas,
                           o.fecha_pago_confirmado, o.motivo_perdida::text, o.notas_seguimiento::text,
                           o.etapa_cambiada_en, v.id, o.creado_en
                    FROM oportunidades o
                    JOIN clientes c ON c.id = o.cliente_id
                    JOIN usuarios u ON u.id = o.comercial_id
                    LEFT JOIN ventas v ON v.oportunidad_id = o.id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_oportunidades_listar(p_comercial_id uuid, p_cliente_id uuid)
                {columnas}
                    WHERE o.eliminado_en IS NULL
                      AND (p_comercial_id IS NULL OR o.comercial_id = p_comercial_id)
                      AND (p_cliente_id IS NULL OR o.cliente_id = p_cliente_id)
                    ORDER BY o.etapa_cambiada_en DESC;
                $$;
                """);

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_oportunidad_por_id(p_id uuid)
                {columnas}
                    WHERE o.id = p_id AND o.eliminado_en IS NULL;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_oportunidad_crear(
                    p_id uuid, p_codigo text, p_cliente_id uuid, p_tipo_venta text, p_comercial_id uuid,
                    p_fecha_contacto_inicial date, p_etapa text, p_moneda text, p_monto_estimado numeric,
                    p_cajas_estimadas integer, p_notas_seguimiento text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO oportunidades (id, codigo, cliente_id, tipo_venta, comercial_id, fecha_contacto_inicial,
                                               etapa, moneda, monto_estimado, cajas_estimadas, notas_seguimiento,
                                               etapa_cambiada_en, creado_en)
                    VALUES (p_id, p_codigo, p_cliente_id, p_tipo_venta, p_comercial_id, p_fecha_contacto_inicial,
                            p_etapa, p_moneda, p_monto_estimado, p_cajas_estimadas, p_notas_seguimiento,
                            p_fecha, p_fecha);
                $$;
                """);

            // La etapa no se toca aquí: solo cambia por sp_oportunidad_cambiar_etapa / sp_oportunidad_ganar.
            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_oportunidad_actualizar(
                    p_id uuid, p_cliente_id uuid, p_tipo_venta text, p_comercial_id uuid,
                    p_fecha_contacto_inicial date, p_moneda text, p_monto_estimado numeric,
                    p_cajas_estimadas integer, p_notas_seguimiento text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE oportunidades
                    SET cliente_id = p_cliente_id, tipo_venta = p_tipo_venta, comercial_id = p_comercial_id,
                        fecha_contacto_inicial = p_fecha_contacto_inicial, moneda = p_moneda,
                        monto_estimado = p_monto_estimado, cajas_estimadas = p_cajas_estimadas,
                        notas_seguimiento = p_notas_seguimiento, actualizado_en = p_fecha
                    WHERE id = p_id AND eliminado_en IS NULL;
                $$;
                """);

            // Comparar y escribir: solo cambia si la etapa sigue siendo la que vio el usuario. La regla
            // de "solo hacia adelante" la valida ReglasPipeline en C#; el CHECK de la tabla exige el motivo.
            migrationBuilder.Sql(
                $"""
                CREATE PROCEDURE sp_oportunidad_cambiar_etapa(
                    p_id uuid, p_etapa_esperada text, p_etapa text, p_motivo_perdida text, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    UPDATE oportunidades
                    SET etapa = p_etapa,
                        motivo_perdida = CASE WHEN p_etapa = 'perdida' THEN p_motivo_perdida END,
                        etapa_cambiada_en = p_fecha, actualizado_en = p_fecha
                    WHERE id = p_id AND etapa = p_etapa_esperada AND eliminado_en IS NULL;

                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'La oportunidad cambió de etapa mientras tanto.' USING ERRCODE = '{SqlStateConcurrencia}';
                    END IF;
                END;
                $$;
                """);

            // Ganar = cerrar la oportunidad + crear la venta + (exportación) crear el tracking, en una
            // sola transacción. La venta hereda cliente, comercial, tipo, moneda, monto y cajas.
            migrationBuilder.Sql(
                $"""
                CREATE PROCEDURE sp_oportunidad_ganar(
                    p_id uuid, p_etapa_esperada text, p_fecha_pago_confirmado date,
                    p_venta_id uuid, p_venta_codigo text, p_origen_canal text, p_trm numeric, p_estado_pago text,
                    p_tracking_id uuid, p_tracking_codigo text, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                DECLARE
                    o oportunidades%ROWTYPE;
                BEGIN
                    UPDATE oportunidades
                    SET etapa = 'ganada', fecha_pago_confirmado = p_fecha_pago_confirmado, motivo_perdida = NULL,
                        etapa_cambiada_en = p_fecha, actualizado_en = p_fecha
                    WHERE id = p_id AND etapa = p_etapa_esperada AND eliminado_en IS NULL
                    RETURNING * INTO o;

                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'La oportunidad cambió de etapa mientras tanto.' USING ERRCODE = '{SqlStateConcurrencia}';
                    END IF;

                    INSERT INTO ventas (id, codigo, oportunidad_id, cliente_id, comercial_id, origen_canal, fecha_pedido,
                                        tipo_venta, moneda, total, trm, total_cajas, estado_pago, estado_preparacion,
                                        creado_en)
                    VALUES (p_venta_id, p_venta_codigo, o.id, o.cliente_id, o.comercial_id, p_origen_canal,
                            p_fecha_pago_confirmado, o.tipo_venta, o.moneda, o.monto_estimado,
                            CASE WHEN o.moneda = 'USD' THEN p_trm END, o.cajas_estimadas, p_estado_pago,
                            'pendiente_picking', p_fecha);

                    IF p_tracking_id IS NOT NULL THEN
                        INSERT INTO tracking_vuelos (id, codigo, venta_id, oportunidad_id, cliente_id, cajas_reales, fase,
                                                     monto_fob_usd, flete_aereo_usd, doa_reportado_usd, creado_en)
                        VALUES (p_tracking_id, p_tracking_codigo, p_venta_id, o.id, o.cliente_id, o.cajas_estimadas,
                                'cuarentena', CASE WHEN o.moneda = 'USD' THEN o.monto_estimado ELSE 0 END, 0, 0, p_fecha);
                    END IF;
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_oportunidad_eliminar(p_id uuid, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE oportunidades SET eliminado_en = p_fecha WHERE id = p_id AND eliminado_en IS NULL;
                $$;
                """);
        }

        private static void CrearFuncionesVentas(MigrationBuilder migrationBuilder)
        {
            const string columnas =
                """
                RETURNS TABLE (id uuid, codigo text, oportunidad_id uuid, oportunidad_codigo text, cliente_id uuid,
                               cliente_codigo text, cliente_nombre text, comercial_id uuid, comercial_nombre text,
                               origen_canal text, fecha_pedido date, tipo_venta text, moneda text, total numeric,
                               trm numeric, total_cop numeric, total_cajas integer, transportadora_aerolinea text,
                               no_guia_awb text, estado_pago text, estado_preparacion text, ciudad_entrega text,
                               direccion_entrega text, notas text, tracking_id uuid, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT v.id, v.codigo::text, v.oportunidad_id, o.codigo::text, v.cliente_id,
                           c.codigo::text, c.nombre_empresa::text, v.comercial_id, u.nombre::text,
                           v.origen_canal::text, v.fecha_pedido, v.tipo_venta::text, v.moneda::text, v.total,
                           v.trm, v.total_cop, v.total_cajas, v.transportadora_aerolinea::text,
                           v.no_guia_awb::text, v.estado_pago::text, v.estado_preparacion::text, v.ciudad_entrega::text,
                           v.direccion_entrega::text, v.notas::text, t.id, v.creado_en
                    FROM ventas v
                    JOIN clientes c ON c.id = v.cliente_id
                    JOIN usuarios u ON u.id = v.comercial_id
                    LEFT JOIN oportunidades o ON o.id = v.oportunidad_id
                    LEFT JOIN tracking_vuelos t ON t.venta_id = v.id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_ventas_listar(p_comercial_id uuid, p_cliente_id uuid, p_desde date, p_hasta date)
                {columnas}
                    WHERE (p_comercial_id IS NULL OR v.comercial_id = p_comercial_id)
                      AND (p_cliente_id IS NULL OR v.cliente_id = p_cliente_id)
                      AND (p_desde IS NULL OR v.fecha_pedido >= p_desde)
                      AND (p_hasta IS NULL OR v.fecha_pedido <= p_hasta)
                    ORDER BY v.fecha_pedido DESC, v.codigo DESC;
                $$;
                """);

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_venta_por_id(p_id uuid)
                {columnas}
                    WHERE v.id = p_id;
                $$;
                """);

            // Venta directa (sin oportunidad). Si es exportación nace también su tracking.
            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_venta_crear(
                    p_id uuid, p_codigo text, p_cliente_id uuid, p_comercial_id uuid, p_origen_canal text,
                    p_fecha_pedido date, p_tipo_venta text, p_moneda text, p_total numeric, p_trm numeric,
                    p_total_cajas integer, p_transportadora_aerolinea text, p_no_guia_awb text, p_estado_pago text,
                    p_estado_preparacion text, p_ciudad_entrega text, p_direccion_entrega text, p_notas text,
                    p_tracking_id uuid, p_tracking_codigo text, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    INSERT INTO ventas (id, codigo, cliente_id, comercial_id, origen_canal, fecha_pedido, tipo_venta,
                                        moneda, total, trm, total_cajas, transportadora_aerolinea, no_guia_awb,
                                        estado_pago, estado_preparacion, ciudad_entrega, direccion_entrega, notas,
                                        creado_en)
                    VALUES (p_id, p_codigo, p_cliente_id, p_comercial_id, p_origen_canal, p_fecha_pedido, p_tipo_venta,
                            p_moneda, p_total, p_trm, p_total_cajas, p_transportadora_aerolinea, p_no_guia_awb,
                            p_estado_pago, p_estado_preparacion, p_ciudad_entrega, p_direccion_entrega, p_notas,
                            p_fecha);

                    IF p_tracking_id IS NOT NULL THEN
                        INSERT INTO tracking_vuelos (id, codigo, venta_id, cliente_id, no_awb, aerolinea, cajas_reales,
                                                     fase, monto_fob_usd, flete_aereo_usd, doa_reportado_usd, creado_en)
                        VALUES (p_tracking_id, p_tracking_codigo, p_id, p_cliente_id, p_no_guia_awb,
                                p_transportadora_aerolinea, p_total_cajas, 'cuarentena',
                                CASE WHEN p_moneda = 'USD' THEN p_total ELSE 0 END, 0, 0, p_fecha);
                    END IF;
                END;
                $$;
                """);

            // Cliente, comercial y tipo de venta no cambian: de ellos dependen la oportunidad y el tracking.
            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_venta_actualizar(
                    p_id uuid, p_origen_canal text, p_fecha_pedido date, p_moneda text, p_total numeric, p_trm numeric,
                    p_total_cajas integer, p_transportadora_aerolinea text, p_no_guia_awb text, p_estado_pago text,
                    p_estado_preparacion text, p_ciudad_entrega text, p_direccion_entrega text, p_notas text,
                    p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE ventas
                    SET origen_canal = p_origen_canal, fecha_pedido = p_fecha_pedido, moneda = p_moneda,
                        total = p_total, trm = p_trm, total_cajas = p_total_cajas,
                        transportadora_aerolinea = p_transportadora_aerolinea, no_guia_awb = p_no_guia_awb,
                        estado_pago = p_estado_pago, estado_preparacion = p_estado_preparacion,
                        ciudad_entrega = p_ciudad_entrega, direccion_entrega = p_direccion_entrega, notas = p_notas,
                        actualizado_en = p_fecha
                    WHERE id = p_id;
                $$;
                """);
        }

        private static void CrearFuncionesTracking(MigrationBuilder migrationBuilder)
        {
            const string columnas =
                """
                RETURNS TABLE (id uuid, codigo text, venta_id uuid, venta_codigo text, oportunidad_id uuid,
                               oportunidad_codigo text, cliente_id uuid, cliente_codigo text, cliente_nombre text,
                               comercial_id uuid, comercial_nombre text, n_factura_invoice text, no_awb text,
                               aerolinea text, fecha_despacho_vuelo date, cajas_reales integer, fase text,
                               monto_fob_usd numeric, flete_aereo_usd numeric, doa_reportado_usd numeric,
                               porcentaje_doa numeric, nota_credito_no text, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT t.id, t.codigo::text, t.venta_id, v.codigo::text, t.oportunidad_id,
                           o.codigo::text, t.cliente_id, c.codigo::text, c.nombre_empresa::text,
                           v.comercial_id, u.nombre::text, t.n_factura_invoice::text, t.no_awb::text,
                           t.aerolinea::text, t.fecha_despacho_vuelo, t.cajas_reales, t.fase::text,
                           t.monto_fob_usd, t.flete_aereo_usd, t.doa_reportado_usd,
                           t.porcentaje_doa, t.nota_credito_no::text, t.creado_en
                    FROM tracking_vuelos t
                    JOIN ventas v ON v.id = t.venta_id
                    JOIN clientes c ON c.id = t.cliente_id
                    JOIN usuarios u ON u.id = v.comercial_id
                    LEFT JOIN oportunidades o ON o.id = t.oportunidad_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_tracking_listar(p_comercial_id uuid, p_cliente_id uuid)
                {columnas}
                    WHERE (p_comercial_id IS NULL OR v.comercial_id = p_comercial_id)
                      AND (p_cliente_id IS NULL OR t.cliente_id = p_cliente_id)
                    ORDER BY coalesce(t.fecha_despacho_vuelo, v.fecha_pedido) DESC, t.codigo DESC;
                $$;
                """);

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_tracking_por_id(p_id uuid)
                {columnas}
                    WHERE t.id = p_id;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_tracking_actualizar(
                    p_id uuid, p_n_factura_invoice text, p_no_awb text, p_aerolinea text, p_fecha_despacho_vuelo date,
                    p_cajas_reales integer, p_fase text, p_monto_fob_usd numeric, p_flete_aereo_usd numeric,
                    p_doa_reportado_usd numeric, p_nota_credito_no text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE tracking_vuelos
                    SET n_factura_invoice = p_n_factura_invoice, no_awb = p_no_awb, aerolinea = p_aerolinea,
                        fecha_despacho_vuelo = p_fecha_despacho_vuelo, cajas_reales = p_cajas_reales, fase = p_fase,
                        monto_fob_usd = p_monto_fob_usd, flete_aereo_usd = p_flete_aereo_usd,
                        doa_reportado_usd = p_doa_reportado_usd, nota_credito_no = p_nota_credito_no,
                        actualizado_en = p_fecha
                    WHERE id = p_id;
                $$;
                """);

            migrationBuilder.Sql(
                $"""
                CREATE PROCEDURE sp_tracking_cambiar_fase(p_id uuid, p_fase_esperada text, p_fase text, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    UPDATE tracking_vuelos SET fase = p_fase, actualizado_en = p_fecha
                    WHERE id = p_id AND fase = p_fase_esperada;

                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'El despacho cambió de fase mientras tanto.' USING ERRCODE = '{SqlStateConcurrencia}';
                    END IF;
                END;
                $$;
                """);
        }

        private static void CrearFuncionesTareas(MigrationBuilder migrationBuilder)
        {
            // dias_restantes contra el "hoy" de negocio (Bogotá), que llega como parámetro desde IReloj.
            const string columnas =
                """
                RETURNS TABLE (id uuid, codigo text, cliente_id uuid, cliente_codigo text, cliente_nombre text,
                               oportunidad_id uuid, oportunidad_codigo text, accion text, tipo_accion text, estado text,
                               prioridad text, comercial_id uuid, comercial_nombre text, fecha_asignacion date,
                               fecha_vencimiento date, dias_restantes integer, notas text, completada_en timestamptz,
                               creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT t.id, t.codigo::text, t.cliente_id, c.codigo::text, c.nombre_empresa::text,
                           t.oportunidad_id, o.codigo::text, t.accion::text, t.tipo_accion::text, t.estado::text,
                           t.prioridad::text, t.comercial_id, u.nombre::text, t.fecha_asignacion,
                           t.fecha_vencimiento, (t.fecha_vencimiento - p_hoy)::integer, t.notas::text, t.completada_en,
                           t.creado_en
                    FROM tareas t
                    JOIN clientes c ON c.id = t.cliente_id
                    JOIN usuarios u ON u.id = t.comercial_id
                    LEFT JOIN oportunidades o ON o.id = t.oportunidad_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_tareas_listar(p_hoy date, p_comercial_id uuid, p_cliente_id uuid)
                {columnas}
                    WHERE t.eliminado_en IS NULL
                      AND (p_comercial_id IS NULL OR t.comercial_id = p_comercial_id)
                      AND (p_cliente_id IS NULL OR t.cliente_id = p_cliente_id)
                    ORDER BY t.fecha_vencimiento, CASE t.prioridad WHEN 'alta' THEN 0 WHEN 'media' THEN 1 ELSE 2 END;
                $$;
                """);

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_tarea_por_id(p_id uuid, p_hoy date)
                {columnas}
                    WHERE t.id = p_id AND t.eliminado_en IS NULL;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_tarea_crear(
                    p_id uuid, p_codigo text, p_cliente_id uuid, p_oportunidad_id uuid, p_accion text, p_tipo_accion text,
                    p_estado text, p_prioridad text, p_comercial_id uuid, p_fecha_asignacion date,
                    p_fecha_vencimiento date, p_notas text, p_completada_en timestamptz, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO tareas (id, codigo, cliente_id, oportunidad_id, accion, tipo_accion, estado, prioridad,
                                        comercial_id, fecha_asignacion, fecha_vencimiento, notas, completada_en, creado_en)
                    VALUES (p_id, p_codigo, p_cliente_id, p_oportunidad_id, p_accion, p_tipo_accion, p_estado, p_prioridad,
                            p_comercial_id, p_fecha_asignacion, p_fecha_vencimiento, p_notas, p_completada_en, p_fecha);
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_tarea_actualizar(
                    p_id uuid, p_cliente_id uuid, p_oportunidad_id uuid, p_accion text, p_tipo_accion text,
                    p_estado text, p_prioridad text, p_comercial_id uuid, p_fecha_asignacion date,
                    p_fecha_vencimiento date, p_notas text, p_completada_en timestamptz, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE tareas
                    SET cliente_id = p_cliente_id, oportunidad_id = p_oportunidad_id, accion = p_accion,
                        tipo_accion = p_tipo_accion, estado = p_estado, prioridad = p_prioridad,
                        comercial_id = p_comercial_id, fecha_asignacion = p_fecha_asignacion,
                        fecha_vencimiento = p_fecha_vencimiento, notas = p_notas, completada_en = p_completada_en,
                        actualizado_en = p_fecha
                    WHERE id = p_id AND eliminado_en IS NULL;
                $$;
                """);

            migrationBuilder.Sql(
                $"""
                CREATE PROCEDURE sp_tarea_cambiar_estado(
                    p_id uuid, p_estado_esperado text, p_estado text, p_completada_en timestamptz, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    UPDATE tareas SET estado = p_estado, completada_en = p_completada_en, actualizado_en = p_fecha
                    WHERE id = p_id AND estado = p_estado_esperado AND eliminado_en IS NULL;

                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'La tarea cambió de estado mientras tanto.' USING ERRCODE = '{SqlStateConcurrencia}';
                    END IF;
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_tarea_eliminar(p_id uuid, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE tareas SET eliminado_en = p_fecha WHERE id = p_id AND eliminado_en IS NULL;
                $$;
                """);
        }

        private static void CrearFuncionesMetas(MigrationBuilder migrationBuilder)
        {
            // Valores iniciales del negocio: meta $180M/mes, $60M por comercial, equilibrio en 7 despachos,
            // pool de comisión del 10%, alerta DOA sobre 3% y 2 clientes nuevos al mes.
            migrationBuilder.Sql(
                """
                INSERT INTO parametros_comerciales (id, meta_empresa_cop, meta_comercial_cop, punto_equilibrio_despachos,
                                                    porcentaje_comision, umbral_doa, meta_clientes_nuevos)
                VALUES (1, 180000000, 60000000, 7, 10, 3, 2)
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_parametros_comerciales()
                RETURNS TABLE (meta_empresa_cop numeric, meta_comercial_cop numeric, punto_equilibrio_despachos integer,
                               porcentaje_comision numeric, umbral_doa numeric, meta_clientes_nuevos integer,
                               actualizado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT meta_empresa_cop, meta_comercial_cop, punto_equilibrio_despachos, porcentaje_comision,
                           umbral_doa, meta_clientes_nuevos, actualizado_en
                    FROM parametros_comerciales WHERE id = 1;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_parametros_comerciales_actualizar(
                    p_meta_empresa_cop numeric, p_meta_comercial_cop numeric, p_punto_equilibrio_despachos integer,
                    p_porcentaje_comision numeric, p_umbral_doa numeric, p_meta_clientes_nuevos integer,
                    p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE parametros_comerciales
                    SET meta_empresa_cop = p_meta_empresa_cop, meta_comercial_cop = p_meta_comercial_cop,
                        punto_equilibrio_despachos = p_punto_equilibrio_despachos,
                        porcentaje_comision = p_porcentaje_comision, umbral_doa = p_umbral_doa,
                        meta_clientes_nuevos = p_meta_clientes_nuevos, actualizado_en = p_fecha
                    WHERE id = 1;
                $$;
                """);

            // Metas de un mes: todos los comerciales vigentes (con o sin meta) más quien tenga meta
            // ese mes aunque ya no sea comercial.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_metas_mes(p_mes date)
                RETURNS TABLE (comercial_id uuid, comercial_nombre text, meta_id uuid, meta_cop numeric)
                LANGUAGE sql STABLE AS $$
                    SELECT u.id, u.nombre::text, m.id, m.meta_cop
                    FROM usuarios u
                    LEFT JOIN metas_comerciales m ON m.comercial_id = u.id AND m.mes = p_mes
                    WHERE m.id IS NOT NULL OR u.id IN (SELECT id FROM fn_comerciales_listar())
                    ORDER BY u.nombre;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_meta_guardar(p_id uuid, p_mes date, p_comercial_id uuid, p_meta_cop numeric,
                                                 p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO metas_comerciales (id, mes, comercial_id, meta_cop, creado_en)
                    VALUES (p_id, p_mes, p_comercial_id, p_meta_cop, p_fecha)
                    ON CONFLICT (mes, comercial_id) DO UPDATE SET meta_cop = EXCLUDED.meta_cop, actualizado_en = p_fecha;
                $$;
                """);

            // --- Dashboard ---

            // Ventas del periodo por comercial. El DOA se pasa a COP con la TRM de su venta.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_crm_ventas_por_comercial(p_desde date, p_hasta date)
                RETURNS TABLE (comercial_id uuid, comercial_nombre text, ventas integer, ventas_cop numeric, doa_cop numeric)
                LANGUAGE sql STABLE AS $$
                    SELECT v.comercial_id, u.nombre::text, count(*)::integer, sum(v.total_cop),
                           coalesce(sum(t.doa_reportado_usd * coalesce(v.trm, 1)), 0)
                    FROM ventas v
                    JOIN usuarios u ON u.id = v.comercial_id
                    LEFT JOIN tracking_vuelos t ON t.venta_id = v.id
                    WHERE v.fecha_pedido BETWEEN p_desde AND p_hasta
                    GROUP BY v.comercial_id, u.nombre
                    ORDER BY u.nombre;
                $$;
                """);

            // Embudo: las etapas activas como están hoy; ganadas y perdidas solo las cerradas en el periodo.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_crm_embudo(p_desde timestamptz, p_hasta timestamptz, p_comercial_id uuid)
                RETURNS TABLE (etapa text, cantidad integer, monto_usd numeric, monto_cop numeric)
                LANGUAGE sql STABLE AS $$
                    SELECT o.etapa::text, count(*)::integer,
                           coalesce(sum(o.monto_estimado) FILTER (WHERE o.moneda = 'USD'), 0),
                           coalesce(sum(o.monto_estimado) FILTER (WHERE o.moneda = 'COP'), 0)
                    FROM oportunidades o
                    WHERE o.eliminado_en IS NULL
                      AND (p_comercial_id IS NULL OR o.comercial_id = p_comercial_id)
                      AND (o.etapa NOT IN ('ganada', 'perdida')
                           OR (o.etapa_cambiada_en >= p_desde AND o.etapa_cambiada_en < p_hasta))
                    GROUP BY o.etapa;
                $$;
                """);

            // Despachos = ventas de exportación del periodo. Cliente nuevo = su primera venta cae en el periodo.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_crm_resumen_periodo(p_desde date, p_hasta date)
                RETURNS TABLE (despachos integer, clientes_nuevos integer)
                LANGUAGE sql STABLE AS $$
                    SELECT
                        (SELECT count(*)::integer FROM ventas
                         WHERE tipo_venta = 'exportacion' AND fecha_pedido BETWEEN p_desde AND p_hasta),
                        (SELECT count(*)::integer FROM (
                            SELECT cliente_id FROM ventas GROUP BY cliente_id
                            HAVING min(fecha_pedido) BETWEEN p_desde AND p_hasta) primeras);
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nombres únicos (sin sobrecargas): DROP sin firma basta.
            foreach (var objeto in new[]
                     {
                         "FUNCTION fn_crm_resumen_periodo", "FUNCTION fn_crm_embudo", "FUNCTION fn_crm_ventas_por_comercial",
                         "PROCEDURE sp_meta_guardar", "FUNCTION fn_metas_mes", "PROCEDURE sp_parametros_comerciales_actualizar",
                         "FUNCTION fn_parametros_comerciales",
                         "PROCEDURE sp_tarea_eliminar", "PROCEDURE sp_tarea_cambiar_estado", "PROCEDURE sp_tarea_actualizar",
                         "PROCEDURE sp_tarea_crear", "FUNCTION fn_tarea_por_id", "FUNCTION fn_tareas_listar",
                         "PROCEDURE sp_tracking_cambiar_fase", "PROCEDURE sp_tracking_actualizar", "FUNCTION fn_tracking_por_id",
                         "FUNCTION fn_tracking_listar",
                         "PROCEDURE sp_venta_actualizar", "PROCEDURE sp_venta_crear", "FUNCTION fn_venta_por_id",
                         "FUNCTION fn_ventas_listar",
                         "PROCEDURE sp_oportunidad_eliminar", "PROCEDURE sp_oportunidad_ganar",
                         "PROCEDURE sp_oportunidad_cambiar_etapa", "PROCEDURE sp_oportunidad_actualizar",
                         "PROCEDURE sp_oportunidad_crear", "FUNCTION fn_oportunidad_por_id", "FUNCTION fn_oportunidades_listar",
                         "FUNCTION fn_comerciales_listar", "PROCEDURE sp_cliente_eliminar", "PROCEDURE sp_cliente_actualizar",
                         "PROCEDURE sp_cliente_crear", "FUNCTION fn_cliente_tiene_movimientos", "FUNCTION fn_cliente_valor_existe",
                         "FUNCTION fn_cliente_por_id", "FUNCTION fn_clientes_listar",
                         "PROCEDURE sp_consecutivo_ajustar", "FUNCTION fn_siguiente_consecutivo",
                     })
            {
                var (tipo, nombre) = (objeto.Split(' ')[0], objeto.Split(' ')[1]);
                migrationBuilder.Sql($"DROP {tipo} IF EXISTS {nombre};");
            }

            // Los datos sembrados (consecutivos, parámetros) se van con las tablas en el Down de Crm.
        }
    }
}
