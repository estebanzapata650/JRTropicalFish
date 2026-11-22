using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sanidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "responsable_id",
                table: "acuarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "alimentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unidad = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alimentos", x => x.id);
                    table.CheckConstraint("ck_alimentos_tipo", "tipo IN ('seco', 'vivo', 'congelado', 'papilla')");
                    table.CheckConstraint("ck_alimentos_unidad", "unidad IN ('g', 'ml', 'cubo')");
                });

            migrationBuilder.CreateTable(
                name: "fichas_tecnicas",
                columns: table => new
                {
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    procedencia = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    caracteristicas = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    dimorfismo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ph_min = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    ph_max = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    gh_min = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    gh_max = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    temperatura_min = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    temperatura_max = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    etologia = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    empaque = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sensibilidad_quimica = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ayuno_llegada_horas = table.Column<int>(type: "integer", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fichas_tecnicas", x => x.producto_id);
                    table.CheckConstraint("ck_fichas_tecnicas_rangos", "(ph_min IS NULL OR ph_max IS NULL OR ph_min <= ph_max) AND (gh_min IS NULL OR gh_max IS NULL OR gh_min <= gh_max) AND (temperatura_min IS NULL OR temperatura_max IS NULL OR temperatura_min <= temperatura_max) AND ayuno_llegada_horas BETWEEN 0 AND 168");
                    table.ForeignKey(
                        name: "fk_fichas_tecnicas_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medicamentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    principio_activo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    presentacion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    unidad = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tipo_dosis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dosis_cantidad = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    dosis_maxima = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    dosis_base = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    exposicion_minutos = table.Column<int>(type: "integer", nullable: true),
                    requiere_ayuno = table.Column<bool>(type: "boolean", nullable: false),
                    dias_retiro = table.Column<int>(type: "integer", nullable: false),
                    indicaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    contraindicaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fuente = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medicamentos", x => x.id);
                    table.CheckConstraint("ck_medicamentos_dosis", "dosis_cantidad > 0 AND dosis_base > 0 AND (dosis_maxima IS NULL OR dosis_maxima >= dosis_cantidad) AND dias_retiro >= 0 AND (exposicion_minutos IS NULL OR exposicion_minutos > 0)");
                    table.CheckConstraint("ck_medicamentos_tipo_dosis", "tipo_dosis IN ('por_volumen', 'por_alimento', 'fija')");
                    table.CheckConstraint("ck_medicamentos_unidad", "unidad IN ('ml', 'g', 'mg', 'gotas', 'tableta')");
                });

            migrationBuilder.CreateTable(
                name: "parametros_sanidad",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    horas_ayuno_vuelo = table.Column<int>(type: "integer", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parametros_sanidad", x => x.id);
                    table.CheckConstraint("ck_parametros_sanidad_ayuno", "horas_ayuno_vuelo BETWEEN 48 AND 120");
                    table.CheckConstraint("ck_parametros_sanidad_fila_unica", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "protocolos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grupo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    centro_acopio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cuenca_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dias_cuarentena = table.Column<int>(type: "integer", nullable: true),
                    ayuno_inicial_horas = table.Column<int>(type: "integer", nullable: false),
                    aprobado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    aprobado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_protocolos", x => x.id);
                    table.CheckConstraint("ck_protocolos_dias", "(dias_cuarentena IS NULL OR dias_cuarentena BETWEEN 1 AND 120) AND ayuno_inicial_horas BETWEEN 0 AND 168 AND (estado <> 'aprobado' OR (dias_cuarentena IS NOT NULL AND aprobado_en IS NOT NULL))");
                    table.CheckConstraint("ck_protocolos_estado", "estado IN ('borrador', 'aprobado', 'inactivo')");
                    table.ForeignKey(
                        name: "fk_protocolos_centros_acopio_centro_acopio_id",
                        column: x => x.centro_acopio_id,
                        principalTable: "centros_acopio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_protocolos_cuencas_cuenca_id",
                        column: x => x.cuenca_id,
                        principalTable: "cuencas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_protocolos_grupos_producto_grupo_id",
                        column: x => x.grupo_id,
                        principalTable: "grupos_producto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_protocolos_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_protocolos_usuarios_aprobado_por_id",
                        column: x => x.aprobado_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reservas_despacho",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pedido_cliente_linea_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservas_despacho", x => x.id);
                    table.CheckConstraint("ck_reservas_despacho_cantidad", "cantidad > 0");
                    table.ForeignKey(
                        name: "fk_reservas_despacho_acuarios_acuario_id",
                        column: x => x.acuario_id,
                        principalTable: "acuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reservas_despacho_lotes_lote_id",
                        column: x => x.lote_id,
                        principalTable: "lotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reservas_despacho_pedidos_cliente_lineas_pedido_cliente_lin",
                        column: x => x.pedido_cliente_linea_id,
                        principalTable: "pedidos_cliente_lineas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reservas_despacho_usuarios_creado_por_id",
                        column: x => x.creado_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dietas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    alimento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    veces_dia = table.Column<int>(type: "integer", nullable: false),
                    cantidad_por100 = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    nota = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dietas", x => x.id);
                    table.CheckConstraint("ck_dietas_destino", "(grupo_id IS NULL) <> (producto_id IS NULL)");
                    table.CheckConstraint("ck_dietas_valores", "veces_dia BETWEEN 1 AND 12 AND cantidad_por100 > 0");
                    table.ForeignKey(
                        name: "fk_dietas_alimentos_alimento_id",
                        column: x => x.alimento_id,
                        principalTable: "alimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_dietas_grupos_producto_grupo_id",
                        column: x => x.grupo_id,
                        principalTable: "grupos_producto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_dietas_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registros_alimentacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    acuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alimento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cantidad = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    usuario_nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registros_alimentacion", x => x.id);
                    table.CheckConstraint("ck_registros_alimentacion_cantidad", "cantidad IS NULL OR cantidad >= 0");
                    table.ForeignKey(
                        name: "fk_registros_alimentacion_acuarios_acuario_id",
                        column: x => x.acuario_id,
                        principalTable: "acuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registros_alimentacion_alimentos_alimento_id",
                        column: x => x.alimento_id,
                        principalTable: "alimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registros_alimentacion_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "medicamentos_incompatibles",
                columns: table => new
                {
                    medicamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    incompatible_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medicamentos_incompatibles", x => new { x.medicamento_id, x.incompatible_id });
                    table.CheckConstraint("ck_medicamentos_incompatibles_par", "medicamento_id < incompatible_id");
                    table.ForeignKey(
                        name: "fk_medicamentos_incompatibles_medicamentos_incompatible_id",
                        column: x => x.incompatible_id,
                        principalTable: "medicamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_medicamentos_incompatibles_medicamentos_medicamento_id",
                        column: x => x.medicamento_id,
                        principalTable: "medicamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "protocolo_pasos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    protocolo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dia = table.Column<int>(type: "integer", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    accion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    medicamento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    porcentaje_agua = table.Column<int>(type: "integer", nullable: true),
                    nota = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_protocolo_pasos", x => x.id);
                    table.CheckConstraint("ck_protocolo_pasos_accion", "accion IN ('medicar', 'cambio_agua', 'ayuno', 'observacion')");
                    table.CheckConstraint("ck_protocolo_pasos_valores", "dia BETWEEN 1 AND 120 AND (accion <> 'medicar' OR medicamento_id IS NOT NULL) AND (porcentaje_agua IS NULL OR porcentaje_agua BETWEEN 1 AND 100)");
                    table.ForeignKey(
                        name: "fk_protocolo_pasos_medicamentos_medicamento_id",
                        column: x => x.medicamento_id,
                        principalTable: "medicamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_protocolo_pasos_protocolos_protocolo_id",
                        column: x => x.protocolo_id,
                        principalTable: "protocolos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tratamientos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    acuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protocolo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    diagnostico = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    litros = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_liberacion = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reporte_clave = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    nota = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tratamientos", x => x.id);
                    table.CheckConstraint("ck_tratamientos_estado", "estado IN ('activo', 'terminado', 'cancelado')");
                    table.CheckConstraint("ck_tratamientos_fechas", "fecha_fin >= fecha_inicio AND fecha_liberacion >= fecha_fin");
                    table.CheckConstraint("ck_tratamientos_motivo", "motivo IN ('profilaxis_llegada', 'prescripcion', 'otro')");
                    table.ForeignKey(
                        name: "fk_tratamientos_acuarios_acuario_id",
                        column: x => x.acuario_id,
                        principalTable: "acuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tratamientos_protocolos_protocolo_id",
                        column: x => x.protocolo_id,
                        principalTable: "protocolos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tratamientos_usuarios_creado_por_id",
                        column: x => x.creado_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tratamiento_aplicaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tratamiento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    dia = table.Column<int>(type: "integer", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    accion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    medicamento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    porcentaje_agua = table.Column<int>(type: "integer", nullable: true),
                    cantidad_calculada = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    cantidad_aplicada = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    unidad = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nota = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    usuario_nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    registrado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tratamiento_aplicaciones", x => x.id);
                    table.CheckConstraint("ck_tratamiento_aplicaciones_accion", "accion IN ('medicar', 'cambio_agua', 'ayuno', 'observacion')");
                    table.CheckConstraint("ck_tratamiento_aplicaciones_estado", "estado IN ('pendiente', 'aplicada', 'omitida')");
                    table.CheckConstraint("ck_tratamiento_aplicaciones_valores", "(cantidad_calculada IS NULL OR cantidad_calculada >= 0) AND (cantidad_aplicada IS NULL OR cantidad_aplicada >= 0) AND (accion <> 'medicar' OR medicamento_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_tratamiento_aplicaciones_medicamentos_medicamento_id",
                        column: x => x.medicamento_id,
                        principalTable: "medicamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tratamiento_aplicaciones_tratamientos_tratamiento_id",
                        column: x => x.tratamiento_id,
                        principalTable: "tratamientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tratamiento_aplicaciones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_acuarios_responsable_id",
                table: "acuarios",
                column: "responsable_id");

            migrationBuilder.CreateIndex(
                name: "ix_alimentos_codigo",
                table: "alimentos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dietas_alimento_id",
                table: "dietas",
                column: "alimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_dietas_grupo_id_alimento_id",
                table: "dietas",
                columns: new[] { "grupo_id", "alimento_id" },
                unique: true,
                filter: "grupo_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_dietas_producto_id_alimento_id",
                table: "dietas",
                columns: new[] { "producto_id", "alimento_id" },
                unique: true,
                filter: "producto_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_medicamentos_codigo",
                table: "medicamentos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_medicamentos_incompatibles_incompatible_id",
                table: "medicamentos_incompatibles",
                column: "incompatible_id");

            migrationBuilder.CreateIndex(
                name: "ix_protocolo_pasos_medicamento_id",
                table: "protocolo_pasos",
                column: "medicamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_protocolo_pasos_protocolo_id_dia_orden",
                table: "protocolo_pasos",
                columns: new[] { "protocolo_id", "dia", "orden" });

            migrationBuilder.CreateIndex(
                name: "ix_protocolos_aprobado_por_id",
                table: "protocolos",
                column: "aprobado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_protocolos_centro_acopio_id",
                table: "protocolos",
                column: "centro_acopio_id");

            migrationBuilder.CreateIndex(
                name: "ix_protocolos_codigo",
                table: "protocolos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_protocolos_cuenca_id",
                table: "protocolos",
                column: "cuenca_id");

            migrationBuilder.CreateIndex(
                name: "ix_protocolos_grupo_id",
                table: "protocolos",
                column: "grupo_id");

            migrationBuilder.CreateIndex(
                name: "ix_protocolos_producto_id",
                table: "protocolos",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "ix_registros_alimentacion_acuario_id_fecha",
                table: "registros_alimentacion",
                columns: new[] { "acuario_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_registros_alimentacion_alimento_id",
                table: "registros_alimentacion",
                column: "alimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_registros_alimentacion_usuario_id",
                table: "registros_alimentacion",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservas_despacho_acuario_id",
                table: "reservas_despacho",
                column: "acuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservas_despacho_creado_por_id",
                table: "reservas_despacho",
                column: "creado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservas_despacho_lote_id_acuario_id",
                table: "reservas_despacho",
                columns: new[] { "lote_id", "acuario_id" });

            migrationBuilder.CreateIndex(
                name: "ix_reservas_despacho_pedido_cliente_linea_id_lote_id_acuario_id",
                table: "reservas_despacho",
                columns: new[] { "pedido_cliente_linea_id", "lote_id", "acuario_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tratamiento_aplicaciones_fecha_estado",
                table: "tratamiento_aplicaciones",
                columns: new[] { "fecha", "estado" });

            migrationBuilder.CreateIndex(
                name: "ix_tratamiento_aplicaciones_medicamento_id",
                table: "tratamiento_aplicaciones",
                column: "medicamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_tratamiento_aplicaciones_tratamiento_id_fecha_orden",
                table: "tratamiento_aplicaciones",
                columns: new[] { "tratamiento_id", "fecha", "orden" });

            migrationBuilder.CreateIndex(
                name: "ix_tratamiento_aplicaciones_usuario_id",
                table: "tratamiento_aplicaciones",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_tratamientos_acuario_id_estado",
                table: "tratamientos",
                columns: new[] { "acuario_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "ix_tratamientos_codigo",
                table: "tratamientos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tratamientos_creado_por_id",
                table: "tratamientos",
                column: "creado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_tratamientos_protocolo_id",
                table: "tratamientos",
                column: "protocolo_id");

            migrationBuilder.AddForeignKey(
                name: "fk_acuarios_usuarios_responsable_id",
                table: "acuarios",
                column: "responsable_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_acuarios_usuarios_responsable_id",
                table: "acuarios");

            migrationBuilder.DropTable(
                name: "dietas");

            migrationBuilder.DropTable(
                name: "fichas_tecnicas");

            migrationBuilder.DropTable(
                name: "medicamentos_incompatibles");

            migrationBuilder.DropTable(
                name: "parametros_sanidad");

            migrationBuilder.DropTable(
                name: "protocolo_pasos");

            migrationBuilder.DropTable(
                name: "registros_alimentacion");

            migrationBuilder.DropTable(
                name: "reservas_despacho");

            migrationBuilder.DropTable(
                name: "tratamiento_aplicaciones");

            migrationBuilder.DropTable(
                name: "alimentos");

            migrationBuilder.DropTable(
                name: "medicamentos");

            migrationBuilder.DropTable(
                name: "tratamientos");

            migrationBuilder.DropTable(
                name: "protocolos");

            migrationBuilder.DropIndex(
                name: "ix_acuarios_responsable_id",
                table: "acuarios");

            migrationBuilder.DropColumn(
                name: "responsable_id",
                table: "acuarios");
        }
    }
}
