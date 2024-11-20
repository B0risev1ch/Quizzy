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

namespace Final.Repository
{
    internal class Quiz
    {

        [JsonInclude] public Guid Guid { get; private set; }
        [JsonInclude] public string Name { get; private set; }
        [JsonInclude] public string Description { get; private set; }
        [JsonInclude] public List<Question> Questions { get; set; }

        public string GetQuizData()
        {
            return $"Guid: {Guid}; Name = {Name}. Questions = {Questions.Count} ";
        }

        public Quiz(string name, string description, List<Question> questions)
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







        internal class Question(MessageType type, string questionData, TimeSpan timeToAnswer)
        {
            [JsonInclude] public Guid Guid { get; private set; } = Guid.NewGuid();

            [JsonInclude] public int QuestionNumber { get; private set; }
            [JsonInclude] public MessageType Type { get; private set; } = type;

            [JsonInclude] public string QuestionData { get; private set; } = questionData;

            [JsonInclude] public TimeSpan TimeToAnswer { get; private set; } = timeToAnswer;

            public string GetQuestionData()
            {
                return QuestionData;
            }

            public void SetTimeSpan(TimeSpan ts)
            {
                TimeToAnswer = ts;
            }

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
}
