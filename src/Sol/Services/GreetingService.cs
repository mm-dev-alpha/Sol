using System;

namespace Sol.Services;

public class GreetingService : IGreetingService
{
    private readonly string[] _greetings = new[]
    {
        "Ich habe nichts gemacht, das war plötzlich einfach so! 🤷‍♀️",
        "Gestern ging es aber noch! ⏳",
        "Ich brauche ein neues Passwort, das alte funktioniert schon wieder nicht. 🔑",
        "Können Sie das schnell beheben? Es brennt wirklich und ich muss in 5 Minuten ein Dokument abgeben! 🔥",
        "Das Internet ist komplett gelöscht! 🌐",
        "Ich habe den PC schon dreimal neu gestartet! (Monitor aus- und wieder eingeschaltet) 🖥️",
        "Mein Bildschirm ist ganz schwarz, woran liegt das? (Stromkabel liegt daneben) 🕶️",
        "Können Sie das nicht einfach magisch reparieren, ohne dass ich etwas tun muss? 🧙‍♂️",
        "Ich habe auf den Link in der komischen E-Mail geklickt, weil da stand, ich hätte ein iPhone gewonnen. 🎁",
        "Mein Passwort? Das klebt doch als Post-it direkt am Monitor! 📝",
        "Seit dem letzten Windows-Update ist die Kaffeemaschine kaputt! ☕",
        "Können Sie mir das Internet schneller machen? Das lädt heute so langsam. 🐌",
        "Die Datei ist einfach verschwunden! (Liegt im Papierkorb) 🗑️",
        "Können Sie kurz vorbeikommen? Übers Telefon verstehe ich das nicht. 🏃‍♂️",
        "Ich habe das Dokument gespeichert, aber ich weiß nicht wo. 📁",
        "Mein Headset geht nicht, hören Sie mich?! 🎧",
        "Können Sie mir mein Passwort verraten? Sie müssen das doch sehen können! 🔐",
        "Ich kann mich nicht einloggen! (Feststelltaste ist dauerhaft an) 🔡",
        "Der PC macht so ein komisches Geräusch, als würde er gleich abheben! 🛸",
        "Ich habe doch gar nichts angeklickt, das Fenster ging von ganz alleine auf! 🪟",
        "Ich gebe euch mal 5 Minuten eurer Zeit zurück. ⏱️",
        "Ich sehe eine Hand oben – ist das noch eine alte Hand oder eine neue Frage? 🙋‍♂️",
        "Könnt ihr mich alle gut hören? 🎙️",
        "Ich schicke den Link dazu gleich mal in den Chat. 💬",
        "Sorry für die Verspätung, der vorherige Termin hat etwas überzogen. 🏃💨"
    };

    private readonly string _startupGreeting;

    public GreetingService()
    {
        var random = new Random();
        _startupGreeting = _greetings[random.Next(_greetings.Length)];
    }

    public string GetStartupGreeting()
    {
        return _startupGreeting;
    }
}
