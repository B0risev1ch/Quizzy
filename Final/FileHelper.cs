using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Final
{
	class FileHelper
	{
		public static void SaveQuizToFile(Quiz quiz, string filePath)
		{
			var directory = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var settings = new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				Converters = { new StringEnumConverter() }
			};

			using (var writer = new StreamWriter(filePath))
			{
				var serializer = new Newtonsoft.Json.JsonSerializer();
				serializer.Formatting = Formatting.Indented;
				serializer.Converters.Add(new StringEnumConverter());
				serializer.Serialize(writer, quiz);
			}

        }
        
		public static Quiz LoadQuizFromFile(string filePath)
		{
			string jsonString = File.ReadAllText(filePath);
			
			var options = new Newtonsoft.Json.JsonSerializerSettings
            {
				Converters = { new StringEnumConverter(), new MessageTypeConverter() }
			};

			return JsonConvert.DeserializeObject<Quiz>(jsonString, options);
            /*
			return Newtonsoft.Json.JsonSerializer.Deserialize<Quiz>();
            */

        }
    
        
    }
}
