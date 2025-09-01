using AutoMapper;
using Prometej_core.DataAccessLayer;
using Prometej_core.Models.efModels;
using Prometej_core.Models.Requests.Period;
using Prometej_core.Models.ViewModels;
using Prometej_core.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Prometej_core.Services.Implementations
{
    public class PeriodService: IPeriodService
    {
        private readonly IMapper _mapper;
        private readonly IRepository<PeriodContent> _periodRepository;
        public PeriodService(IMapper mapper, IRepository<PeriodContent> periodRepository)
        {
            _mapper = mapper;
            _periodRepository = periodRepository;
        }

        public PeriodContentViewModel GetPeriodContent(int id)
        {
            var periodEntity = _periodRepository.ReadAll().FirstOrDefault(p => p.PeriodId == id);
            if (periodEntity == null)
            {
                throw new Exception("Period not found");
            }

            PeriodContentViewModel periodViewModel = _mapper.Map<PeriodContentViewModel>(periodEntity);

            return periodViewModel;
        }

        public List<PeriodSearchContentViewModel> SearchPeriodContent(string query)
        {
            List<PeriodSearchContentViewModel> searchResults = new List<PeriodSearchContentViewModel>();

            var periodEntities = _periodRepository.ReadAll().ToList();
            List<PeriodContentViewModel> periodViewModels = _mapper.Map<List<PeriodContentViewModel>>(periodEntities);

            foreach (PeriodContentViewModel periodContent in periodViewModels)
            {
                if (periodContent.Content.Contains(query))
                {
                    string searchContent = ConstructSearchContent(periodContent.Content, query);

                    PeriodSearchContentViewModel searchResult = new PeriodSearchContentViewModel
                    {
                        PeriodId = periodContent.PeriodId,
                        SearchContent = searchContent
                    };

                    searchResults.Add(searchResult);
                }

            }

                return searchResults;

        }

        public string ConstructSearchContent(string content, string query)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);

            // Find the paragraph (<p>) tag containing the query
            HtmlNode paragraphNode = htmlDoc.DocumentNode.SelectSingleNode($"//p[contains(text(), '{query}')]");

            if (paragraphNode != null)
            {
                // Find the closest preceding h2 tag
                HtmlNode h2Tag = paragraphNode.SelectSingleNode("preceding-sibling::h2");

                if (h2Tag != null)
                {
                    // Return the outer HTML of both the h2 tag and the paragraph
                    return h2Tag.OuterHtml + paragraphNode.OuterHtml;
                }
                else
                {
                    // If no preceding h2 tag is found, return just the outer HTML of the paragraph
                    return paragraphNode.OuterHtml;
                }
            }

            // If the paragraph containing the query is not found, return an empty string
            return string.Empty;
        }

        public int UpdatePeriodContent(PeriodContentEditRequest period)
        {
            var periodEntity = _periodRepository.ReadAll().FirstOrDefault(p => p.Id == period.Id);
            if (periodEntity == null)
            {
                var periodCreationEntity = _mapper.Map<PeriodContent>(period);
                _periodRepository.Create(periodCreationEntity);
                _periodRepository.Save();

                return periodCreationEntity.Id;
            }

            periodEntity.Content = period.Content;

            _periodRepository.Update(periodEntity);
            _periodRepository.Save();

            return periodEntity.Id;
        }
    }
}
