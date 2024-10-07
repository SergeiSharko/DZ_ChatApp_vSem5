using System.Net;
using System.Net.Sockets;
using System.Text;
using DZ_ChatApp_vSem5.Models;

namespace DZ_ChatApp_vSem5
{
    internal class Server
    {
        private static CancellationTokenSource cts = new CancellationTokenSource();

        private static Dictionary<String, IPEndPoint> clients = new Dictionary<string, IPEndPoint>();
        private static UdpClient udpClient = new UdpClient(12345);

        public static async Task Run(string name, CancellationToken cancellationToken)
        {

            Task serverTask = Task.Run(async () =>
            {
                Console.WriteLine("UDP Сервер ожидает сообщений...");
                Console.WriteLine("Нажмите любую клавишу для завершения работы сервера!");

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var receiveData = await udpClient.ReceiveAsync();
                        byte[] receiveBytes = receiveData.Buffer;
                        string receivedString = Encoding.UTF8.GetString(receiveBytes);

                        if (receivedString.ToLower().Equals("exit"))
                        {
                            Console.WriteLine("Получена команда для завершения работы. Сервер закрывается...");
                            udpClient.Close();
                            cts.Cancel();
                            Environment.Exit(0);
                        }

                        var message = MessageUDP.FromJson(receivedString);
                        await ProcessMessage(message!, receiveData.RemoteEndPoint);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка при обработке сообщения: " + ex.Message);
                    }
                }
            }, cancellationToken);

            try
            {
                Console.ReadKey(true);
                Console.WriteLine("Клавиша нажата. Сервер закрывается...");
                cts.Cancel();
            }
            finally
            {
                udpClient.Close();
            }
        }


        public static void Register(MessageUDP message, IPEndPoint fromEndPoint)
        {
            if (clients.TryAdd(message.FromName!, fromEndPoint))
            {
                using (var ctx = new ChatUdpContext())
                {
                    if (ctx.Users.FirstOrDefault(x => x.Name == message.FromName) != null)
                    {
                        Console.WriteLine($"Пользователь {message.FromName} уже зарегистрирован в чате");
                        return;
                    }
                    else
                    {
                        ctx.Add(new User { Name = message.FromName });
                        ctx.SaveChanges();
                        Console.WriteLine($"Пользователь {message.FromName} зарегистрирован в чате");
                    }
                }
            }

        }


        public static void ConfirmMessageReceived(int? id)
        {
            Console.WriteLine($"Cообщение подтверждено, ему назначено id = {id}");

            using (var ctx = new ChatUdpContext())
            {
                var msg = ctx.Messages.FirstOrDefault(x => x.Id == id);
                if (msg != null)
                {
                    msg.isReceived = true;
                    ctx.SaveChanges();
                }
            }
        }


        public static async Task ReplyMessage(MessageUDP message)
        {
            int? id = null;
            if (clients.TryGetValue(message.ToName!, out IPEndPoint? endPoint))
            {
                using (var ctx = new ChatUdpContext())
                {
                    var fromUser = ctx.Users.FirstOrDefault(x => x.Name == message.FromName);
                    var toUser = ctx.Users.FirstOrDefault(y => y.Name == message.ToName);
                    var msg = new Message
                    {
                        FromUserId = fromUser!.Id,
                        ToUserId = toUser!.Id,
                        isReceived = false,
                        Text = message.Text,
                    };

                    ctx.Messages.Add(msg);

                    ctx.SaveChanges();

                    id = msg.Id;
                }

                var forwardMessage = new MessageUDP()
                {
                    Id = id,
                    Command = Command.Message,
                    ToName = message.ToName,
                    FromName = message.FromName,
                    Text = message.Text
                };

                ConfirmMessageReceived(forwardMessage.Id);

                byte[] forwardBytes = Encoding.UTF8.GetBytes(forwardMessage.ToJson());
                await udpClient.SendAsync(forwardBytes, forwardBytes.Length, endPoint);
                Console.WriteLine($"Подтвержденное сообщение, от = {message.FromName} для = {message.ToName}");
            }
            else
            {
                Console.WriteLine("Пользователь не найден");
            }
        }


        public static async Task ProcessMessage(MessageUDP message, IPEndPoint fromEndPoint)
        {
            if (message.Command == Command.Register)
            {
                Register(message, new IPEndPoint(fromEndPoint.Address, fromEndPoint.Port));
            }
            if (message.Command == Command.Message)
            {
                await ReplyMessage(message);
            }
        }
    }
}