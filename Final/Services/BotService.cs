using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Final.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using Final.Services;
using Final.Repository;


namespace Final
{
    internal class BotService : IHostedService
	{
		private readonly ITelegramBotClient _bot;
		private readonly IUserService _userService;
		private readonly IQuizService _quizService;
		private readonly BotMenuService _botMenuService;
		private readonly ILogger<BotService> _logger;
		private CancellationTokenSource? _cts;
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			_cts = new CancellationTokenSource();

			try
			{
				var me = await _bot.GetMeAsync(cancellationToken);

				_logger.LogInformation($"Бот {(me.Username)} запущен.");
			}
			catch (Exception e)
			{
				_logger.LogWarning($"Бот не запущен.");
				//return;
				throw;
			}

			_bot.StartReceiving(
				updateHandler: HandleUpdateAsync,
				pollingErrorHandler: HandleErrorAsync,
				cancellationToken: cancellationToken
			);

			_logger.LogDebug("Бот готов принимать сообщения");

			Console.ReadLine();
			_cts.CancelAsync();

		}
		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Bot service is stopping.");
			_cts?.Cancel();
			return Task.CompletedTask;
		}
		public BotService(ITelegramBotClient botClient, IUserService userService, ILogger<BotService> logger )
		{
			_bot = botClient;
			_userService = userService;
			_botMenuService = new BotMenuService(_bot);
			_logger = logger;
			_quizService = new QuizService(_bot, _userService, _logger);
		}

		public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
		{
			User? currentUser = _userService.GetUserByUpdate(update);

			if (!_userService.UserPresented(currentUser))
			{
				_userService.AddUserData(new UserData(currentUser));
			}

			Console.WriteLine($"Handling Update for {currentUser}. UpdateType is {update.Type}");

			switch (update.Type)
			{
				case UpdateType.Message:

					await HandleMessage(update.Message!);
					break;

				case UpdateType.CallbackQuery:

					await HandleButton(update.CallbackQuery!, _cts.Token);
					break;

				case UpdateType.MyChatMember:
					{
						await HandleMyChatMember(update, _userService.GetUserDataByUser(currentUser));
						break;
					}
				default:
					Console.WriteLine(update.Type);
					break;
			}
		}
		public async Task HandleErrorAsync(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
		{
			await Console.Error.WriteLineAsync(exception.ToString());
		}

		async Task HandleMessage(Message msg)
		{
			User currentUser = msg.From;
			var text = msg.Text ?? string.Empty;
			if (currentUser is null) return;
			_logger.LogInformation($"Handling Message from {currentUser}...");

			if (!_userService.UserPresented(currentUser))
			{
				_userService.AddUserData(new UserData(currentUser));
				_logger.LogInformation($"UserData for {currentUser} not presented. Added. Users: {(string.Join(", ", _userService.GetUsersList()))}");
			}
			UserData currentUserData = _userService.GetUserDataByUser(currentUser);
			if (currentUserData is null) return;

			_logger.LogInformation($"isWaitingForInput = {currentUserData.IsWaitingForInput}; isNewQuiz = {currentUserData.IsNewQuiz};" +
			                       $" isNewQuestion = {currentUserData.IsNewQuestion}; isNewQuestionNewTimeSpan = {currentUserData.IsNewQuestionNewTimeSpan}");


			if (currentUserData.IsWaitingForInput)
			{
				if (currentUserData.IsNewQuiz)
				{
					if (msg.Text != null)
					{
						var newQuiz = _quizService.CreateNewQuiz(msg);
						currentUserData.Quizzes.Add(newQuiz);
						currentUserData.EditingQuizGuid = newQuiz.Guid;
					}
					else
					{
						await _bot.SendTextMessageAsync(msg.Chat.Id, $"Название должно быть текстом");
						return;
					}
					currentUserData.IsNewQuiz = false;
				}

				TimeSpan ts = TimeSpan.Zero;

				if (currentUserData.IsNewQuestionNewTimeSpan && msg.Type == MessageType.Text)
				{
					Console.WriteLine(currentUserData.Quizzes.First(a => a.Guid == currentUserData.EditingQuizGuid).Name);
					var seconds = 0.0;

					if (!Double.TryParse(msg.Text, out seconds))
					{
						await _bot.SendTextMessageAsync(msg.Chat.Id, $"Формат не распознан. Введите количество секунд на ответ");
						return;
						////TODO: default value from config 
						seconds = 120.0;
					}

					ts = TimeSpan.FromSeconds(seconds);

					var curreentQuiz = currentUserData.Quizzes.First(a => a.Guid == currentUserData.EditingQuizGuid);

					curreentQuiz.Questions.Last().SetTimeSpan(ts);

					Console.WriteLine("\nTOTAL : " + curreentQuiz.GetTotalTimeSpan() + "\n");

					Console.WriteLine($"question data: {currentUserData.Quizzes.First(a => a.Guid == currentUserData.EditingQuizGuid).Questions.Last().GetQuestionData()}");

					//isNewQuestionNewTimeSpan = false;
					currentUserData.IsWaitingForInput = false;
				}

				if (currentUserData.IsNewQuestion)
				{
					_quizService.CreateNewQuestion(msg);
					currentUserData.IsNewQuestion = false;
					currentUserData.IsNewQuestionNewTimeSpan = true;
					await _bot.SendTextMessageAsync(msg.Chat, "Время на ответ:");
				}

				if (!currentUserData.IsWaitingForInput && currentUserData.IsNewQuestionNewTimeSpan)
				{
					currentUserData.IsNewQuestionNewTimeSpan = false;
					Task.Run(async () =>
					{
						await _bot.SendTextMessageAsync(msg.Chat.Id, $"Вопрос добавлен. " +
																	$"{currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).Questions.Last().GetQuestionData()}." +
																	$" Всего вопросов = {currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).Questions.Count()}",
							replyMarkup: _botMenuService.NewQuizMenuMarkup);
					});
				}
			}

			Console.WriteLine($"{currentUser.FirstName} [Id: {currentUser.Id}] sent {msg.Type} {text}");

			if (text.StartsWith("/"))
			{
				await HandleCommand(currentUser.Id,
					ParseCommandAndAttribute(text).command,
					ParseCommandAndAttribute(text).attribute ?? "null");
			}
		}

		async Task HandleCommand(long userId, string command, string? message)
		{
			switch (command)
			{
				case "/start":
					_logger.LogInformation($"Sending Menu to {userId}... _botMenuService = {_botMenuService}");
					await _botMenuService.SendMenu(userId, _botMenuService.MainMenuMarkup);
					break;
			}

			await Task.CompletedTask;
		}

		async Task HandleButton(CallbackQuery query,
			CancellationToken cancellationToken)
		{

			User currentUser = query.From;
			UserData currentUserData = _userService.GetUserDataByUser(currentUser);
			if (currentUserData is null)
			{
				return;
			}

			Console.WriteLine($"users Count = {_userService.GetUsersList().Count()};\n" +
							  $"currentUser = {currentUser};\n" +
							  $"currentUserData.User = {_userService.GetUserDataByUser(currentUser)}");

			Console.WriteLine(query.Data);

			string text = "";

			InlineKeyboardMarkup markup = new(Array.Empty<InlineKeyboardButton>());

			var quizGuid = Guid.Empty;

			if (Guid.TryParse(query.Data,
					out quizGuid) &&
				currentUserData.Quizzes.Any())
			{
				try
				{
					Chat chat = await _bot.GetChatAsync(currentUserData.ChatId,
						cancellationToken);
					string chatTitle = chat.Title;

					text = $"Quiz data: {currentUserData.Quizzes.First(a => a.Guid == quizGuid).GetQuizData()}";

					markup = new InlineKeyboardMarkup(new[]
					{
				InlineKeyboardButton.WithCallbackData("Назад",
					"QuizGeneratedMarkupBack"),
				InlineKeyboardButton.WithCallbackData($"🔥fire! Целимся в\n{chatTitle}",
					$"fireQuiz_{quizGuid}"),
			});
				}
				catch (ApiRequestException e)
				{
					Console.WriteLine("ChatId = 0?\n" + e);
					currentUserData.NoAdminMessage = await _bot.SendTextMessageAsync(query.Message.Chat,
						"Я пока нигде не админ, удоли и добавь в нужную группу для старта квизов");
				}
			}

			Console.WriteLine($"query data = {query.Data}; " +
							  $"quizGuid = {quizGuid};" +
							  $" armedToFire = {(currentUserData.ArmedToFire == null ? "none" : currentUserData.ArmedToFire.ToString())}");

			if (string.Equals(query.Data,
					quizGuid.ToString(),
					StringComparison.InvariantCulture))
			{
				currentUserData.ArmedToFire = quizGuid;
				Console.WriteLine($"Заряжен квиз с GUID = {currentUserData.ArmedToFire}!");
			}

			if (query.Data == $"fireQuiz_{currentUserData.ArmedToFire}")
			{
				Chat chat = await _bot.GetChatAsync(currentUserData.ChatId,
					cancellationToken);
				string chatTitle = chat.Title;
				Console.WriteLine($"FIRED");

				if (currentUserData.ArmedToFire != null)
				{
					Task.Run(async () => await _quizService.FireQuiz(currentUserData.ArmedToFire,
						currentUserData
						));
					text = $"Выстрелили квизом с GUID = {currentUserData.ArmedToFire} в чятик {chatTitle}!";
				}

			}

			switch (query.Data)
			{
				case "NewQuizDataCallback":
					text = "Создаём новый квиз.\nИмя квиза:";
					markup = new(Array.Empty<InlineKeyboardButton>());
					currentUserData.IsNewQuiz = true;
					currentUserData.IsWaitingForInput = true;
					break;

				case "NewQuizAddQuestionDataCallBack":
					text = "Новый вопрос (text or photo or video):";
					markup = new(Array.Empty<InlineKeyboardButton>());
					currentUserData.IsNewQuestion = true;
					currentUserData.IsWaitingForInput = true;
					break;

				case "HelpDataCallback":
					text = "Меню:";
					markup = _botMenuService.MainMenuMarkup;
					break;

				case "NewQuizStopEditingDataCallBack":
					if (!currentUserData.Quizzes.Any())
						break;


					markup = _botMenuService.MainMenuMarkup;
					currentUserData.IsNewQuestion = false;
					currentUserData.IsWaitingForInput = false;
					Quiz quiz = currentUserData.Quizzes.First(a => a.Guid == currentUserData.EditingQuizGuid);

					FileHelper.SaveQuizToFile(quiz,
						Path.Combine(currentUser.Username,
							$"{quiz.Guid}_{quiz.Name}_{DateTime.Now.TimeOfDay.ToString().Split('.')[0].Replace(':', '-')}.json"));

					text = "Quiz saved!";
					break;

				case "MyQuizzesDataCallback":
					text = "Мои квизы:";
					currentUserData.Quizzes.Clear();

					var path = currentUser.Username;

					if (Directory.Exists(path))
					{
						var files = Directory.EnumerateFiles(path);

						foreach (var file in files)
						{
							currentUserData.Quizzes.Add(FileHelper.LoadQuizFromFile(file));
							Console.WriteLine($"{file} added to list Of Quizzes");
						}
					}
					else
					{
						text = "Нет квизов.\nМеню:";
						Console.WriteLine($"Directory {path} does not exist.");
					}

					markup = _botMenuService.GenerateMarkupFromQuizzes(currentUserData.Quizzes);

					break;
				case "QuizGeneratedMarkupBack":
					text = "Меню:";
					markup = _botMenuService.GenerateMarkupFromQuizzes(currentUserData.Quizzes);
					break;


			}


			try
			{
				await _bot.AnswerCallbackQueryAsync(query.Id);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			try
			{
				//_logger.LogInformation(markup.ToString());

				await _bot.EditMessageTextAsync(
					query.Message!.Chat.Id,
					query.Message.MessageId,
					text,
					ParseMode.Html,
					replyMarkup: markup
				);
			}
			catch (ApiRequestException apiRequestException)
			{
				Console.WriteLine(apiRequestException.Message);
			}
		}
		async Task HandleMyChatMember(Update update1, UserData? userData)
		{
			TelegramBotClient telegramBotClient;
			if (update1.MyChatMember != null)
			{
				Console.WriteLine($"ChatMemberUpdate : " +
								  $"\nChatType: {update1.MyChatMember.Chat.Type},\n" +
								  $"ChatId: {update1.MyChatMember.Chat.Id},\n" +
								  $"ChatMember: {(update1.ChatMember == null ? "null" : "not null")}\n" +
								  $"MyChatMember old {(update1.MyChatMember.OldChatMember)} new {(update1.MyChatMember.NewChatMember)}");

				userData.ChatId = update1.MyChatMember.Chat.Id;

				if (update1.MyChatMember.NewChatMember is ChatMemberAdministrator)
					if (userData.NoAdminMessage != null)
					{
						await _bot.EditMessageTextAsync(
							userData.NoAdminMessage!.Chat.Id,
							userData.NoAdminMessage.MessageId,
							$"Я теперь админ в {update1.MyChatMember.Chat.Type} '{update1.MyChatMember.Chat.Title}'",
							ParseMode.Html
						);
					}

				if (update1.MyChatMember.NewChatMember is ChatMemberLeft)
				{
					if (userData.NoAdminMessage != null)
					{
						await _bot.EditMessageTextAsync(
							userData.NoAdminMessage!.Chat.Id,
							userData.NoAdminMessage.MessageId,
							$"Я удалился из {update1.MyChatMember.Chat.Type} '{update1.MyChatMember.Chat.Title}'",
							ParseMode.Html
						);
					}
				}
			}
		}
		(string? command, string? attribute) ParseCommandAndAttribute(string? inputLine)
		{
			Console.WriteLine($"Parsing command");
			if (inputLine != null)
			{
				var commandLastIndex = inputLine.IndexOf(' ') > 0 ? inputLine.IndexOf(' ') : inputLine.Length;

				var command = inputLine[..commandLastIndex];

				var attribute = commandLastIndex + 1 < inputLine.Length ?
					inputLine.Substring(commandLastIndex + 1, inputLine.Length - commandLastIndex - 1)
					: null;
				return (command, attribute);
			}
			else
				return (null, null);
		}
	}

}
