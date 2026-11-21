using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Comun;

namespace JRTropicalFish.Application.Crm;

/// <summary>
/// Decide el comercial de un registro (oportunidad, venta, tarea) según quién lo pide:
/// <list type="bullet">
/// <item>Un comercial sin <c>crm:ver-todo</c> solo se asigna a sí mismo y no reasigna.</item>
/// <item>Los demás eligen entre los comerciales habilitados; un comercial que pide sin elegir se asigna a sí mismo.</item>
/// <item>Si el comercial no cambia, se conserva aunque ya no esté habilitado (el registro no se rompe).</item>
/// </list>
/// </summary>
public static class ComercialesCrm
{
    public static async Task<Resultado<ComercialInfo>> ResolverAsync(
        IComercialRepository comerciales,
        AlcanceCrm alcance,
        Guid? pedido,
        (Guid Id, string Nombre)? actual,
        CancellationToken ct)
    {
        Guid? id;
        if (alcance.FiltroComercial is { } propio)
        {
            id = actual?.Id ?? propio;
            if (pedido is not null && pedido != id)
            {
                return Error.Prohibido("Solo puedes asignarte registros a ti mismo.");
            }
        }
        else
        {
            id = pedido ?? actual?.Id ?? (alcance.EsComercial ? alcance.UsuarioId : null);
        }

        if (id is null)
        {
            return Error.Validacion("Elige el comercial asignado.");
        }

        if (actual is { } a && a.Id == id)
        {
            return new ComercialInfo(a.Id, a.Nombre, "");
        }

        var habilitado = (await comerciales.ListarAsync(ct)).FirstOrDefault(c => c.Id == id);
        return habilitado is null
            ? Error.Validacion("El comercial elegido no está habilitado (necesita un perfil con «crm:vender»).")
            : habilitado;
    }
}
