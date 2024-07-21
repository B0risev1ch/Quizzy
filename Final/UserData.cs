using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Final
{
    internal class UserData
    {
	    //public Dictionary<User, (Message, Chat)> UserDataTuples;

	    private User _user;
	    private Message _lastRecievedMessage;
	    private Chat _chat;
		private List<Quiz> _quizesList;

		/*
	    void NewUserData(User user, (Message, Chat) valueTuple)
	    {
			//userDataTuples.Add(user, valueTuple);
	    }

		*/

    }
}
