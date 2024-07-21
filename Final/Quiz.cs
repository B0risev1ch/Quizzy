using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /*
	    void FireQuiz(Guid quizGuid)
	    {

	    }
        */

        private Guid _guid;
        private string _name;
        private string _description;

        public List<Question> Questions = new List<Question>(); //

        public Quiz(string name, string description, List<Question> questions)
        {
            _guid = Guid.NewGuid();
            _name = name;
            _description = description;
            this.Questions = questions;
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

        public void ReCalculateQuestionTs()
        {
            TimeSpan totalTimeSpan = TimeSpan.Zero;

            foreach (Question question in Questions)
            {
                totalTimeSpan += question.GetTimeSpan();
                question.SetShiftedTimeSpan(totalTimeSpan);
            }
            Console.WriteLine(
                $"New questions timeStamps: {(string.Join(", ", Questions.Select(a => a.GetShiftedTimeSpan())))}");
        }

        void AddQuestion(Question question)
        {
	        Questions.Add(question);
            question.SetQuestionNumber(Questions.Count() + 1);
        }

        public void RemoveQuestion(Guid qGuid) { }

        public void Clear() { Questions.Clear(); }
        public Guid GetGuid() { return _guid; }
        public string GetName() { return _name; }

        public string GetQuizData()
        {
            return $"Guid: {_guid}; Name = {_name}. Questions = {Questions.Count()} ";
        }
    }

    internal class Question
    {
        private Guid _guid;
        private int _questionNumber;
        private MessageType _type;
        private string _questionData;
        private TimeSpan _timeToAnswer;
        private TimeSpan _shiftedTimeSpan;

        public Question(MessageType type, string questionData, TimeSpan timeToAnswer)
        {
            _guid = Guid.NewGuid();
            _type = type;
            _questionData = questionData;
            _timeToAnswer = timeToAnswer;
        }
        public Guid GetGuid() { return _guid; }
        public MessageType GetMessageType() { return _type; }

        public void SetTimeSpan(TimeSpan ts)
        { this._timeToAnswer = ts; }

        public void SetShiftedTimeSpan(TimeSpan ts)
        { this._shiftedTimeSpan = ts; }

        public TimeSpan GetTimeSpan()
        {
	        return _timeToAnswer;
        }
        public TimeSpan GetShiftedTimeSpan()
        {
	        return _shiftedTimeSpan;
        }

        public int GetQuestionNumber()
        {
	        return _questionNumber;
        }
        public void SetQuestionNumber(int questionNumber)
        {
	        _questionNumber = questionNumber;
        }


        public string GetQuestionData()
        {
            return _questionData;
        }
    }
}
