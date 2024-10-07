using System.Text.Json;

namespace DZ_ChatApp_vSem5
{
    internal class MessageUDP
    {
        public int? Id { get; set; }
        public string? FromName { get; set; }
        public string? ToName { get; set; }
        public DateTime Date { get; set; }
        public string? Text { get; set; }
        public Command Command { get; set; }

        public MessageUDP() { }

        public MessageUDP(string fromName, string toName, string text)
        {
            FromName = fromName;
            ToName = toName;
            Date = DateTime.Now;
            Text = text;
        }

        // Метод для сериализации в JSON
        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        // Статический метод для десериализации JSON в объект Message
        public static MessageUDP? FromJson(string json)
        {
            return JsonSerializer.Deserialize<MessageUDP>(json);
        }

        public override string ToString()
        {
            return $"Cообщение от {FromName} для {ToName} - ({Date}):=> {Text}";
        }

    }

    public enum Command
    {
        Register,
        Message,
        Confirmation
    }
}