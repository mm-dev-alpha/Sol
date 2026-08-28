using System;
using System.Collections.Generic;
using System.Linq;
using Sol.Helpers;
using Sol.Models;
using Sol.ViewModels;
using Xunit;

namespace Sol.Tests;

public class UserWorkspaceEnhancementsTests
{
    [Fact]
    public void GenerateSecurePassword_GeneratesValidComplexPassword()
    {
        for (int i = 0; i < 20; i++)
        {
            var pwd = UserWorkspaceViewModel.GenerateSecurePassword();
            
            Assert.NotNull(pwd);
            Assert.Equal(16, pwd.Length);
            Assert.Contains(pwd, c => char.IsUpper(c));
            Assert.Contains(pwd, c => char.IsLower(c));
            Assert.Contains(pwd, c => char.IsDigit(c));
            Assert.Contains(pwd, c => "!@#$%^&*-_+=".Contains(c));
        }
    }

    [Fact]
    public void Strings_GermanTerminology_ConformsToMicrosoftStandard()
    {
        Strings.CurrentLanguage = "de";

        Assert.Equal("Kennwort zurücksetzen", Strings.S.ResetPasswordBtn);
        Assert.Equal("Kennwort bei nächster Anmeldung ändern", Strings.S.MustChangePasswordCheckbox);
        Assert.Equal("Konto entsperren, falls gesperrt", Strings.S.UnlockAccountCheckbox);
        Assert.Equal("Gruppenmitgliedschaften", Strings.S.GroupsTitle);
        Assert.Equal("Startseite", Strings.S.NavHome);
        Assert.Equal("Benutzer-Arbeitsbereich", Strings.S.NavUserWorkspace);
        Assert.Equal("Computer-Arbeitsbereich", Strings.S.NavComputerWorkspace);
        Assert.Equal("Sicherheitskennung (SID)", Strings.S.SidLabel);
        Assert.Equal("BitLocker-Wiederherstellungsschlüssel", Strings.S.BitLockerKeysTitle);
        Assert.Equal("Wiederherstellungsschlüssel in die Zwischenablage kopiert.", Strings.S.BitLockerKeyCopiedSuccess);
        Assert.Equal("Startseite", Strings.S.HomeBtn);
        Assert.Equal("Einstellungen", Strings.S.SettingsTitle);

        Strings.CurrentLanguage = "en";
    }

    [Fact]
    public void AdComputer_Model_HydratesCorrectly()
    {
        var computer = new AdComputer
        {
            Name = "DESKTOP-TEST01",
            SamAccountName = "DESKTOP-TEST01$",
            DnsHostName = "desktop-test01.corp.contoso.com",
            OperatingSystem = "Windows 11 Enterprise",
            OperatingSystemVersion = "10.0 (26100)",
            IsEnabled = true,
            AccountStatus = "Enabled",
            ManagedBy = "Fabiola Mustermann",
            Groups = new List<string> { "Domain Computers", "Workstations" },
            BitLockerKeys = new List<BitLockerKeyInfo>
            {
                new()
                {
                    KeyId = "{11111111-2222-3333-4444-555555555555}",
                    RecoveryPassword = "123456-789012-345678-901234-567890-123456-789012-345678",
                    Created = new DateTime(2026, 1, 15, 10, 30, 0)
                }
            }
        };

        Assert.Equal("DESKTOP-TEST01", computer.Name);
        Assert.Equal("desktop-test01.corp.contoso.com", computer.DnsHostName);
        Assert.True(computer.IsEnabled);
        Assert.Single(computer.BitLockerKeys);
        Assert.Equal("123456-789012-345678-901234-567890-123456-789012-345678", computer.BitLockerKeys[0].RecoveryPassword);
        Assert.Equal(2, computer.Groups.Count);
    }
}
