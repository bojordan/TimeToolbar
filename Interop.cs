using System.Runtime.InteropServices;

namespace TimeToolbar
{
    public static class Interop
    {
        // https://docs.microsoft.com/en-us/answers/questions/715081/how-to-detect-windows-dark-mode.html
        [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
        public static extern bool ShouldSystemUseDarkMode();
    }
}