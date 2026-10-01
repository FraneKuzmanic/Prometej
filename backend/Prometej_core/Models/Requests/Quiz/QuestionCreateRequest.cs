using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.Requests.Quiz
{
    public class QuestionCreateRequest
    {
        [Required, StringLength(500)]
        public required string QuestionTitle { get; set; }
        [Required, StringLength(500)]
        public required string FirstAnswer { get; set; }
        [Required, StringLength(500)]
        public required string SecondAnswer { get; set; }
        [Required, StringLength(500)]
        public required string ThirdAnswer { get; set; }
        [Required, StringLength(500)]
        public required string FourthAnswer { get; set; }
        // Which of the four answers is the correct one.
        [Range(1, 4)]
        public int CorrectOption { get; set; }
        [StringLength(1000)]
        public string? HintText { get; set; }
        [StringLength(1000)]
        public string? ExploreMore { get; set; }
    }
}
