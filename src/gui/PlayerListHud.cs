using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using playerlist.util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace playerlist.gui;

public class PlayerListHud : HudElement {
    private static readonly double[] _white = ColorUtil.Hex2Doubles("#FFFFFF", 0.1);

    private readonly PlayerList _mod;
    private readonly KeyHandler _keyHandler;

    private long _gameTickListenerId;
    private bool _wasOpen;

    public PlayerListHud(PlayerList mod) : base((ICoreClientAPI)mod.Api) {
        _mod = mod;

        _keyHandler = new KeyHandler(capi);

        capi.Event.PlayerJoin += UpdateList;
        capi.Event.PlayerLeave += UpdateList;
    }

    public override void OnOwnPlayerDataReceived() {
        UpdateList();
    }

    private void UpdateList(IPlayer? notUsed = null) {
        List<IPlayer> players = capi.World.AllOnlinePlayers
            .OrderBy(player => player.PlayerName) // todo - configurable sort order (maybe?)
            .ToList();

        Composers["playerlist"] = Compose(players).Compose();

        TryOpen();
    }

    private GuiComposer Compose(List<IPlayer> players) {
        // only show the first 100 players
        int maxCount = Math.Min(players.Count, 100);
        // max 20 players per row
        int columns = (int)Math.Ceiling(maxCount / 20D);
        // evenly distribute players between columns
        int rows = (int)Math.Ceiling(maxCount / (double)columns);

        // calculate max player name width
        ElementBounds maxBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0, 0, 0, 0);
        for (int i = 0; i < maxCount; i++) {
            players[i].EntitlementColoredFont().AutoBoxSize(players[i].PlayerName, maxBounds, true);
        }

        const double padding = 2;
        const double iconSize = 16;
        const double rowHeight = 25;
        double columnWidth = maxBounds.fixedWidth + (iconSize * 2.5D);

        // todo - add configurable header and footer for servers
        // todo - show cur/max player counts
        GuiComposer composer = capi.Gui
            .CreateCompo("playerlist:thelist", new ElementBounds {
                Alignment = EnumDialogArea.CenterTop,
                BothSizing = ElementSizing.FitToChildren,
                fixedOffsetY = 100
            })
            .AddStaticElement(new GuiElementRectangle(capi, ElementBounds.Fill, GuiStyle.DialogDefaultBgColor));

        double yOffset = 0;
        CairoFont centeredFont = PlayerList.DefaultFont.Clone().WithOrientation(EnumTextOrientation.Center);
        (string? header, string? footer) = ParseHeaderAndFooter();

        if (!string.IsNullOrEmpty(header)) {
            ElementBounds bounds = ElementBounds.FixedSize(EnumDialogArea.CenterTop, 1024, rowHeight + padding);
            composer.AddVtmlText(header, centeredFont, bounds);
            yOffset += bounds.fixedHeight;
        }

        for (int i = 0; i < maxCount; i++) {
            composer.BeginChildElements(new ElementBounds {
                        Alignment = EnumDialogArea.CenterTop,
                        horizontalSizing = ElementSizing.FitToChildren,
                        verticalSizing = ElementSizing.Fixed,
                        fixedWidth = columnWidth,
                        fixedHeight = rowHeight
                    }
                    // ReSharper disable once PossibleLossOfFraction
                    .WithFixedPosition((columnWidth + padding) * (i / rows), ((rowHeight + padding) * (i % rows)) + yOffset)
                    .WithFixedPadding(padding)
                )
                .AddStaticElement(new GuiElementRectangle(capi, ElementBounds.Fixed(EnumDialogArea.LeftTop, 0, 0, maxBounds.fixedWidth + (iconSize * 2.5D), rowHeight), _white))
                .AddStaticElement(new GuiElementScalableImage(capi, ElementBounds.Fixed(EnumDialogArea.LeftMiddle, iconSize / 2, 1, iconSize, iconSize), _mod.PingIcon(players[i].Ping())))
                .AddStaticText(players[i].PlayerName, players[i].EntitlementColoredFont(), EnumTextOrientation.Left, ElementBounds.Fixed(EnumDialogArea.LeftMiddle, iconSize * 2, -1, maxBounds.fixedWidth, maxBounds.fixedHeight))
                .EndChildElements();
        }

        if (!string.IsNullOrEmpty(footer)) {
            ElementBounds bounds = ElementBounds.Fixed(EnumDialogArea.CenterTop, 0, ((rowHeight + padding) * rows) + yOffset, 1024, rowHeight + padding);
            composer.AddVtmlText(footer, centeredFont, bounds);
            composer.AddStaticElement(new GuiElementRectangle(capi, ElementBounds.Fixed(EnumDialogArea.CenterTop, 0, bounds.fixedY + bounds.fixedHeight, 0, 0), _white));
        }

        return composer;
    }

    private (string?, string?) ParseHeaderAndFooter() {
        GameCalendar calendar = (GameCalendar)capi.World.Calendar;
        string? serverName = ((ClientMain)capi.World).GetField<ServerInformation>("ServerInfo")?.GetField<string>("ServerName");
        string maxPlayers = _mod.Config.MaxPlayers?.ToString() ?? "?";
        string curPlayers = capi.World.AllOnlinePlayers.Length.ToString();
        string month = Lang.Get($"month-{calendar.MonthName}");
        string day = calendar.DayOfMonth.ToString("00");
        string year = calendar.Year.ToString("0");

        return (
            Parse(_mod.Config.Header, serverName, maxPlayers, curPlayers, month, day, year),
            Parse(_mod.Config.Footer, serverName, maxPlayers, curPlayers, month, day, year)
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

    public override bool ShouldReceiveRenderEvents() {
        bool shouldOpen = _keyHandler.IsKeyComboActive();

        switch (shouldOpen) {
            case true when !_wasOpen:
                UpdateList();
                _gameTickListenerId = capi.Event.RegisterGameTickListener(_ => UpdateList(), 1000);
                break;
            case false when _wasOpen:
                capi.Event.UnregisterGameTickListener(_gameTickListenerId);
                break;
        }

        return _wasOpen = shouldOpen;
    }

    public override double InputOrder => 1.0999;
    public override double DrawOrder => 0.8899;
    public override float ZSize => 200F;
    public override bool ShouldReceiveKeyboardEvents() => false;
    public override void OnKeyDown(KeyEvent args) { }
    public override void OnKeyPress(KeyEvent args) { }
    public override void OnKeyUp(KeyEvent args) { }
    public override bool OnEscapePressed() => false;
    public override bool ShouldReceiveMouseEvents() => false;
    public override void OnMouseDown(MouseEvent args) { }
    public override void OnMouseUp(MouseEvent args) { }
    public override void OnMouseMove(MouseEvent args) { }
    public override void OnMouseWheel(MouseWheelEventArgs args) { }
    public override bool OnMouseEnterSlot(ItemSlot slot) => false;
    public override bool OnMouseLeaveSlot(ItemSlot itemSlot) => false;
    public override bool CaptureAllInputs() => false;
    public override bool TryClose() => false;
    public override void Toggle() { }
    public override void UnFocus() { }
    public override void Focus() { }
    public override bool Focused => false;
    protected override void OnFocusChanged(bool on) => focused = false;

    [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize")]
    public override void Dispose() {
        base.Dispose();

        _keyHandler.Dispose();

        capi.Event.UnregisterGameTickListener(_gameTickListenerId);

        capi.Event.PlayerJoin -= UpdateList;
        capi.Event.PlayerLeave -= UpdateList;
    }
}
