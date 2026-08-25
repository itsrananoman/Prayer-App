using System.Reflection;

namespace Prayer.ViewModels;

public class AboutViewModel : ViewModelBase
{
    public string AppTitle => "PRAYER (صلوٰۃ)";
    public string AppTagline => "Your screen waits. Salah doesn't.";

    public string Version
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v != null ? $"Version {v.Major}.{v.Minor}.{v.Build}" : "Version 2.0.0";
        }
    }

    public string Developer => "Developed by Rana Noman";
    public string Publisher => "DevCrafters";
    public string Copyright => "All Rights Reserved © 2026 DevCrafters";

    public string MissionStatement =>
        "In our modern, fast-paced work and digital environments, it is easy to get immersed in tasks and unintentionally delay our daily worship. " +
        "Prayer is designed to build steadfast consistency in Salah by providing full-screen focus locks, timely reminders, authentic Quranic reflection, " +
        "and accurate prayer calculations across all Windows devices.";

    public string AladhanAttribution => "Prayer calculation engine powered by the Aladhan API (aladhan.com).";
    public string QuranAttribution => "Quran text and translations sourced from the Tanzil Project via Al-Quran Cloud (alquran.cloud). Arabic Uthmani text with Fateh Muhammad Jalandhry Urdu translation and Saheeh International English translation.";
    public string HadithAttribution => "Hadith narrations restricted strictly to verified authentic collections: Sahih al-Bukhari and Sahih Muslim.";
}
