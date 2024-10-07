using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace DZ_ChatApp_vSem5
{
    internal class Client
    {
        static private CancellationTokenSource cts = new CancellationTokenSource();

        public static async Task Run(string name, string ip, int localPort, CancellationTokenSource cts)
        {
            UdpClient udpClient = new UdpClient();
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
            udpClient.Client.Bind(localEndPoint);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), 12345);
            Console.WriteLine("UDP Клиент запущен...");

            Task clientListenerTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var receiveData = await udpClient.ReceiveAsync();
                        string receiveString = Encoding.UTF8.GetString(receiveData.Buffer);
                        var receiveMessage = MessageUDP.FromJson(receiveString);
                        PrintingMessagesWithOffset(receiveMessage!.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка при получении сообщения: " + ex.Message);
                    }
                }
            }, cts.Token);

            Task clientSenderTask = Task.Run(async () =>
            {
                var defRegMessage = new MessageUDP() { FromName = name, Text = null, ToName = "Server", Command = Command.Register }.ToJson();
                byte[] defRegBytes = Encoding.UTF8.GetBytes(defRegMessage);
                await udpClient.SendAsync(defRegBytes, defRegBytes.Length, remoteEndPoint);
                Console.WriteLine($"Пользователь {name} зарегистрирован в чате поумолчанию.");


                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        string? toName = GetCorrectInput("Введите адресата сообщения => ");
                        string? message = GetCorrectInput("Введите сообщение или Exit для выхода из клиента => ");
                        if (message!.ToLower().Equals("exit"))
                        {
                            byte[] exitBytes = Encoding.UTF8.GetBytes("exit");
                            await udpClient.SendAsync(exitBytes, exitBytes.Length, remoteEndPoint);
                            udpClient.Close();
                            cts.Cancel();
                            break;
                        }

                        var messageJson = new MessageUDP()
                        {
                            Command = Command.Message,
                            FromName = name,
                            ToName = toName!,
                            Date = DateTime.Now,
                            Text = message
                        }.ToJson();

                        byte[] replyBytes = Encoding.UTF8.GetBytes(messageJson);
                        await udpClient.SendAsync(replyBytes, replyBytes.Length, remoteEndPoint);
                        Console.WriteLine("Сообщение отправлено.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка при отправке сообщения: " + ex.Message);
                    }
                }
            }, cts.Token);

            await Task.WhenAny(clientListenerTask, clientSenderTask);
        }


        private static bool IsCorrectInput(string input)
        {
            return !string.IsNullOrEmpty(input) && !string.IsNullOrWhiteSpace(input);
        }

        private static string GetCorrectInput(string message)
        {
            Console.Write(message);
            string? result = Console.ReadLine();
            while (!IsCorrectInput(result!))
            {
                Console.WriteLine("Некорретный ввод. Пробуйте ещё раз");
                Console.Write(message);
                result = Console.ReadLine();
            }
            return result!;
        }

        private static void PrintingMessagesWithOffset(string message)
        {
            if (OperatingSystem.IsWindows())
            {
                var position = Console.GetCursorPosition(); // получаем текущую позицию курсора
                int left = position.Left;   // смещение в символах относительно левого края
                int top = position.Top;     // смещение в строках относительно верха                                                
                Console.MoveBufferArea(0, top, left, 1, 0, top + 1); // копируем ранее введенные символы в строке на следующую строку                    
                Console.SetCursorPosition(0, top); // устанавливаем курсор в начало текущей строки
                Console.WriteLine(message); // в текущей строке выводит полученное сообщение
                Console.SetCursorPosition(left, top + 1);// переносим курсор на следующую строку и продолжаем ввод
            }
            else Console.WriteLine(message);
        }

        public static void SaveState(ClientState state)
        {
            string json = JsonSerializer.Serialize(state);
            File.WriteAllText("client_state.json", json);
        }

        public static ClientState? LoadState()
        {
            if (File.Exists("client_state.json"))
            {
                string json = File.ReadAllText("client_state.json");
                return JsonSerializer.Deserialize<ClientState>(json);
            }
            return null;
        }
    }
}
