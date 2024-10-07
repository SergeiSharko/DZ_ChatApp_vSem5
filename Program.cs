namespace DZ_ChatApp_vSem5
{
    internal class Program
    {
        static private CancellationTokenSource cts = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                await Server.Run("Server", cts.Token);
            }
            else if (args.Length == 1)
            {
                var state = Client.LoadState();
                if (state != null)
                {
                    state.Name = args[0];
                    await Client.Run(state.Name, state.Ip!, state.LocalPort, cts);
                }
                else
                {
                    Console.WriteLine("Для запуска клиента введите <Имя>, <IP-сервера> и <Номер порта>, как параметры запуска приложения");
                }
            }
            else if (args.Length == 3)
            {

                int port = int.Parse(args[2]);
                await Client.Run(args[0], args[1], port, cts);
                Client.SaveState(new ClientState { Name = args[0], Ip = args[1], LocalPort = port });
            }
            else
            {
                //Console.WriteLine("Для запуска сервера введите ник-нейм как параметр запуска приложения");
                Console.WriteLine("Для запуска клиента введите <Имя>, <IP-сервера> и <Номер порта>, как параметры запуска приложения");
            }
        }
    }
}
