namespace SistemaGastos.Application.Interfaces;

public interface IAccountInterestService
{
    /// <summary>
    /// Pone al día el log diario de intereses de todas las cuentas configuradas
    /// y, si corresponde (día >= 7), genera la transacción de cobro mensual.
    /// Pensado para ejecutarse una vez al iniciar el sistema.
    /// </summary>
    Task RunAccrualAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reconstruye desde cero el log diario de intereses de las cuentas configuradas
    /// del usuario, en base al historial de transacciones. No vuelve a generar
    /// las transacciones de cobro mensual ya creadas.
    /// </summary>
    Task RecalculateAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suma de los intereses diarios devengados hasta la fecha para las cuentas
    /// configuradas (habilitadas) del usuario.
    /// </summary>
    Task<decimal> GetTotalAccruedInterestAsync(int userId, CancellationToken cancellationToken = default);
}
