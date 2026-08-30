using System;
using Sol.Helpers;

namespace Sol.Services;

public class GreetingService : IGreetingService
{
    private readonly int _greetingIndex;

    public GreetingService()
    {
        var random = new Random();
        _greetingIndex = random.Next(Strings.AllGreetings.Length);
    }

    public string GetStartupGreeting()
    {
        var greetings = Strings.AllGreetings;
        if (_greetingIndex >= 0 && _greetingIndex < greetings.Length)
        {
            return greetings[_greetingIndex];
        }
        return greetings.Length > 0 ? greetings[0] : string.Empty;
    }
}
