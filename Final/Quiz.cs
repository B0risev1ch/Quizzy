using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Final
{
    internal class Quiz 
    {
        [JsonInclude]
        public Guid Guid { get; private set; }
        [JsonInclude]
        public string Name { get; private set; }
        [JsonInclude]
        public string Description { get; private set; }
        [JsonInclude]
        public List<Question> Questions { get; set; }

        private static TelegramBotClient bot;


        //public Guid GetGuid() { return Guid; }
        //public string GetName() { return Name; }

        public string GetQuizData()
        {
	        return $"Guid: {Guid}; Name = {Name}. Questions = {Questions.Count} ";
        }

        public Quiz(string name, string description, List<Question> questions, TelegramBotClient bot)
        {
            Guid = Guid.NewGuid();
            Name = name;
            Description = description;
            Questions = questions;
        }

        public TimeSpan GetTotalTimeSpan()
        {
            TimeSpan totalTimeSpan = TimeSpan.Zero;
            foreach (Question question in Questions)
            {
                totalTimeSpan += question.GetTimeSpan();
            }
            return totalTimeSpan;
        }

        public void AddQuestion(Question question)
        {
            Questions.Add(question);
            question.SetQuestionNumber(Questions.Count + 1);
        }

        public void RemoveQuestion(Guid qGuid) { }

        public void Clear() { Questions.Clear(); }



        async public static Task FireQuiz(Guid? quizGuid, UserData userData)
        {

            if (quizGuid is null)
            {
                Console.WriteLine($"Nothing armed to fire for user = {userData.User}");
                return;
            }
            Quiz quiz = userData.Quizzes.First(a => a.Guid == quizGuid);
            if (userData.ChatId != 0)
                await SendQuestionsSequentially(userData.ChatId, quiz.Questions);
            else
            {
                Console.WriteLine("No Chats found with Administrative role/permissions to send messages");
            }

        }

        async static Task SendQuestionsSequentially(long chatId, IEnumerable<Question> questions)
        {
            foreach (var question in questions)
            {
                await SendQuestion(chatId, question);
                await Task.Delay(question.GetTimeSpan());
            }
        }

        async static Task SendQuestion(long chatId, Question question) //отправляем в чятик
        {

            Task.Run(async () =>
            {
                if (question.Type is MessageType.Text)
                {
                    await bot.SendTextMessageAsync(chatId, question.GetQuestionData());
                    //Todo вынести в config отправлять ли в чят время на ответ
                    //await bot.SendTextMessageAsync(chatId, "Время на ответ: " + question.GetTimeSpan().ToString());
                }
                if (question.Type is MessageType.Photo)
                {
                    await bot.SendPhotoAsync(chatId, new InputFileId(question.GetQuestionData()));
                    //await bot.SendTextMessageAsync(chatId, "Время на ответ: " + question.GetTimeSpan().ToString());
                }
                if (question.Type is MessageType.Video)
                {
                    await bot.SendVideoAsync(chatId, new InputFileId(question.GetQuestionData()));
                    //await bot.SendTextMessageAsync(chatId, "Время на ответ: " + question.GetTimeSpan().ToString());
                }
            });
        }
    }

    internal class Question(MessageType type, string questionData, TimeSpan timeToAnswer)
    {
        [JsonInclude]
        public Guid Guid { get; private set; } = Guid.NewGuid();

        [JsonInclude]
        public int QuestionNumber { get; private set; }
        [JsonInclude]
        public MessageType Type { get; private set; } = type;

        [JsonInclude]
        public string QuestionData { get; private set; } = questionData;

        [JsonInclude]
        public TimeSpan TimeToAnswer { get; private set; } = timeToAnswer;

        public string GetQuestionData()
        {
	        return QuestionData;
        }

        public void SetTimeSpan(TimeSpan ts)
        { TimeToAnswer = ts; }
        
        public TimeSpan GetTimeSpan()
        {
            return TimeToAnswer;
        }
        public void SetQuestionNumber(int questionNumber)
        {
	        QuestionNumber = questionNumber;
        }


    }

}
