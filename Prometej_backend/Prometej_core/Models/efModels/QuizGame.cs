using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.efModels
{
    public class QuizGame
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string UserName { get; set; }
        public int Score { get; set; }
        public DateTime DatePlayed { get; set; }
        public List<Answer> Answers { get; set; }
    }
}
