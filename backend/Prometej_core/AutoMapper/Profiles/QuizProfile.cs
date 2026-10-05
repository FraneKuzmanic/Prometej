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
            CreateMap<Quiz, QuizEditRequest>();
            CreateMap<Quiz, QuizViewModel>().ForMember(dest => dest.CreatorName, opt => opt.MapFrom(src => src.Creator.FirstName + " " + src.Creator.LastName))
                                            .ForMember(dest => dest.Questions, opt => opt.MapFrom(src => src.Questions))
                                            .ForMember(dest => dest.SourceTexts, opt => opt.Ignore());
            CreateMap<Quiz, QuizBaseModel>().ForMember(dest => dest.CreatorName, opt => opt.MapFrom(src => src.Creator.FirstName + " " + src.Creator.LastName))
                                            .ForMember(dest => dest.QuestionCount, opt => opt.Ignore());
            CreateMap<Quiz, CreatorQuizViewModel>().IncludeBase<Quiz, QuizBaseModel>()
                                                   .ForMember(dest => dest.QuizGameCount, opt => opt.Ignore());

            CreateMap<Question, QuestionCreateRequest>();
            // The content is set by the service, as a new object each time: the column is
            // compared by reference, so one filled in place would not be saved.
            CreateMap<QuestionCreateRequest, Question>().ForMember(dest => dest.Content, opt => opt.Ignore());
            CreateMap<Question, QuestionViewModel>();
            CreateMap<QuestionEditRequest, Question>().ForMember(dest => dest.Content, opt => opt.Ignore());
            CreateMap<SourceText, SourceTextViewModel>();

            CreateMap<QuizGame, QuizGameViewModel>();
            CreateMap<QuizGame, QuizGameReviewViewModel>().IncludeBase<QuizGame, QuizGameViewModel>()
                                                          .ForMember(dest => dest.PeriodId, opt => opt.MapFrom(src => src.Quiz.PeriodId))
                                                          .ForMember(dest => dest.PeriodName, opt => opt.MapFrom(src => src.Quiz.Period!.Name))
                                                          .ForMember(dest => dest.QuizIsListed, opt => opt.Ignore());
            CreateMap<Answer, AnswerViewModel>();

        }
    }
}
