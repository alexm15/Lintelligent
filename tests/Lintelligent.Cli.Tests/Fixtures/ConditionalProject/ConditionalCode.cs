namespace ConditionalProject;

public class ConditionalCode
{
#if DEBUG
    // This method should only be analyzed in Debug configuration
    public void DebugOnlyMethod()
    {
        var debugMessage = "This is debug code";
        Console.WriteLine(debugMessage);
        
        // Intentionally long method for testing (21+ statements)
        var a = 1;
        var b = 2;
        var c = 3;
        var d = 4;
        var e = 5;
        var f = 6;
        var g = 7;
        var h = 8;
        var i = 9;
        var j = 10;
        var k = 11;
        var l = 12;
        var m = 13;
        var n = 14;
        var o = 15;
        var p = 16;
        var q = 17;
        var r = 18;
        var s = 19;
        var t = 20;
        Console.WriteLine(a + b + c + d + e + f + g + h + i + j + k + l + m + n + o + p + q + r + s + t);
    }
#endif

#if RELEASE
    // This method should only be analyzed in Release configuration
    public void ReleaseOnlyMethod()
    {
        var releaseMessage = "This is release code";
        Console.WriteLine(releaseMessage);
    }
#endif

    // This method should always be analyzed
    public void AlwaysCompiledMethod()
    {
        var message = "This code is always compiled";
        Console.WriteLine(message);
    }
}