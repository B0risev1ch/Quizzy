using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Final.Repository;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Final
{
    internal class UserService : IUserService
	{
		private List<UserData> _usersList = new List<UserData>();
		public User? GetUserByUpdate(Update update)
		{
			try
			{

				switch (update.Type)
				{
					case UpdateType.Message:
						return update.Message.From;

					case UpdateType.CallbackQuery:
						return update.CallbackQuery.From;

					case UpdateType.MyChatMember:
						return update.MyChatMember.From;

					//TODO дописать все case UpdateType.

				}
			}
			catch (Exception ee)
			{
				Console.WriteLine($"Не смог распознать пользователя по UpdateType = {update.Type}: \n {ee.Message}");
			}

			return null;
		}
		public void AddUserData(UserData userdata)
		{
			_usersList.Add(userdata);
		}
		public bool UserPresented(User user)
		{
			//Console.WriteLine($"UserData for {user} presented;");

			return GetUserDataByUser(user) != null;
		}
		public UserData? GetUserDataByUser(User user)
		{
			return _usersList.FirstOrDefault(a => a.User.Id == user.Id &&
			                                      a.User.Username == user.Username);
		}

		public List<UserData> GetUsersList() { return _usersList; }
	}
}
