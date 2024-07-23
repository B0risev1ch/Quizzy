using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		    // {   @"/delay", "DelayDataCallback" },
		    {   @"New Quiz",  "NewQuizDataCallback" },
			//  {   @"/start",  "to get the information about this app and assemblies" },
		    {   @"My Quizzes",  "MyQuizzesDataCallback" },//TODO
		   // {   @"/exit",  "to exit" },
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
            /*
            var buttons = commandDict.Select(kvp => InlineKeyboardButton.WithCallbackData(kvp.Key, kvp.Value)).ToList();

            // Разделение кнопок на два массива
            var half = (buttons.Count + 1) / 2;
            var firstRow = buttons.Take(half).ToArray();
            var secondRow = buttons.Skip(half).ToArray();

            MenuMarkup =  new InlineKeyboardMarkup(new[]
            {
                firstRow,
                secondRow
            });*/

            /*
		    IEnumerable<KeyboardButton> buttons = new List<KeyboardButton>();

		    foreach (var key in commandDict.Keys)
		    {
			    buttons.Append(commandDict[key]);
	        }
			*/

        }
    }
}
