using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Final
{
    internal class BotMenu
    {
        public InlineKeyboardMarkup GenerateMarkupFromQuizzes(List<Quiz> listQ)
        {
            var buttons = listQ
                .Select(k => new[] { InlineKeyboardButton.WithCallbackData(k.Name, k.Guid.ToString()) })
                .ToList();

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Назад", "QuizGeneratedMarkupBack") });

            return new InlineKeyboardMarkup(buttons);
        }


        public Dictionary<string, string> CommandDict = new()
        {

            {   @"New Quiz",  "NewQuizDataCallback" },

            {   @"My Quizzes",  "MyQuizzesDataCallback" },//TODO
		   
		    {   @"help", "HelpDataCallback"}
        };

        public InlineKeyboardMarkup NewQuizMenuMarkup
            = new(
                new[] {
                    new[] { InlineKeyboardButton.WithCallbackData("Prev", "NewQuizPrevQuestionDataCallBack") },
                    new[] { InlineKeyboardButton.WithCallbackData("Stop", "NewQuizStopEditingDataCallBack") },
                    new[] { InlineKeyboardButton.WithCallbackData("AddQuestion", "NewQuizAddQuestionDataCallBack") },
                }
            );

        public InlineKeyboardMarkup MainMenuMarkup
            = new(
                new[] {
                    new[] { InlineKeyboardButton.WithCallbackData("New Quiz", "NewQuizDataCallback") },
                    new[] { InlineKeyboardButton.WithCallbackData("My Quizzes", "MyQuizzesDataCallback") },
                    new[] { InlineKeyboardButton.WithCallbackData("Help", "HelpDataCallback") },
                }
            );

        public InlineKeyboardMarkup MenuMarkup;


        public BotMenu()
        {
            MenuMarkup = MainMenuMarkup;
        }



    }
}
