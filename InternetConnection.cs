using System.Runtime.InteropServices;

namespace SARModel
{

    #region ConnectionCheckerClass
    public static partial class InternetConnection
    {
        public static bool UseIternet { get; set; }
        public static bool ShouldCheck { get; set; } = true;
        public static string AppName { get => AppDomain.CurrentDomain.FriendlyName; }

        public static event EventHandler<ConnectionCheckerArgs>? StatusEvent;
        public static void Call(bool value) => StatusEvent?.Invoke(new(), new() { IsConnected = value });

        static Action<bool>? _availability;
        public static Action<bool>? Availability
        {
            get=>_availability;
            set => _availability = value;
        }

        static Action<bool>? _whileChecking;
        public static Action<bool>? WhileChecking
        {
            get => _whileChecking;
            set => _whileChecking = value;
        }

        //InternetConnection.Availability =
        //            (value) =>
        //            {
        //                Application.Current?.Dispatcher?.Invoke(new Action(() => InternetConnection.Call(value)));

        //            };


        //InternetConnection.WhileChecking =
        //            (value) =>
        //            {
        //                Application.Current?.Dispatcher?.Invoke(new Action(() => InternetConnection.Call(value)));

        //            };

        public static async Task TaskTryUntilConnect()
        {
            if (!ShouldCheck || !UseIternet) return;
            await Task.Run(() =>
            {
                if (Availability==null) Call(IsAvailable());
                else Availability.Invoke(IsAvailable());
                
                while (!IsAvailable())
                {
                }
            });
        }

        public static async Task CheckingInternetConnection()
        {
            if (!ShouldCheck || !UseIternet) return;
            bool lastCheck = IsAvailable();
            while (true)
            {
                await Task.Run(() =>
                {
                    bool nextCheck = IsAvailable();
                    if (lastCheck != nextCheck)
                    {
                        lastCheck = nextCheck;
                        if (WhileChecking == null) Call(nextCheck);
                        else WhileChecking.Invoke(nextCheck);
                    }
                });
            }
        }
        public static Task<bool> IsAvailableTask()
        {
            if (!UseIternet) return Task.FromResult(true);
            bool result = IsAvailable();
            Call(result);
            return Task.FromResult(result);
        }

        [LibraryImport("wininet.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool InternetGetConnectedState(out int description, int reservedValue);

        static bool IsAvailable() => InternetGetConnectedState(out _, 0);

        public class ConnectionCheckerArgs : EventArgs
        {
            public bool IsConnected { get; set; }
            public string Message { get; set; } = "NO INTERNET, I'll keep trying...";
        }
    }

    #endregion

}
