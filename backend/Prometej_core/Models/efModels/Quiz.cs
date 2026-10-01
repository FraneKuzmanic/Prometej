using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.efModels
{
    public class Quiz
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public bool IsPrivate { get; set; }
        public int CreatorId { get; set; }
        public required User Creator { get; set; }
        public int? EntryCode { get; set; }
        public List<Question> Questions { get; set; }

    }
}
