using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Final.Repository
{

    class Users
    {

        private List<UserData> _usersList = new List<UserData>();

        public Users()
        {

        }



        public void RemoveUserData(UserData userdata)
        {

        }

        public bool ContainsUserData(UserData userdata)
        {
            return _usersList.Contains(userdata);
        }
        public List<UserData> GetUsersList() { return _usersList; }
        public void Clear() { _usersList = new List<UserData>(); }



    }

    internal class UserData
    {
        public UserData(User user)
        {
            User = user;
        }

        public long ChatId = 0; //TODO: THIS IS BOGUS! Add to list of chats where bot is Admin / has sending messages permissions

        public User User { get; set; }
        public bool IsWaitingForInput { get; set; } = false;
        public bool IsNewQuestion { get; set; } = false;
        public bool IsNewQuiz { get; set; } = false;
        public bool IsNewQuestionNewTimeSpan { get; set; } = false;
        public Guid EditingQuizGuid { get; set; } = Guid.NewGuid();
        public Guid? ArmedToFire { get; set; } = Guid.NewGuid();
        public List<Quiz> Quizzes { get; set; } = new();
        public Message NoAdminMessage { get; set; } = null;

    }
}
