using System.Diagnostics;
using Final;
using System.Drawing;
using TdLib;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
//using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


var chatId = -1002204194032; //TODO: THIS IS BOGUS! How to get a chatId? 

BotMenu botMenu = new BotMenu();

//user:
UserData userData = new UserData();
//:
bool isWaitingForInput = false;
bool isNewQuestion = false;
bool isNewQuiz = false;
bool isNewQuestionNewTimeSpan = false;
Guid editingQuizGuid = Guid.NewGuid();
Guid armedToFire = Guid.NewGuid();
List<Quiz> quizzes = new();

var fileHelper = new FileHelper("quiz.json");

//List<UserData> userDataList = new();

Dictionary<User, Message> lastMessagesRecievedFromUsers = new Dictionary<User, Message>();

var bot = new TelegramBotClient("7367447150:AAFHBjaG_Hwak_CM2v6FhHY6EL36J--Mq44");
using var cts = new CancellationTokenSource();

//Message lastMessageRecieved = null;



// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool, so we use cancellation token
bot.StartReceiving(
	updateHandler: HandleUpdate,
	cancellationToken: cts.Token,
	pollingErrorHandler: HandleError
);

// Tell the user the bot is online
Console.WriteLine("Start listening for updates. Press enter to stop");

Console.ReadLine();

// Send cancellation request to stop the bot
cts.Cancel();

// Each time a user interacts with the bot, this method is called
async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
{
	switch (update.Type)
	{
		// A message was received
		case UpdateType.Message:

			await HandleMessage(update.Message!);

			break;

		case UpdateType.CallbackQuery:
			await HandleButton(update.CallbackQuery!, cts.Token);
			break;

		case UpdateType.MyChatMember:
			{
				if (update.MyChatMember != null)
				{
					Console.WriteLine($"ChatMemberUpdate : " +
					                  $"\nChatType: {update.MyChatMember.Chat.Type},\n" +
					                  $" ChatId: {update.MyChatMember.Chat.Id} ");
					chatId = update.MyChatMember.Chat.Id;
				}

				break;
			}
		default:
			Console.WriteLine(update.Type);
			//cts.Cancel();
			break;
	}
}

async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
{
	await Console.Error.WriteLineAsync(exception.ToString());
}


async Task HandleMessage(Message msg)
{
	
	Message newQuizQuestionMessage;
	
	var user = msg.From;
	var text = msg.Text ?? string.Empty;

	if (user is null)
		return;

	/*
	if (!userDataList.Where(a => a.).Contains(user))
	{
		userDataTuples.Add(user, (msg, msg.Chat));
		Console.WriteLine($"user = {user} was not presented. Added. Total users = {userDataTuples.Count()}");
	}

	*/
	// if(isWaitingForInputFrom(user))
	/*  lastMessagesRecievedFromUsers.Where(a => a.UserId).First()
	 *
	 */

	Console.WriteLine($"isWaitingForInput = {isWaitingForInput}; isNewQuiz = {isNewQuiz}; isNewQuestion = {isNewQuestion}; isNewQuestionNewTimeSpan ={isNewQuestionNewTimeSpan}");

	if (isWaitingForInput)
	{
		//lastMessageRecieved = msg;

		if (isNewQuiz)
		{
			if (msg.Text != null)
			{
				var newQuiz = CreateNewQuiz(msg);
				quizzes.Add(newQuiz);
				editingQuizGuid = newQuiz.GetGuid();

			}

			isNewQuiz = false;
		}

		TimeSpan ts = TimeSpan.Zero;

		if (isNewQuestionNewTimeSpan)
		{
			//updateTimeSpan

			Console.WriteLine(quizzes.First(a => a.GetGuid() == editingQuizGuid).GetName());
			var seconds = 0.0;

			if (!Double.TryParse(msg.Text, out seconds))
			//TODO: default value
			{ seconds = 120.0; }

			ts = TimeSpan.FromSeconds(seconds);

			var curreentQuiz = quizzes.First(a => a.GetGuid() == editingQuizGuid);

			curreentQuiz.Questions.Last().SetTimeSpan(ts);

			Console.WriteLine("\nTOTAL : " + curreentQuiz.GetTotalTimeSpan() + "\n");

			Console.WriteLine($"question data: {quizzes.First(a => a.GetGuid() == editingQuizGuid).Questions.Last().GetQuestionData()}");

			//isNewQuestionNewTimeSpan = false;
			isWaitingForInput = false;
		}

		if (isNewQuestion)
		{
			CreateNewQuestion(msg);
			isNewQuestion = false;
			isNewQuestionNewTimeSpan = true;
			await bot.SendTextMessageAsync(msg.Chat, "Время на ответ:");
		}

		if (!isWaitingForInput && isNewQuestionNewTimeSpan)
		{
			isNewQuestionNewTimeSpan = false;
			Task.Run(async () =>
			{
				await bot.SendTextMessageAsync(msg.Chat.Id, $"Question Added. {quizzes.First(quiz => quiz.GetGuid() == editingQuizGuid).Questions.Last().GetQuestionData()}. Total = {quizzes.First(quiz => quiz.GetGuid() == editingQuizGuid).Questions.Count()}", replyMarkup: botMenu.NewQuizMenuMarkup);
			});
		}
	}

	Console.WriteLine($"{user.FirstName} [Id: {user.Id}] sent {msg.Type} {text}");

	/*

	if (msg.Type is MessageType.Photo)
	{
		var photo = msg.Photo;

		if (photo != null)
		{
			if (isNewQuestion)
			{
				//var newQuizQuestion = CreateNewQuestion(msg);
				isNewQuestion = false;
			}

			var photoFileId = photo[photo.Count() - 1].FileId;
			Console.WriteLine($"PhotoFileID = {photoFileId}");
			//await bot.SendPhotoAsync(CHAT_ID, new InputFileId(photoFileId));
		}
		//await bot.SendPhotoAsync(CHAT_ID, msg.Photo );
	}

	*/
	// When we get a command, we react accordingly
	if (text.StartsWith("/"))
	{
		(string? command, string? attribute) userInput;
		userInput = ParseCommandAndAttribute(text);
		var command = userInput.command;
		var message = userInput.attribute ?? "null";

		await HandleCommand(user.Id, command, message);
	}
}

(string? command, string? attribute) ParseCommandAndAttribute(string? inputLine)
{
	if (inputLine != null)
	{
		var commandLastIndex = inputLine.IndexOf(' ') > 0 ? inputLine.IndexOf(' ') : inputLine.Length;

		var command = inputLine[..commandLastIndex];

		var attribute = commandLastIndex + 1 < inputLine.Length ?
			inputLine.Substring(commandLastIndex + 1, inputLine.Length - commandLastIndex - 1)
			: null;
		//var commandList = botMenu.commandDict.Keys;
		return (command, attribute);
	}
	else
		return (null, null);
}

async Task SendQuestion(long chatId, Question question) //отправляем в чятик
{
	var messageType = question.GetMessageType();

	Task.Run(async () =>
	{
		if (messageType is MessageType.Text)
		{
			await bot.SendTextMessageAsync(chatId, question.GetQuestionData());
			await bot.SendTextMessageAsync(chatId, "Время на ответ: " + question.GetTimeSpan().ToString());
		}
		if (messageType is MessageType.Photo)
		{
			await bot.SendPhotoAsync(chatId, new InputFileId(question.GetQuestionData()));
			await bot.SendTextMessageAsync(chatId, "Время на ответ: " + question.GetTimeSpan().ToString());
		}
	});
}
async Task HandleCommand(long userId, string command, string? message)
{
	switch (command)
	{
		//TODO: JUST FOR TESTING!
		case "/delay":

			System.TimeSpan delay = new TimeSpan(0, 0, 5);

			//if (message != null)
			//SendTextMessageWithDelayAsync(message, delay);

			break;


		case "/start":
			await SendMenu(userId);
			break;
	}

	await Task.CompletedTask;
}

Quiz CreateNewQuiz(Message quizMessage)
{
	Quiz newQuiz = new Quiz(quizMessage.Text, string.Empty, new List<Question>());
	Task.Run(async () =>
	{
		await bot.SendTextMessageAsync(quizMessage.Chat.Id, "ok, so be it '" + quizMessage.Text + $"'\nNew quiz Guid = {newQuiz.GetGuid()}",
			replyMarkup: botMenu.NewQuizMenuMarkup);
	});

	isWaitingForInput = false;

	return newQuiz;
}

Question CreateNewQuestion(Message quizMessage)
{
	string questionData = string.Empty;
	Question question;


	if (quizMessage.Type == MessageType.Photo)
	{
		var photo = quizMessage.Photo;
		questionData = photo[photo.Count() - 1].FileId;
	}
	if (quizMessage.Type == MessageType.Text)
		questionData = quizMessage.Text;

	question = new Question(quizMessage.Type, questionData, TimeSpan.Zero);

	quizzes.First(quiz => quiz.GetGuid() == editingQuizGuid).Questions.Add(question);

	Console.WriteLine($"Question {question.GetGuid()} added to {quizzes.First(quiz => quiz.GetGuid() == editingQuizGuid).GetName()}, TotalTimeSpan = {quizzes.First(quiz => quiz.GetGuid() == editingQuizGuid).GetTotalTimeSpan()}");

	/*
	Quiz newQuiz = new Quiz(quizMessage.Text, string.Empty, new List<Question>());

	
	*/
	//throw new NotImplementedException();

	return question;
}


async Task SendMenu(long userId)
{
	var text = "Menu:";
	var markup = botMenu.MenuMarkup;

	await bot.SendTextMessageAsync(
		userId,
		text,
		replyMarkup: markup
	);
}
async Task FireQuiz(Guid quizGuid)
{
	Quiz quiz = quizzes.First(a => a.GetGuid() == quizGuid);
	await SendQuestionsSequentially(chatId, quiz.Questions);

}
async Task SendQuestionsSequentially(long chatId, IEnumerable<Question> questions)
{
	foreach (var question in questions)
	{
		await SendQuestion(chatId, question);
		await Task.Delay(question.GetTimeSpan());
	}
}

async Task HandleButton(CallbackQuery query, CancellationToken cancellationToken)
{
	Console.WriteLine(query.Data);

	string text = "";

	InlineKeyboardMarkup markup = new(Array.Empty<InlineKeyboardButton>());

	var quizGuid = Guid.Empty;

	if (Guid.TryParse(query.Data, out quizGuid) && quizzes.Any())
	{
		text = $"Quiz data: {quizzes.First(a => a.GetGuid() == quizGuid).GetQuizData()}";
		markup = new InlineKeyboardMarkup(new[]
		{
			InlineKeyboardButton.WithCallbackData("back", "QuizGeneratedMarkupBack") ,
			InlineKeyboardButton.WithCallbackData("🔥fire!", $"fireQuiz_{quizGuid}") ,
		});
	}

	Console.WriteLine($"query data = {query.Data}; quizGuid = {quizGuid}; armedToFire = {armedToFire}");

	if (string.Equals(query.Data, quizGuid.ToString(), StringComparison.InvariantCulture))
	{
		armedToFire = quizGuid;
		Console.WriteLine($"armed {armedToFire}!");
	}

	if (query.Data == $"fireQuiz_{armedToFire}")
	{
		Console.WriteLine($"FIRED");
		text = "fired";

		Task.Run(async () => await FireQuiz(armedToFire));

	}
    switch (query.Data)
	{
		case "DelayDataCallback":
			text = "Delay Message:";
			Console.WriteLine($"delay button pressed");
			await bot.SendTextMessageAsync(query.Message.Chat.Id, "Enter message to deliever:");
			//markup = botMenu.GenerateMarkupFromQuizzes(quizzes);
			isWaitingForInput = true;
			return;

		case "NewQuizDataCallback":
			text = "Ok, let's create a new Quiz!\nName of the quiz, please:";
			markup = new(Array.Empty<InlineKeyboardButton>());
			isNewQuiz = true;
			isWaitingForInput = true;
			break;

		case "NewQuizAddQuestionDataCallBack":
			{
				text = "New question (text or photo):";
				markup = new(Array.Empty<InlineKeyboardButton>());
				isNewQuestion = true;
				isWaitingForInput = true;
				break;
			}

		case "HelpDataCallback":
			text = "Menu:";
			markup = botMenu.MenuMarkup;
			break;

		case "NewQuizStopEditingDataCallBack":
			if (!quizzes.Any()) 
				break;
			text = "Quiz saved!";
			//Quiz quiz = quizzes.First(a => a.GetGuid() == editingQuizGuid);
            quizzes.First(a => a.GetGuid() == editingQuizGuid).ReCalculateQuestionTs();
			await fileHelper.SaveQuizzesAsync(quizzes);
            markup = botMenu.MenuMarkup;
			isNewQuestion = false;
			isWaitingForInput = false;
			break;

		case "MyQuizzesDataCallback":
			text = "Quizzes:";

			fileHelper = new FileHelper("quizzes.json");
            var loadedQuizzes = await fileHelper.LoadQuizzesAsync();
			foreach (var quiz in loadedQuizzes)
			{
				Console.WriteLine($"Loaded Quiz: {quiz.GetName()}, totalTimeSpan: {quiz.GetTotalTimeSpan()}");
			}

			//quizzes = loadedQuizzes;

            markup = botMenu.GenerateMarkupFromQuizzes(quizzes);
			//isNewQuestion = false;
			//isWaitingForInput = false;
			break;
		case "QuizGeneratedMarkupBack":
			text = "Menu:";
			markup = botMenu.MenuMarkup;
			//isNewQuestion = false;
			//isWaitingForInput = false;
			break;
	}


	// Close the query to end the client-side loading animation
	try
	{
		await bot.AnswerCallbackQueryAsync(query.Id);
	}
	catch (Exception e)
	{
		Console.WriteLine(e);
		// throw;
	}

	try
	{
		// Replace menu text and keyboard
		await bot.EditMessageTextAsync(
			query.Message!.Chat.Id,
			query.Message.MessageId,
			text,
			ParseMode.Html,
			replyMarkup: markup
		);
	}
	catch (ApiRequestException apiRequestException)
	{
		//await bot.SendTextMessageAsync(query.Message.Chat.Id, "Чё-то пошло не так..." + apiRequestException.Message);
		Console.WriteLine(apiRequestException.Message);
	}
}