using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGastos.Application.Interfaces
{
    public interface IDolarService
    {
        Task<decimal> GetDolarTarjetaAsync();
        Task<decimal> GetDolarBolsaAsync();
    }
}
