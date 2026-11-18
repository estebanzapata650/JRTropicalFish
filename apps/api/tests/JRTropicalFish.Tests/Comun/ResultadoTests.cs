using JRTropicalFish.Application.Comun;

namespace JRTropicalFish.Tests.Comun;

public class ResultadoTests
{
    [Fact]
    public void Un_resultado_ok_expone_su_valor()
    {
        Resultado<int> resultado = 42;

        Assert.True(resultado.Exitoso);
        Assert.Null(resultado.Error);
        Assert.Equal(42, resultado.Valor);
    }

    [Fact]
    public void Un_error_se_convierte_en_resultado_fallido()
    {
        Resultado<int> resultado = Error.Conflicto("No hay stock suficiente.");

        Assert.False(resultado.Exitoso);
        Assert.Equal(TipoError.Conflicto, resultado.Error!.Tipo);
        Assert.Equal("No hay stock suficiente.", resultado.Error.Mensaje);
    }

    [Fact]
    public void Leer_el_valor_de_un_resultado_fallido_es_un_error_de_programacion()
    {
        Resultado<string> resultado = Error.NoEncontrado("La especie no existe.");

        var ex = Assert.Throws<InvalidOperationException>(() => resultado.Valor);
        Assert.Contains("La especie no existe.", ex.Message);
    }

    [Fact]
    public void El_resultado_sin_valor_tambien_distingue_exito_y_falla()
    {
        Assert.True(Resultado.Ok().Exitoso);
        Assert.False(Resultado.Falla(Error.Validacion("Dato inválido")).Exitoso);
    }
}
