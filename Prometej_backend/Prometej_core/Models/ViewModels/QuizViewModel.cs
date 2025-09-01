using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.ViewModels
{
    public class QuizViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsPrivate { get; set; }
        public int CreatorId { get; set; }
        public string CreatorName { get; set; }
        public int? EntryCode { get; set; }
        public List<QuestionViewModel> Questions { get; set; }
    }
}
