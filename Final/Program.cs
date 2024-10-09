using Final;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

BotMenu botMenu = new BotMenu();

Users users = new Users();
if (args.Length == 0)
{
	Console.WriteLine("Нужно указать botApi первым аргументом");
	Console.ReadKey();
    return;
}

string botApi = args[0];

var bot = new TelegramBotClient(botApi);
using var cts = new CancellationTokenSource();

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

async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
{
    User? currentUser = users.GetUserByUpdate(update);

    if (currentUser == null)
    {
        Console.WriteLine($"Не смог распознать пользователя {currentUser} по UpdateType = {update.Type}");
        return;
    }

    if (!users.UserPresented(currentUser))
    {
        users.AddUserData(new UserData(currentUser));
    }

    Console.WriteLine($"Handling Update for {currentUser}. UpdateType is {update.Type}");

    switch (update.Type)
    {
        case UpdateType.Message:

            await HandleMessage(update.Message!);
            break;

        case UpdateType.CallbackQuery:

            await HandleButton(update.CallbackQuery!, cts.Token);
            break;

        case UpdateType.MyChatMember:
            {
                await HandleMyChatMember(update, users.GetUserDataByUser(currentUser));
                break;
            }
        default:
            Console.WriteLine(update.Type);
            break;
    }
}
async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
{
    await Console.Error.WriteLineAsync(exception.ToString());
}

async Task HandleMessage(Message msg)
{
    User currentUser = msg.From;
    var text = msg.Text ?? string.Empty;
    if (currentUser is null) return;
    Console.WriteLine($"Handling Message from {currentUser}...");

    if (!users.UserPresented(currentUser))
    {
        users.AddUserData(new UserData(currentUser));
        Console.WriteLine($"UserData for {currentUser} not presented. Added. Users: {(string.Join(", ", users.GetUsersList()))}");
    }
    UserData currentUserData = users.GetUserDataByUser(currentUser);
    if (currentUserData is null) return;

    Console.WriteLine($"isWaitingForInput = {currentUserData.IsWaitingForInput}; isNewQuiz = {currentUserData.IsNewQuiz};" +
                      $" isNewQuestion = {currentUserData.IsNewQuestion}; isNewQuestionNewTimeSpan = {currentUserData.IsNewQuestionNewTimeSpan}");


    if (currentUserData.IsWaitingForInput)
    {
        if (currentUserData.IsNewQuiz)
        {
            if (msg.Text != null)
            {
                var newQuiz = CreateNewQuiz(msg);
                currentUserData.Quizzes.Add(newQuiz);
                currentUserData.EditingQuizGuid = newQuiz.Guid;
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, $"Название должно быть текстом");
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
                await bot.SendTextMessageAsync(msg.Chat.Id, $"Формат не распознан. Введите количество секунд на ответ");
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
            CreateNewQuestion(msg);
            currentUserData.IsNewQuestion = false;
            currentUserData.IsNewQuestionNewTimeSpan = true;
            await bot.SendTextMessageAsync(msg.Chat, "Время на ответ:");
        }

        if (!currentUserData.IsWaitingForInput && currentUserData.IsNewQuestionNewTimeSpan)
        {
            currentUserData.IsNewQuestionNewTimeSpan = false;
            Task.Run(async () =>
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, $"Вопрос добавлен. " +
                                                            $"{currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).Questions.Last().GetQuestionData()}." +
                                                            $" Всего вопросов = {currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).Questions.Count()}",
                    replyMarkup: botMenu.NewQuizMenuMarkup);
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
            await SendMenu(userId);
            break;
    }

    await Task.CompletedTask;
}

async Task HandleButton(CallbackQuery query,
	CancellationToken cancellationToken)
{

	User currentUser = query.From;
	UserData currentUserData = users.GetUserDataByUser(currentUser);
	if (currentUserData is null)
	{
		return;
	}

	Console.WriteLine($"users Count = {users.GetUsersList().Count()};\n" +
	                  $"currentUser = {currentUser};\n" +
	                  $"currentUserData.User = {users.GetUserDataByUser(currentUser)}");

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
			Chat chat = await bot.GetChatAsync(currentUserData.ChatId,
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
			currentUserData.NoAdminMessage = await bot.SendTextMessageAsync(query.Message.Chat,
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
		Chat chat = await bot.GetChatAsync(currentUserData.ChatId,
			cancellationToken);
		string chatTitle = chat.Title;
		Console.WriteLine($"FIRED");

		if (currentUserData.ArmedToFire != null)
		{
			Task.Run(async () => await Quiz.FireQuiz(currentUserData.ArmedToFire,
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
			markup = botMenu.MenuMarkup;
			break;

		case "NewQuizStopEditingDataCallBack":
			if (!currentUserData.Quizzes.Any())
				break;
			


			markup = botMenu.MenuMarkup;
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

			markup = botMenu.GenerateMarkupFromQuizzes(currentUserData.Quizzes);

			break;
		case "QuizGeneratedMarkupBack":
			text = "Меню:";
			markup = botMenu.MenuMarkup;
			break;
	}

	try
	{
		await bot.AnswerCallbackQueryAsync(query.Id);
	}
	catch (Exception e)
	{
		Console.WriteLine(e);
	}

	try
	{
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
                await bot.EditMessageTextAsync(
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
                await bot.EditMessageTextAsync(
                    userData.NoAdminMessage!.Chat.Id,
                    userData.NoAdminMessage.MessageId,
                    $"Я удалился из {update1.MyChatMember.Chat.Type} '{update1.MyChatMember.Chat.Title}'",
                    ParseMode.Html
                );
            }
        }
    }
}
Quiz CreateNewQuiz(Message quizMessage)
{

    User currentUser = quizMessage.From;
    UserData currentUserData = users.GetUserDataByUser(currentUser);

    Quiz newQuiz = new Quiz(quizMessage.Text, string.Empty, new List<Question>(), bot);
    Task.Run(async () =>
    {
        await bot.SendTextMessageAsync(quizMessage.Chat.Id, "пусть будет '" + quizMessage.Text + $"'\nGuid = {newQuiz.Guid}",
            replyMarkup: botMenu.NewQuizMenuMarkup);
    });

    currentUserData.IsWaitingForInput = false;

    return newQuiz;
}

Question CreateNewQuestion(Message quizMessage)
{

    User currentUser = quizMessage.From;
    UserData currentUserData = users.GetUserDataByUser(currentUser);

    string questionData = string.Empty;
    Question question;


    if (quizMessage.Type == MessageType.Photo)
    {
        var photo = quizMessage.Photo;
        questionData = photo[photo.Count() - 1].FileId;
    }
    if (quizMessage.Type == MessageType.Video)
    {
        var video = quizMessage.Video;
        questionData = video.FileId;
    }
    if (quizMessage.Type == MessageType.Text)
        questionData = quizMessage.Text;

    question = new Question(quizMessage.Type, questionData, TimeSpan.Zero);

    currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).Questions.Add(question);

    Console.WriteLine($"Вопрос {question.Guid} добавлен в квиз '{currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).Name}'," +
                      $" всего времени на ответы в квизе = {currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).GetTotalTimeSpan()}");
    return question;
}


async Task SendMenu(long userId)
{
    var text = "Меню:";
    var markup = botMenu.MenuMarkup;

    await bot.SendTextMessageAsync(
        userId,
        text,
        replyMarkup: markup
    );
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