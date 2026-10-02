using System;
using Internal.Main;

public class Program
{    
    public static async Task Main (string[] args)
    {
        var UserDataPath = Path.Combine(AppContext.BaseDirectory, "UserData");
        Directory.CreateDirectory(UserDataPath);
        await InternalMain.StartLoadingReplays();
    }
}