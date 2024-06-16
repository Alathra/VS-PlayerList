using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace playerlist.util;

public abstract class Util {
    public static readonly CairoFont DefaultFont = new() {
        Color = GuiStyle.DialogDefaultTextColor,
        Fontname = GuiStyle.StandardFontName,
        UnscaledFontsize = GuiStyle.SmallFontSize,
        Orientation = EnumTextOrientation.Left
    };

    public static readonly CairoFont CenteredFont = DefaultFont.Clone().WithOrientation(EnumTextOrientation.Center);

    public static (string?, string?) ParseHeaderAndFooter(PlayerList mod, int maxCount) {
        GameCalendar calendar = (GameCalendar)mod.Api.World.Calendar;
        string? serverName = ((ClientMain)mod.Api.World).GetField<ServerInformation>("ServerInfo")?.GetField<string>("ServerName");
        string maxPlayers = mod.Config.MaxPlayers?.ToString() ?? "?";
        string curPlayers = maxCount.ToString();
        string month = Lang.Get($"month-{calendar.MonthName}");
        string day = calendar.DayOfMonth.ToString("00");
        string year = calendar.Year.ToString("0");

        return (
            Parse(mod.Config.Header, serverName, maxPlayers, curPlayers, month, day, year),
            Parse(mod.Config.Footer, serverName, maxPlayers, curPlayers, month, day, year)
        );
    }

    private static string? Parse(string? text, string? serverName, string maxPlayers, string curPlayers, string month, string day, string year) {
        return text?
            .Replace("{ServerName}", serverName)
            .Replace("{MaxPlayers}", maxPlayers)
            .Replace("{CurPlayers}", curPlayers)
            .Replace("{Month}", month)
            .Replace("{Day}", day)
            .Replace("{Year}", year);
    }
}
