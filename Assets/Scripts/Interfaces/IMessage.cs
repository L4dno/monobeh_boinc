public interface IMessage
{
    // базовый класс для всех REQUEST, REPLY, RESULT
    // Termination в исходном коде применяется главным сервом для остановки DataServers
    IActor Sender { get; }

}