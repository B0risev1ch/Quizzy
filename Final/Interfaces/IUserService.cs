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
    internal interface IUserService
	{
		public User? GetUserByUpdate(Update update);
		public void AddUserData(UserData userdata);

		public bool UserPresented(User user);

		public UserData? GetUserDataByUser(User user);

		public List<UserData> GetUsersList();
	}
}
