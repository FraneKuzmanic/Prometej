using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.ViewModels
{
    public class PeriodSearchContentViewModel
    {
        public int PeriodId { get; set; }
        public required string PeriodName { get; set; }
        // How many passages of the Period match; only the first few are sent.
        public int MatchCount { get; set; }
        public required List<PeriodSearchPassageViewModel> Passages { get; set; }
    }
}
