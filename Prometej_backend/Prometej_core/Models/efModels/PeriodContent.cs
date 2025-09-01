using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.efModels
{
    public class PeriodContent
    {
        public int Id { get; set; }
        public int PeriodId { get; set; }
        public required string Content { get; set; }
    }
}
