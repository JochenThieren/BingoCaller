// AppIcon.cs
namespace BingoCaller
{
    /// <summary>The application icon (embedded BingoCaller.ico), shared by every window and dialog.</summary>
    internal static class AppIcon
    {
        private static Icon? _icon;

        public static Icon Get()
        {
            if (_icon != null) return _icon;

            using Stream? stream = typeof(AppIcon).Assembly.GetManifestResourceStream("BingoCaller.ico");
            _icon = stream != null ? new Icon(stream) : SystemIcons.Application;
            return _icon;
        }
    }
}
