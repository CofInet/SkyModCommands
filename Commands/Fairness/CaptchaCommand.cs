using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Coflnet.Sky.Commands.MC
{
    [CommandDescription("Solve a captcha",
        "You will be asked to solve a captcha if you are afk for too long",
        "You can also use this command to get a new captcha",
        "Example: /cl captcha another",
        "Use /cl captcha vertical to letters below each other",
        "Which helps if you have a mod with different font",
        "Captchas are necesary to prevent bots from using the flipper")]
    public class CaptchaCommand : McCommand
    {
        public override bool IsPublic => true;
        private int debugMultiplier = 1;
        private HashSet<string> formats = new() {
            "vertical", // fallback one letter per line
            "big", // normal
            "optifine", // different characters for optifine
            "short" // different characters for when spaces are shorter
            };
        public override async Task Execute(MinecraftSocket socket, string arguments)
        {
            var info = socket.SessionInfo.captchaInfo;
            var accountInfo = socket.AccountInfo;
            var solution = info.CurrentSolutions;
            info.CurrentSolutions = new List<string>();
            var receivedAt = DateTime.UtcNow;
            var attempt = arguments.Trim('"');
            info.CaptchaRequests++;
            Console.WriteLine($"Recieved {attempt}");
            if (formats.Contains(attempt))
            {
                accountInfo.CaptchaType = attempt;
                await RequireAnotherSolve(socket, info);
                await socket.sessionLifesycle.AccountInfo.Update();
                Console.WriteLine("Updated captcha type " + attempt);
                return;
            }
            if (attempt == "debug")
            {

                socket.Dialog(db => db
                    .ForEach("ayz🤨🤔🇧🇾:|,:-.#ä+!^°~´` ", (db, c) => db.ForEach("01234567890123456789", (idb, ignore) => idb.Msg(c.ToString())).MsgLine("|"))
                    .LineBreak().ForEach(":;", (db, c) => db.ForEach("012345678901234567890123456789", (idb, ignore) => idb.Msg(c.ToString())).MsgLine("|"))
                    .LineBreak().ForEach("´", (db, c) => db.ForEach("012345678901234567890123", (idb, ignore) => idb.Msg(c.ToString())).MsgLine("|"))
                    .MsgLine("Please screenshot the above and post it to our bug-report channel on discord"));
                return;
            }
            if (attempt == "another")
            {
                await RequireAnotherSolve(socket, info);
                return;
            }
            if (string.IsNullOrEmpty(attempt))
                socket.Dialog(db => db
                    .MsgLine($"{McColorCodes.BLUE}You requested to get a new captcha.")
                    .MsgLine($"{McColorCodes.OBFUSCATED}OOO{McColorCodes.RESET + McColorCodes.BLUE} This counts as a failed attempt. Have fun."));
            else
            {
                socket.SendMessage(COFLNET + "Checking your response");
                await Task.Delay(500 * debugMultiplier).ConfigureAwait(false);
            }
            if (solution.Contains(attempt))
            {
                info.RequireSolves--;
                if (info.RequireSolves < 0)
                    info.RequireSolves = 0;
                if (info.RequireSolves > 0)
                {
                    socket.SendMessage(COFLNET + McColorCodes.GREEN + "You solved the captcha, but you failed too many previously so please solve another one\n");
                    await RequireAnotherSolve(socket, info).ConfigureAwait(false);
                    return;
                }
                socket.SendMessage(COFLNET + McColorCodes.GREEN + "Thanks for confirming that you are a real user\n");

                info.LastSolve = DateTime.UtcNow;
                socket.sessionLifesycle.AccountInfo.Value.LastCaptchaSolve = DateTime.UtcNow;
                await socket.sessionLifesycle.AccountInfo.Update();
                Activity.Current.Log("solved captcha");
                await Task.Delay(2000).ConfigureAwait(false);
                await socket.sessionLifesycle.DelayHandler.Update(await socket.sessionLifesycle.GetMinecraftAccountUuids(), DateTime.Now);
                socket.SendMessage(COFLNET + McColorCodes.GREEN + "Your afk delay was updated\n");

                return;
            }

            if (!string.IsNullOrEmpty(attempt))
                socket.SendMessage($"{COFLNET}Your answer was {McColorCodes.RED}not correct{DEFAULT_COLOR}, lets try again");
            await RequireAnotherSolve(socket, info);

            info.RequireSolves++;
            info.FaildCount++;
            if (info.FaildCount <= 1 || accountInfo.CaptchaType != "vertical")
                return;
            socket.SendMessage($"{McColorCodes.DARK_GREEN}NOTE:{McColorCodes.YELLOW} "
                + $"Please make sure that the vertical green lines ({McColorCodes.GREEN}|{McColorCodes.YELLOW}) at the end of the captcha line up continuously.\n"
                + $"{McColorCodes.YELLOW}If they don't line up click on {McColorCodes.AQUA}Vertical{McColorCodes.YELLOW} to get a simpler captcha");
        }

        private async Task RequireAnotherSolve(MinecraftSocket socket, CaptchaInfo info)
        {
            socket.SendMessage(COFLNET + "Generating captcha");
            await Task.Delay(Math.Max(Math.Max(info.RequireSolves, 1), info.CaptchaRequests) * 800 * debugMultiplier).ConfigureAwait(false);
            socket.SendMessage(new CaptchaGenerator().SetupChallenge(socket, info));
            if (info.CaptchaRequests > 10)
            {
                info.CaptchaRequests = 0;
                info.RequireSolves++;
            }
            if (info.RequireSolves > 3)
                info.RequireSolves = 3;
        }
    }
}
