using System.Threading.Tasks;

namespace Coflnet.Sky.Commands.MC
{
    public class TrackCommand : McCommand
    {
        public override Task Execute(MinecraftSocket socket, string arguments)
        {
            return Task.CompletedTask;
        }
    }
}