using Microsoft.AspNetCore.Authorization;

namespace SistemaGastos.WebApp.Controllers;

[Route("api/dolar")]
[ApiController]
[Authorize]
public class DolarController(IDolarService dolarService) : ControllerBase
{
    [HttpGet("tarjeta")]
    public async Task<IActionResult> ObtenerDolarTarjeta()
    {
        var valorDolar = await dolarService.GetDolarTarjetaAsync();
        return Ok(new { dolarTarjeta = valorDolar });
    }

    [HttpGet("bolsa")]
    public async Task<IActionResult> ObtenerDolarBolsa()
    {
        var valorDolar = await dolarService.GetDolarBolsaAsync();
        return Ok(new { dolarBolsa = valorDolar });
    }
}
