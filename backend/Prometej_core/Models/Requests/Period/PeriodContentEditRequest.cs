using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.Requests.Period
{
    public class PeriodContentEditRequest
    {
        public int Id { get; set; }
        public int PeriodId { get; set; }
        public required string Content { get; set; }
    }
}
