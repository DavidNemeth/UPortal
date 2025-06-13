using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace UPortal.HelperServices
{
    public class IconService : IIconService
    {

        // The single, authoritative list of all supported icon names.
        private static readonly IReadOnlyList<string> IconNames = new List<string>
    {
        "Accessibility", "AppFolder", "ArrowSyncCircle", "Apps", "Beaker", "BookDatabase",
        "Building", "CalendarLtr", "ClipboardDataBar", "ClipboardText", "ClipboardTextEdit", "Cloud",
        "Clover", "CoinMultiple", "Connected", "ContactCard", "ContentView", "Cookies",
        "Desktop", "DesktopMac", "DeviceMeetingRoom", "Dialpad", "Document", "Flash",
        "FlipHorizontal", "Gauge", "Games", "Globe", "Handshake", "HatGraduation",
        "HeadphonesSoundWave", "Heart", "HeartPulse", "Home", "LeafOne", "LeafTwo", "Lightbulb",
        "Link", "LockOpen", "Mail", "Notepad", "PeopleTeam", "PersonAlert", "Search",
        "Settings", "Star"
    }.AsReadOnly();

        public IEnumerable<string> GetAvailableIconNames() => IconNames.OrderBy(name => name);

        /// <summary>
        /// The centralized method to resolve an icon name and size to a specific Icon object.
        /// Add any new icons ONLY to this switch statement.
        /// </summary>
        public Icon GetIcon(string name, IconSize size = IconSize.Size24)
        {
            // Default icon for any name that isn't found
            Icon FallbackIcon() => size switch
            {
                IconSize.Size32 => new Icons.Regular.Size32.Question(),
                _ => new Icons.Regular.Size24.Question()
            };

            // This switch is now the single source of truth for icon rendering.
            return name switch
            {
                "Accessibility" => size == IconSize.Size32 ? new Icons.Regular.Size32.Accessibility() : (Icon)new Icons.Regular.Size24.Accessibility(),
                "AppFolder" => size == IconSize.Size32 ? new Icons.Regular.Size32.AppFolder() : new Icons.Regular.Size24.AppFolder(),
                "ArrowSyncCircle" => size == IconSize.Size32 ? new Icons.Regular.Size32.ArrowSyncCircle() : new Icons.Regular.Size24.ArrowSyncCircle(),
                "Apps" => size == IconSize.Size32 ? new Icons.Regular.Size32.Apps() : new Icons.Regular.Size24.Apps(),
                "Beaker" => size == IconSize.Size32 ? new Icons.Regular.Size32.Beaker() : new Icons.Regular.Size24.Beaker(),
                "BookDatabase" => size == IconSize.Size32 ? new Icons.Regular.Size32.BookDatabase() : new Icons.Regular.Size24.BookDatabase(),
                "Building" => size == IconSize.Size32 ? new Icons.Regular.Size32.Building() : new Icons.Regular.Size24.Building(),
                "CalendarLtr" => size == IconSize.Size32 ? new Icons.Regular.Size32.CalendarLtr() : new Icons.Regular.Size24.CalendarLtr(),
                "ClipboardDataBar" => size == IconSize.Size32 ? new Icons.Regular.Size32.ClipboardDataBar() : new Icons.Regular.Size24.ClipboardDataBar(),
                "ClipboardText" => size == IconSize.Size32 ? new Icons.Regular.Size32.ClipboardText() : new Icons.Regular.Size24.ClipboardTextEdit(),
                "ClipboardTextEdit" => size == IconSize.Size32 ? new Icons.Regular.Size32.ClipboardTextEdit() : new Icons.Regular.Size24.ClipboardTextEdit(),
                "Cloud" => size == IconSize.Size32 ? new Icons.Regular.Size32.Cloud() : new Icons.Regular.Size24.Cloud(),
                "Clover" => size == IconSize.Size32 ? new Icons.Regular.Size32.Clover() : new Icons.Regular.Size24.Clover(),
                "CoinMultiple" => size == IconSize.Size32 ? new Icons.Regular.Size32.CoinMultiple() : new Icons.Regular.Size24.CoinMultiple(),
                "Connected" => size == IconSize.Size32 ? new Icons.Regular.Size32.Connected() : new Icons.Regular.Size24.Connected(),
                "ContactCard" => size == IconSize.Size32 ? new Icons.Regular.Size32.ContactCard() : new Icons.Regular.Size24.ContactCard(),
                "ContentView" => size == IconSize.Size32 ? new Icons.Regular.Size32.ContentView() : new Icons.Regular.Size24.ContentView(),
                "Cookies" => size == IconSize.Size32 ? new Icons.Regular.Size32.Cookies() : new Icons.Regular.Size24.Cookies(),
                "Desktop" => size == IconSize.Size32 ? new Icons.Regular.Size32.Desktop() : new Icons.Regular.Size24.Desktop(),
                "DesktopMac" => size == IconSize.Size32 ? new Icons.Regular.Size32.DesktopMac() : new Icons.Regular.Size24.DesktopMac(),
                "DeviceMeetingRoom" => size == IconSize.Size32 ? new Icons.Regular.Size32.DeviceMeetingRoom() : new Icons.Regular.Size24.DeviceMeetingRoom(),
                "Dialpad" => size == IconSize.Size32 ? new Icons.Regular.Size32.Dialpad() : new Icons.Regular.Size24.Dialpad(),
                "Document" => size == IconSize.Size32 ? new Icons.Regular.Size32.Document() : new Icons.Regular.Size24.Document(),
                "Flash" => size == IconSize.Size32 ? new Icons.Regular.Size32.Flash() : new Icons.Regular.Size24.Flash(),
                "FlipHorizontal" => size == IconSize.Size32 ? new Icons.Regular.Size32.FlipHorizontal() : new Icons.Regular.Size24.FlipHorizontal(),
                "Gauge" => size == IconSize.Size32 ? new Icons.Regular.Size32.Gauge() : new Icons.Regular.Size24.Gauge(),
                "Games" => size == IconSize.Size32 ? new Icons.Regular.Size32.Games() : new Icons.Regular.Size24.Games(),
                "Globe" => size == IconSize.Size32 ? new Icons.Regular.Size32.Globe() : new Icons.Regular.Size24.Globe(),
                "Handshake" => size == IconSize.Size32 ? new Icons.Regular.Size32.Handshake() : new Icons.Regular.Size24.Handshake(),
                "HatGraduation" => size == IconSize.Size32 ? new Icons.Regular.Size32.HatGraduation() : new Icons.Regular.Size24.HatGraduation(),
                "HeadphonesSoundWave" => size == IconSize.Size32 ? new Icons.Regular.Size32.HeadphonesSoundWave() : new Icons.Regular.Size24.HeadphonesSoundWave(),
                "Heart" => size == IconSize.Size32 ? new Icons.Regular.Size32.Heart() : new Icons.Regular.Size24.Heart(),
                "HeartPulse" => size == IconSize.Size32 ? new Icons.Regular.Size32.HeartPulse() : new Icons.Regular.Size24.HeartPulse(),
                "Home" => size == IconSize.Size32 ? new Icons.Regular.Size32.Home() : new Icons.Regular.Size24.Home(),
                "LeafOne" => size == IconSize.Size32 ? new Icons.Regular.Size32.LeafOne() : new Icons.Regular.Size24.LeafOne(),
                "LeafTwo" => size == IconSize.Size32 ? new Icons.Regular.Size32.LeafTwo() : new Icons.Regular.Size24.LeafTwo(),
                "Lightbulb" => size == IconSize.Size32 ? new Icons.Regular.Size32.Lightbulb() : new Icons.Regular.Size24.Lightbulb(),
                "Link" => size == IconSize.Size32 ? new Icons.Regular.Size32.Link() : new Icons.Regular.Size24.Link(),
                "LockOpen" => size == IconSize.Size32 ? new Icons.Regular.Size32.LockOpen() : new Icons.Regular.Size24.LockOpen(),
                "Mail" => size == IconSize.Size32 ? new Icons.Regular.Size32.Mail() : new Icons.Regular.Size24.Mail(),
                "Notepad" => size == IconSize.Size32 ? new Icons.Regular.Size32.Notepad() : new Icons.Regular.Size24.Notepad(),
                "PeopleTeam" => size == IconSize.Size32 ? new Icons.Regular.Size32.PeopleTeam() : new Icons.Regular.Size24.PeopleTeam(),
                "PersonAlert" => size == IconSize.Size32 ? new Icons.Regular.Size32.PersonAlert() : new Icons.Regular.Size24.PersonAlert(),
                "Search" => size == IconSize.Size32 ? new Icons.Regular.Size32.Search() : new Icons.Regular.Size24.Search(),
                "Settings" => size == IconSize.Size32 ? new Icons.Regular.Size32.Settings() : new Icons.Regular.Size24.Settings(),
                "Star" => size == IconSize.Size32 ? new Icons.Regular.Size32.Star() : new Icons.Regular.Size24.Star(),
                _ => FallbackIcon(),
            };
        }
    }
}