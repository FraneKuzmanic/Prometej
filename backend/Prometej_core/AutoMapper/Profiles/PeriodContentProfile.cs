using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Prometej_core.Models.efModels;
using Prometej_core.Models.Requests.Period;
using Prometej_core.Models.ViewModels;

namespace Prometej_core.AutoMapper.Profiles
{
    public class PeriodContentProfile: Profile
    {
        public PeriodContentProfile()
        {
            CreateMap<PeriodContentCreateRequest, PeriodContent>();
            CreateMap<PeriodContent, PeriodContentCreateRequest>();
            CreateMap<PeriodContentEditRequest, PeriodContent>();
            CreateMap<PeriodContent, PeriodContentEditRequest>();
            CreateMap<PeriodContent, PeriodContentViewModel>();
        }
    }
}
