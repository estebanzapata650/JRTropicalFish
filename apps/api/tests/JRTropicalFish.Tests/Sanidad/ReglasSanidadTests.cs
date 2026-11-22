using JRTropicalFish.Application.Sanidad;
using JRTropicalFish.Domain.Sanidad;

namespace JRTropicalFish.Tests.Sanidad;

/// <summary>
/// Reglas de sanidad con las dosis de los reportes AQUAVET (acuarios de 80 L) y la regla firme de 48 h de
/// ayuno antes del vuelo.
/// </summary>
public class ReglasSanidadTests
{
    private static MedicamentoFila Medicamento(string codigo, string unidad, string tipo, decimal cantidad, decimal baseDosis, int retiro = 0) =>
        new(Guid.NewGuid(), codigo, codigo, null, null, unidad, tipo, cantidad, null, baseDosis, null, true, retiro, null, null, null, true, null);

    [Fact]
    public void Dosis_por_volumen_es_proporcional_a_los_litros_como_en_los_reportes()
    {
        // Levamisol 15 %: 0,6 mL/100 L → 0,48 mL en 80 L (el reporte redondea a 0,4–0,5).
        Assert.Equal(0.48m, ReglasSanidad.Dosis(TiposDosis.PorVolumen, 0.6m, 100, 80, UnidadesSanidad.Mililitro));
        // Praziquantel: 250 mg/100 L → 200 mg en 80 L (exacto como el reporte).
        Assert.Equal(200m, ReglasSanidad.Dosis(TiposDosis.PorVolumen, 250, 100, 80, UnidadesSanidad.Miligramo));
        // Metronidazol: 500 mg/80 L → una tableta en 80 L.
        Assert.Equal(500m, ReglasSanidad.Dosis(TiposDosis.PorVolumen, 500, 80, 80, UnidadesSanidad.Miligramo));
        // Sal marina 1 g/L en 64 L.
        Assert.Equal(64m, ReglasSanidad.Dosis(TiposDosis.PorVolumen, 1, 1, 64, UnidadesSanidad.Gramo));
        // Cupramine 1 mL/80 L en 120 L.
        Assert.Equal(1.5m, ReglasSanidad.Dosis(TiposDosis.PorVolumen, 1, 80, 120, UnidadesSanidad.Mililitro));
    }

    [Fact]
    public void Sin_litraje_la_dosis_por_volumen_no_se_calcula()
    {
        Assert.Null(ReglasSanidad.Dosis(TiposDosis.PorVolumen, 0.6m, 100, null, UnidadesSanidad.Mililitro));
        Assert.Null(ReglasSanidad.Dosis(TiposDosis.PorAlimento, 200, 100, 80, UnidadesSanidad.Miligramo));
        Assert.Equal(5m, ReglasSanidad.Dosis(TiposDosis.Fija, 5, 1, null, UnidadesSanidad.Mililitro));
    }

    [Theory]
    [InlineData(1.649, UnidadesSanidad.Mililitro, 1.6)]
    [InlineData(0.333, UnidadesSanidad.Mililitro, 0.33)]
    [InlineData(2.4, UnidadesSanidad.Gota, 2)]
    [InlineData(0.2, UnidadesSanidad.Gota, 1)]
    [InlineData(0.8, UnidadesSanidad.Tableta, 1)]
    [InlineData(1.3, UnidadesSanidad.Tableta, 1.5)]
    [InlineData(8.04, UnidadesSanidad.Miligramo, 8.0)]
    [InlineData(187.5, UnidadesSanidad.Miligramo, 188)]
    public void Redondea_a_lo_que_el_operario_puede_medir(decimal valor, string unidad, decimal esperado) =>
        Assert.Equal(esperado, ReglasSanidad.Redondear(valor, unidad));

    [Fact]
    public void Aplicaciones_salen_por_dia_con_la_dosis_del_acuario()
    {
        var levamisol = Medicamento("LEV15", UnidadesSanidad.Mililitro, TiposDosis.PorVolumen, 0.6m, 100);
        var mebendazol = Medicamento("MEBEN", UnidadesSanidad.Miligramo, TiposDosis.PorAlimento, 200, 100);
        var medicamentos = new[] { levamisol, mebendazol }.ToDictionary(m => m.Id);
        var inicio = new DateOnly(2026, 11, 22);
        PasoRequest[] pasos =
        [
            new(1, AccionesSanidad.Ayuno, null, null, null),
            new(1, AccionesSanidad.CambioAgua, null, 50, null),
            new(2, AccionesSanidad.Medicar, levamisol.Id, null, null),
            new(3, AccionesSanidad.Medicar, mebendazol.Id, null, null),
        ];

        var aplicaciones = ReglasSanidad.Aplicaciones(pasos, medicamentos, inicio, 80);

        Assert.Equal(4, aplicaciones.Count);
        Assert.Equal([1, 2], aplicaciones.Where(a => a.Dia == 1).Select(a => a.Orden));
        Assert.Equal(50, aplicaciones[1].PorcentajeAgua);
        Assert.Equal(new DateOnly(2026, 11, 23), aplicaciones[2].Fecha);
        Assert.Equal(0.48m, aplicaciones[2].Cantidad);
        Assert.Equal(UnidadesSanidad.Mililitro, aplicaciones[2].Unidad);
        Assert.Null(aplicaciones[3].Cantidad);
        Assert.Contains("200 mg por cada 100 g", aplicaciones[3].Nota);
    }

    [Fact]
    public void Fin_y_liberacion_cuentan_el_tiempo_de_retiro_del_medicamento_mas_largo()
    {
        var inicio = new DateOnly(2026, 11, 22);
        PasoRequest[] pasos = [new(1, AccionesSanidad.Ayuno, null, null, null), new(3, AccionesSanidad.CambioAgua, null, 80, null)];

        var fin = ReglasSanidad.FechaFin(inicio, 5, pasos);
        Assert.Equal(new DateOnly(2026, 11, 26), fin);
        Assert.Equal(new DateOnly(2026, 11, 24), ReglasSanidad.FechaFin(inicio, null, pasos));

        var sinRetiro = Medicamento("SAL", UnidadesSanidad.Gramo, TiposDosis.PorVolumen, 1, 1);
        var conRetiro = Medicamento("X", UnidadesSanidad.Mililitro, TiposDosis.PorVolumen, 1, 1, retiro: 7);
        Assert.Equal(new DateOnly(2026, 11, 27), ReglasSanidad.FechaLiberacion(fin, [sinRetiro]));
        Assert.Equal(new DateOnly(2026, 12, 4), ReglasSanidad.FechaLiberacion(fin, [sinRetiro, conRetiro]));
        Assert.Equal(new DateOnly(2026, 11, 27), ReglasSanidad.FechaLiberacion(fin, []));
    }

    [Fact]
    public void Dia_actual_del_tratamiento()
    {
        var inicio = new DateOnly(2026, 11, 20);
        var fin = new DateOnly(2026, 11, 24);
        Assert.Equal(3, ReglasSanidad.DiaActual(inicio, fin, new DateOnly(2026, 11, 22)));
        Assert.Equal(5, ReglasSanidad.DiasTotal(inicio, fin));
        Assert.Equal(5, ReglasSanidad.DiaActual(inicio, fin, new DateOnly(2026, 12, 1)));
        Assert.Equal(1, ReglasSanidad.DiaActual(inicio, fin, new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public void Ayuno_pre_vuelo_empieza_exactamente_48_horas_antes()
    {
        var salida = new DateTime(2026, 11, 24, 15, 0, 0, DateTimeKind.Utc);
        Assert.False(ReglasSanidad.EnAyunoVuelo(salida, salida.AddHours(-48).AddMinutes(-1), 48));
        Assert.True(ReglasSanidad.EnAyunoVuelo(salida, salida.AddHours(-48), 48));
        Assert.True(ReglasSanidad.EnAyunoVuelo(salida, salida.AddHours(-30), 48));
        // Vuelo ya salió y el pedido sigue sin despachar: sigue en ayuno.
        Assert.True(ReglasSanidad.EnAyunoVuelo(salida, salida.AddHours(2), 48));
        // Con más horas configuradas empieza antes.
        Assert.True(ReglasSanidad.EnAyunoVuelo(salida, salida.AddHours(-60), 72));
    }

    [Fact]
    public void Sin_hora_de_vuelo_cuenta_desde_la_medianoche_del_despacho_en_Bogota()
    {
        var conHora = ReglasSanidad.SalidaVuelo(new DateTime(2026, 11, 24, 20, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 11, 24));
        Assert.Equal((new DateTime(2026, 11, 24, 20, 0, 0, DateTimeKind.Utc), false), conHora);

        var sinHora = ReglasSanidad.SalidaVuelo(null, new DateOnly(2026, 11, 24));
        Assert.Equal((new DateTime(2026, 11, 24, 5, 0, 0, DateTimeKind.Utc), true), sinHora);

        Assert.Null(ReglasSanidad.SalidaVuelo(null, null));
    }

    [Theory]
    [InlineData(true, true, true, true, EstadosAcuario.AyunoVuelo)]
    [InlineData(false, true, false, true, EstadosAcuario.Cuarentena)]
    [InlineData(false, false, true, true, EstadosAcuario.Cuarentena)]
    [InlineData(false, false, false, true, EstadosAcuario.Aclimatacion)]
    [InlineData(false, false, false, false, EstadosAcuario.Estable)]
    public void Columna_del_tablero_por_prioridad(bool vuelo, bool tratamiento, bool enfermos, bool aclimatacion, string esperada) =>
        Assert.Equal(esperada, ReglasSanidad.Columna(vuelo, tratamiento, enfermos, aclimatacion));

    [Fact]
    public void Motivos_de_ayuno()
    {
        var conBano = new TratamientoAcuario(Guid.NewGuid(), "TRT-001", "P", EstadosTratamiento.Activo, 2, 5, new DateOnly(2026, 11, 30), 1, true, false);
        var soloAlimentoMedicado = conBano with { RequiereAyuno = false, AlimentoMedicado = true };
        var enRetiro = conBano with { Estado = EstadosTratamiento.Terminado };

        Assert.Equal([MotivosAyuno.Vuelo, MotivosAyuno.Tratamiento, MotivosAyuno.Llegada], ReglasSanidad.RazonesAyuno(true, conBano, true));
        Assert.Empty(ReglasSanidad.RazonesAyuno(false, soloAlimentoMedicado, false));
        Assert.Empty(ReglasSanidad.RazonesAyuno(false, enRetiro, false));
        Assert.Equal([MotivosAyuno.Llegada], ReglasSanidad.RazonesAyuno(false, null, true));
    }

    [Fact]
    public void Ayuno_de_llegada_dura_las_horas_de_la_ficha()
    {
        var llegada = new DateTime(2026, 11, 22, 13, 0, 0, DateTimeKind.Utc);
        Assert.True(ReglasSanidad.EnAyunoLlegada(llegada, 24, llegada.AddHours(23)));
        Assert.False(ReglasSanidad.EnAyunoLlegada(llegada, 24, llegada.AddHours(24)));
        Assert.False(ReglasSanidad.EnAyunoLlegada(llegada, 0, llegada));
    }

    [Fact]
    public void Dieta_del_producto_reemplaza_la_del_grupo_y_la_racion_es_por_cada_100_peces()
    {
        var grupo = Guid.NewGuid();
        var producto = Guid.NewGuid();
        var otro = Guid.NewGuid();
        var delGrupo = new DietaFila(Guid.NewGuid(), grupo, "Corydoras", null, null, null, Guid.NewGuid(), "Hojuela", "g", 2, 1.5m, null);
        var propia = new DietaFila(Guid.NewGuid(), null, null, producto, "CO125", "C. x", Guid.NewGuid(), "Artemia", "g", 3, 2m, null);
        DietaFila[] dietas = [delGrupo, propia];

        Assert.Equal([propia], ReglasSanidad.DietasDe(producto, grupo, dietas));
        Assert.Equal([delGrupo], ReglasSanidad.DietasDe(otro, grupo, dietas));
        Assert.Empty(ReglasSanidad.DietasDe(otro, Guid.NewGuid(), dietas));

        Assert.Equal(6.75m, ReglasSanidad.CantidadToma(1.5m, 450, UnidadesSanidad.Gramo));
        Assert.Equal(0m, ReglasSanidad.CantidadToma(1.5m, 0, UnidadesSanidad.Gramo));
        Assert.Equal(2m, ReglasSanidad.CantidadToma(1m, 150, UnidadesSanidad.Cubo));
    }

    [Fact]
    public void Sugiere_el_protocolo_aprobado_mas_especifico_y_nunca_un_borrador()
    {
        var producto = Guid.NewGuid();
        var grupo = Guid.NewGuid();
        var centro = Guid.NewGuid();
        ProtocoloFila Protocolo(string codigo, string estado, Guid? p = null, Guid? g = null, Guid? c = null) =>
            new(Guid.NewGuid(), codigo, codigo, null, estado, p, null, g, null, c, null, null, null, 5, 24, null, null, 3);

        var general = Protocolo("PRT-001", EstadosProtocolo.Aprobado);
        var delCentro = Protocolo("PRT-002", EstadosProtocolo.Aprobado, c: centro);
        var delGrupo = Protocolo("PRT-003", EstadosProtocolo.Aprobado, g: grupo);
        var borradorDelProducto = Protocolo("PRT-004", EstadosProtocolo.Borrador, p: producto);
        var otroCentro = Protocolo("PRT-005", EstadosProtocolo.Aprobado, g: grupo, c: Guid.NewGuid());

        ProtocoloFila[] todos = [general, delCentro, delGrupo, borradorDelProducto, otroCentro];
        Assert.Equal(delGrupo, ReglasSanidad.Sugerir(todos, producto, grupo, centro, null));
        Assert.Equal(delCentro, ReglasSanidad.Sugerir(todos, producto, Guid.NewGuid(), centro, null));
        Assert.Equal(general, ReglasSanidad.Sugerir(todos, producto, Guid.NewGuid(), null, null));
        Assert.Null(ReglasSanidad.Sugerir([borradorDelProducto], producto, grupo, centro, null));
    }

    [Fact]
    public void Valida_los_pasos()
    {
        var sal = Medicamento("SAL", UnidadesSanidad.Gramo, TiposDosis.PorVolumen, 1, 1);
        var medicamentos = new[] { sal }.ToDictionary(m => m.Id);

        Assert.Null(ReglasSanidad.ValidarPasos([new(1, AccionesSanidad.Medicar, sal.Id, null, null)], medicamentos));
        Assert.NotNull(ReglasSanidad.ValidarPasos([new(1, AccionesSanidad.Medicar, null, null, null)], medicamentos));
        Assert.NotNull(ReglasSanidad.ValidarPasos([new(0, AccionesSanidad.Ayuno, null, null, null)], medicamentos));
        Assert.NotNull(ReglasSanidad.ValidarPasos([new(1, AccionesSanidad.CambioAgua, null, null, null)], medicamentos));
        Assert.NotNull(ReglasSanidad.ValidarPasos([new(1, "bailar", null, null, null)], medicamentos));
    }

    [Fact]
    public void Detecta_medicamentos_incompatibles()
    {
        var a = Medicamento("A", UnidadesSanidad.Mililitro, TiposDosis.PorVolumen, 1, 1);
        var b = Medicamento("B", UnidadesSanidad.Mililitro, TiposDosis.PorVolumen, 1, 1);
        var c = Medicamento("C", UnidadesSanidad.Mililitro, TiposDosis.PorVolumen, 1, 1);
        a = a with { Incompatibles = [b.Id] };
        b = b with { Incompatibles = [a.Id] };

        Assert.Single(ReglasSanidad.Incompatibles([a, b, c]));
        Assert.Empty(ReglasSanidad.Incompatibles([a, c]));
    }
}
