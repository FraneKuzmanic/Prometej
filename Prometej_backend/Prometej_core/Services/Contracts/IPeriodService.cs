using Prometej_core.Models.Requests.Period;
using Prometej_core.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Services.Contracts
{
    public interface IPeriodService
    {
        int UpdatePeriodContent(PeriodContentEditRequest period);
        PeriodContentViewModel GetPeriodContent(int id);
        List<PeriodSearchContentViewModel> SearchPeriodContent(string query);
    }
}
