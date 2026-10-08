using System;
using System.IO;
using Internal.Main;

namespace Internal.Start;

public class Program
{    
    public static async Task Main (string[] args)
    {
        var UserCachePath = Path.Combine(AppContext.BaseDirectory, "UserCache");
        var UserDataPath = Path.Combine(AppContext.BaseDirectory, "UserData");
        Directory.CreateDirectory(UserDataPath);
        Directory.CreateDirectory(UserCachePath);
        await InternalMain.StartLoadingReplays();
    }
}