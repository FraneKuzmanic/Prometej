using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.Base
{
    public class QuizBaseModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string CreatorName { get; set; }
        public bool IsPrivate { get; set; }
        public int? entryCode { get; set; }
    }
}
