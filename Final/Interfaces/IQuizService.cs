using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Final.Repository;
using Telegram.Bot.Types;
using static Final.Repository.Quiz;

namespace Final.Interfaces
{
    internal interface IQuizService
	{
		public Quiz CreateNewQuiz(Message quizMessage);
		public Question CreateNewQuestion(Message quizMessage);
		public Task FireQuiz(Guid? quizGuid, UserData userData);
	}
}
