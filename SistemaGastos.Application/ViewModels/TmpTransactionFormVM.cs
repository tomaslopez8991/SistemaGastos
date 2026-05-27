using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaGastos.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGastos.Application.ViewModels
{
    public class TmpTransactionFormVM
    {
        public TmpTransaction Transaction { get; set; } = new();
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Accounts { get; set; } = new();
        public List<SelectListItem> NextMonths { get; set; } = new();
    }
}
