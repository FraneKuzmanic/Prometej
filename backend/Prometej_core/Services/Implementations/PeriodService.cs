using AutoMapper;
using Prometej_core.DataAccessLayer;
using Prometej_core.Exceptions;
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
                throw new NotFoundException("Period not found");
            }

            PeriodContentViewModel periodViewModel = _mapper.Map<PeriodContentViewModel>(periodEntity);

            return periodViewModel;
        }

        // The search reads the text of these elements, which is what the editor writes. Markup is
        // never matched, and text outside them (a table cell, a bare div) is not searched.
        private static readonly HashSet<string> HeadingTags = ["h1", "h2", "h3", "h4", "h5", "h6"];
        private static readonly HashSet<string> BlockTags = [.. HeadingTags, "p", "li", "blockquote", "pre"];
        private const int MaxPassagesPerPeriod = 3;
        private const int SnippetLength = 300;

        public List<PeriodSearchContentViewModel> SearchPeriodContent(string? query)
        {
            var foldedQuery = Fold(CleanText(query ?? ""));
            // One letter matches nearly every paragraph.
            if (foldedQuery.Length < 2)
            {
                return [];
            }

            // Every Period Content is loaded and searched in memory: there is no text to compare
            // until its HTML is parsed, and the Periods are a short, fixed list.
            var periodEntities = _periodRepository.ReadAll().OrderBy(p => p.PeriodId).ToList();

            var searchResults = new List<PeriodSearchContentViewModel>();
            foreach (var periodContent in periodEntities)
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(periodContent.Content);

                string? heading = null;
                var matchCount = 0;
                var passages = new List<PeriodSearchPassageViewModel>();
                foreach (var block in htmlDoc.DocumentNode.Descendants().Where(IsInnermostBlock))
                {
                    var text = TextOf(block);
                    if (text.Length == 0)
                    {
                        continue;
                    }

                    var isHeading = HeadingTags.Contains(block.Name);
                    if (isHeading)
                    {
                        heading = text;
                    }

                    // Folding keeps one character per character, so the index found in the
                    // folded text is the place of the match in the text itself.
                    var matchIndex = Fold(text).IndexOf(foldedQuery, StringComparison.Ordinal);
                    if (matchIndex < 0)
                    {
                        continue;
                    }

                    matchCount++;
                    if (passages.Count < MaxPassagesPerPeriod)
                    {
                        passages.Add(new PeriodSearchPassageViewModel
                        {
                            Heading = isHeading ? null : heading,
                            Text = Snippet(text, matchIndex, foldedQuery.Length),
                        });
                    }
                }

                if (matchCount > 0)
                {
                    searchResults.Add(new PeriodSearchContentViewModel
                    {
                        PeriodId = periodContent.PeriodId,
                        MatchCount = matchCount,
                        Passages = passages,
                    });
                }
            }

            return searchResults;
        }

        private static bool IsInnermostBlock(HtmlNode node) =>
            BlockTags.Contains(node.Name) && !node.Descendants().Any(d => BlockTags.Contains(d.Name));

        // The text a reader sees: inline markup dropped, entities decoded, a line break read as a space.
        private static string TextOf(HtmlNode block)
        {
            var raw = new StringBuilder();
            foreach (var node in block.Descendants())
            {
                if (node is HtmlTextNode textNode)
                {
                    raw.Append(textNode.Text);
                }
                else if (node.Name == "br")
                {
                    raw.Append(' ');
                }
            }

            return CleanText(HtmlEntity.DeEntitize(raw.ToString()));
        }

        // Composed characters and single spaces, so "š" is one character however it was typed.
        private static string CleanText(string text) =>
            string.Join(' ', text.Normalize(NormalizationForm.FormC).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        // Lower case without diacritics, one character for each character of the text.
        private static string Fold(string text) =>
            string.Create(text.Length, text, (folded, source) =>
            {
                for (var i = 0; i < source.Length; i++)
                {
                    folded[i] = FoldCharacter(source[i]);
                }
            });

        private static char FoldCharacter(char character)
        {
            // "đ" is a letter of its own in Unicode and does not decompose into "d" and a stroke.
            if (character is 'đ' or 'Đ')
            {
                return 'd';
            }

            var lower = char.ToLowerInvariant(character);
            if (lower < 128 || char.IsSurrogate(lower))
            {
                return lower;
            }

            return lower.ToString().Normalize(NormalizationForm.FormD)[0];
        }

        // A short text whole; otherwise the words around the match, cut at spaces.
        private static string Snippet(string text, int matchIndex, int matchLength)
        {
            if (text.Length <= SnippetLength)
            {
                return text;
            }

            var start = Math.Max(0, matchIndex - (SnippetLength - matchLength) / 2);
            var end = Math.Min(text.Length, start + SnippetLength);
            start = Math.Max(0, end - SnippetLength);

            if (start > 0)
            {
                var space = text.IndexOf(' ', start, matchIndex - start);
                start = space < 0 ? start : space + 1;
            }

            if (end < text.Length)
            {
                var matchEnd = matchIndex + matchLength;
                var space = text.LastIndexOf(' ', end - 1, end - matchEnd);
                end = space < 0 ? end : space;
            }

            return (start > 0 ? "…" : "") + text[start..end] + (end < text.Length ? "…" : "");
        }

        public int UpdatePeriodContent(PeriodContentEditRequest period)
        {
            // A Period has one content. Looking it up by PeriodId, not by the row id the client
            // sends, means a stale id can never overwrite another Period's content.
            var periodEntity = _periodRepository.ReadAll().FirstOrDefault(p => p.PeriodId == period.PeriodId);
            if (periodEntity == null)
            {
                var periodCreationEntity = new PeriodContent { PeriodId = period.PeriodId, Content = period.Content };
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
