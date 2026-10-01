using AutoMapper;
using Prometej_core.Models.Base;
using Prometej_core.Models.efModels;
using Prometej_core.Models.Requests.Quiz;
using Prometej_core.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.AutoMapper.Profiles
{
    public class QuizProfile : Profile
    {
        public QuizProfile() 
        {
            CreateMap<QuizCreateRequest, Quiz>();
            CreateMap<Quiz, QuizCreateRequest>();
            CreateMap<QuizEditRequest, Quiz>();
            CreateMap<Quiz, QuizEditRequest>();
            CreateMap<Quiz, QuizViewModel>().ForMember(dest => dest.CreatorName, opt => opt.MapFrom(src => src.Creator.FirstName + " " + src.Creator.LastName))
                                            .ForMember(dest => dest.Questions, opt => opt.MapFrom(src => src.Questions));
            CreateMap<Quiz, QuizBaseModel>().ForMember(dest => dest.CreatorName, opt => opt.MapFrom(src => src.Creator.FirstName + " " + src.Creator.LastName));
            
            CreateMap<Question, QuestionCreateRequest>();
            CreateMap<QuestionCreateRequest, Question>();
            CreateMap<Question, QuestionViewModel>();
            CreateMap<QuestionEditRequest, Question>();
            

        }
    }
}
