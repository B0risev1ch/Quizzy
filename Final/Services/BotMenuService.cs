using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Final.Repository;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Final.Services
{
    internal class BotMenuService
    {
        private readonly ITelegramBotClient _bot;

        public InlineKeyboardMarkup MenuMarkup;
        public BotMenuService(ITelegramBotClient botClient)
        {
            _bot = botClient;
        }


        public async Task SendMenu(long userId, InlineKeyboardMarkup markup)
        {
            var text = "Меню:";

            await _bot.SendTextMessageAsync(
                userId,
                text,
                replyMarkup: markup
            );
        }

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


        public BotMenuService()
        {
            MenuMarkup = MainMenuMarkup;
        }



    }
}
