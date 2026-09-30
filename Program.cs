using System;
using Internal.Main;

public class Program
{    
    public static async Task Main (string[] args)
    {
        await InternalMain.StartLoadingReplays();
    }
}