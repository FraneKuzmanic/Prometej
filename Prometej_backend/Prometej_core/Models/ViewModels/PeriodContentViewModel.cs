using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.ViewModels
{
    public class PeriodContentViewModel
    {
        public int Id { get; set; }
        public int PeriodId { get; set; }
        public required string Content { get; set; }
    }
}
