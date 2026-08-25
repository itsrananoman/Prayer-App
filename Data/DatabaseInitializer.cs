using Microsoft.EntityFrameworkCore;
using Prayer.Models;

namespace Prayer.Data;

public static class DatabaseInitializer
{
    public static void Initialize(PrayerDbContext context)
    {
        context.Database.EnsureCreated();

        // Auto-migrate schema changes for SQLite if existing database lacks new columns
        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE DailyVerses ADD COLUMN SourceAttribution TEXT DEFAULT '';");
        }
        catch { }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE UserSettings ADD COLUMN ReminderLeadMinutes INTEGER DEFAULT 5;");
        }
        catch { }

        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Surahs (
                    Number INTEGER PRIMARY KEY,
                    ArabicName TEXT NOT NULL,
                    EnglishName TEXT NOT NULL,
                    EnglishNameTranslation TEXT NOT NULL,
                    NumberOfAyahs INTEGER NOT NULL,
                    RevelationType TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS CachedAyahs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SurahNumber INTEGER NOT NULL,
                    AyahNumber INTEGER NOT NULL,
                    ArabicText TEXT NOT NULL,
                    UrduText TEXT NOT NULL,
                    EnglishText TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS ReadingProgress (
                    SurahNumber INTEGER PRIMARY KEY,
                    LastReadAyah INTEGER NOT NULL,
                    LastReadAt TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_CachedAyahs_SurahNumber_AyahNumber ON CachedAyahs (SurahNumber, AyahNumber);
            ");
        }
        catch { }

        // Seed default user settings if not present
        if (!context.UserSettings.Any())
        {
            context.UserSettings.Add(new UserSetting
            {
                City = "Karachi",
                Country = "PK",
                CalculationMethod = 1,
                LockDurationMinutes = 15,
                Theme = "Dark",
                PlayDefaultChime = true,
                AutostartEnabled = true,
                MinimizeToTrayOnClose = true
            });
            context.SaveChanges();
        }

        // Reseed or populate verified Quran & Sahih Hadiths
        try
        {
            bool needsSeed = !context.DailyVerses.Any() || !context.DailyVerses.Any(v => !string.IsNullOrEmpty(v.SourceAttribution));
            if (needsSeed)
            {
                context.DailyVerses.RemoveRange(context.DailyVerses);
                var verses = GetVerifiedDailyContent();
                context.DailyVerses.AddRange(verses);
                context.SaveChanges();
            }
        }
        catch
        {
            try
            {
                context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS DailyVerses;");
                context.Database.EnsureCreated();
                var verses = GetVerifiedDailyContent();
                context.DailyVerses.AddRange(verses);
                context.SaveChanges();
            }
            catch { }
        }
    }

    public static List<DailyVerse> GetVerifiedDailyContent()
    {
        const string QuranAttribution = "Source: Tanzil Project (quran-uthmani) · Urdu: Fateh Muhammad Jalandhry · English: Saheeh International";
        const string BukhariAttribution = "Source: Sahih al-Bukhari (Darussalam/Sunnah.com) · Verified Authentic";
        const string MuslimAttribution = "Source: Sahih Muslim (Darussalam/Sunnah.com) · Verified Authentic";

        return new List<DailyVerse>
        {
            // 1. Quran - Surah An-Nisa (4:103)
            new()
            {
                DayOfYear = 1,
                Type = "Quran",
                ArabicText = "إِنَّ الصَّلَاةَ كَانَتْ عَلَى الْمُؤْمِنِينَ كِتَابًا مَّوْقُوتًا",
                TranslationUrdu = "بے شک نماز مومنوں پر مقررہ اوقات میں فرض کی گئی ہے۔",
                TranslationEnglish = "Indeed, prayer has been decreed upon the believers a decree of specified times.",
                Reference = "Surah An-Nisa (4:103)",
                SourceAttribution = QuranAttribution
            },
            // 2. Hadith - Sahih al-Bukhari 527
            new()
            {
                DayOfYear = 2,
                Type = "Hadith",
                ArabicText = "سَأَلْتُ النَّبِيَّ صلى الله عليه وسلم أَىُّ الْعَمَلِ أَحَبُّ إِلَى اللَّهِ قَالَ: «الصَّلاَةُ عَلَى وَقْتِهَا»",
                TranslationUrdu = "میں نے نبی کریم ﷺ سے پوچھا کہ اللہ کو کون سا عمل سب سے زیادہ پسند ہے؟ آپ ﷺ نے فرمایا: اپنے وقت پر نماز پڑھنا۔",
                TranslationEnglish = "I asked the Prophet (ﷺ): 'Which deed is the dearest to Allah?' He replied: 'To perform the prayers at their proper times.'",
                Reference = "Sahih al-Bukhari 527 (Book 9, Hadith 5)",
                SourceAttribution = BukhariAttribution
            },
            // 3. Quran - Surah Al-Baqarah (2:43)
            new()
            {
                DayOfYear = 3,
                Type = "Quran",
                ArabicText = "وَأَقِيمُوا الصَّلَاةَ وَآتُوا الزَّكَاةَ وَارْكَعُوا مَعَ الرَّاكِعِينَ",
                TranslationUrdu = "اور نماز قائم رکھو اور زکوٰۃ دیا کرو اور رکوع کرنے والوں کے ساتھ رکوع کیا کرو۔",
                TranslationEnglish = "And establish prayer and give zakah and bow with those who bow [in worship and obedience].",
                Reference = "Surah Al-Baqarah (2:43)",
                SourceAttribution = QuranAttribution
            },
            // 4. Hadith - Sahih Muslim 283
            new()
            {
                DayOfYear = 4,
                Type = "Hadith",
                ArabicText = "مَثَلُ الصَّلَوَاتِ الْخَمْسِ كَمَثَلِ نَهْرٍ جَارٍ غَمْرٍ عَلَى بَابِ أَحَدِكُمْ يَغْتَسِلُ مِنْهُ كُلَّ يَوْمٍ خَمْسَ مَرَّاتٍ",
                TranslationUrdu = "پانچوں نمازوں کی مثال ایک گہری بہتی ہوئی نہر جیسی ہے جو تم میں سے کسی کے دروازے پر ہو اور وہ اس میں روزانہ پانچ بار نہاتا ہو۔",
                TranslationEnglish = "The similitude of five prayers is like an overflowing river running at the door of one of you in which he takes a bath five times a day.",
                Reference = "Sahih Muslim 283 (Book 5, Hadith 362)",
                SourceAttribution = MuslimAttribution
            },
            // 5. Quran - Surah Al-Baqarah (2:238)
            new()
            {
                DayOfYear = 5,
                Type = "Quran",
                ArabicText = "حَافِظُوا عَلَى الصَّلَوَاتِ وَالصَّلَاةِ الْوُسْطَىٰ وَقُومُوا لِلَّهِ قَانِتِينَ",
                TranslationUrdu = "سب نمازیں خصوصاً بیچ کی نماز (عصر) پورے التزام سے پڑھتے رہو اور اللہ کے آگے باادب کھڑے رہا کرو۔",
                TranslationEnglish = "Maintain with care the [obligatory] prayers and [in particular] the middle prayer and stand before Allah, devoutly obedient.",
                Reference = "Surah Al-Baqarah (2:238)",
                SourceAttribution = QuranAttribution
            },
            // 6. Hadith - Sahih al-Bukhari 554
            new()
            {
                DayOfYear = 6,
                Type = "Hadith",
                ArabicText = "مَنْ صَلَّى الْبَرْدَيْنِ دَخَلَ الْجَنَّةَ",
                TranslationUrdu = "جس نے دو ٹھنڈے وقتوں کی نمازیں (فجر اور عصر) باقاعدگی سے پڑھیں، وہ جنت میں داخل ہوگا۔",
                TranslationEnglish = "Whoever prays the two cool prayers (Asr and Fajr) will enter Paradise.",
                Reference = "Sahih al-Bukhari 554 (Book 9, Hadith 31)",
                SourceAttribution = BukhariAttribution
            },
            // 7. Quran - Surah Al-Ankabut (29:45)
            new()
            {
                DayOfYear = 7,
                Type = "Quran",
                ArabicText = "اتْلُ مَا أُوحِيَ إِلَيْكَ مِنَ الْكِتَابِ وَأَقِمِ الصَّلَاةَ ۖ إِنَّ الصَّلَاةَ تَنْهَىٰ عَنِ الْفَحْشَاءِ وَالْمُنكَرِ",
                TranslationUrdu = "جو کتاب آپ پر وحی کی گئی ہے اسے پڑھئے اور نماز قائم کیجئے، بے شک نماز بے حیائی اور برائی سے روکتی ہے۔",
                TranslationEnglish = "Recite what has been revealed to you of the Book and establish prayer. Indeed, prayer prohibits immorality and wrongdoing.",
                Reference = "Surah Al-Ankabut (29:45)",
                SourceAttribution = QuranAttribution
            },
            // 8. Hadith - Sahih Muslim 82
            new()
            {
                DayOfYear = 8,
                Type = "Hadith",
                ArabicText = "إِنَّ بَيْنَ الرَّجُلِ وَبَيْنَ الشِّرْكِ وَالْكُفْرِ تَرْكَ الصَّلاَةِ",
                TranslationUrdu = "انسان اور کفر و شرک کے درمیان حد فاصل نماز کو چھوڑ دینا ہے۔",
                TranslationEnglish = "Verily, between a person and polytheism and disbelief is the abandonment of prayer.",
                Reference = "Sahih Muslim 82 (Book 1, Hadith 154)",
                SourceAttribution = MuslimAttribution
            },
            // 9. Quran - Surah Taha (20:14)
            new()
            {
                DayOfYear = 9,
                Type = "Quran",
                ArabicText = "إِنَّنِي أَنَا اللَّهُ لَا إِلَٰهَ إِلَّا أَنَا فَاعْبُدْنِي وَأَقِمِ الصَّلَاةَ لِذِكْرِي",
                TranslationUrdu = "بے شک میں ہی اللہ ہوں، میرے سوا کوئی معبود نہیں، پس میری عبادت کرو اور میری یاد کے لیے نماز قائم کرو۔",
                TranslationEnglish = "Indeed, I am Allah. There is no deity except Me, so worship Me and establish prayer for My remembrance.",
                Reference = "Surah Taha (20:14)",
                SourceAttribution = QuranAttribution
            },
            // 10. Hadith - Sahih Muslim 725
            new()
            {
                DayOfYear = 10,
                Type = "Hadith",
                ArabicText = "رَكْعَتَا الْفَجْرِ خَيْرٌ مِنَ الدُّنْيَا وَمَا فِيهَا",
                TranslationUrdu = "فجر کی دو رکعتیں (سنتیں) دنیا اور جو کچھ دنیا میں ہے سب سے بہتر ہیں۔",
                TranslationEnglish = "The two Rak'ahs before the dawn (Fajr) prayer are better than this world and all that is in it.",
                Reference = "Sahih Muslim 725 (Book 6, Hadith 118)",
                SourceAttribution = MuslimAttribution
            },
            // 11. Quran - Surah Al-Muminun (23:1-2)
            new()
            {
                DayOfYear = 11,
                Type = "Quran",
                ArabicText = "قَدْ أَفْلَحَ الْمُؤْمِنُونَ ۝ الَّذِينَ هُمْ فِي صَلَاتِهِمْ خَاشِعُونَ",
                TranslationUrdu = "یقیناً وہ ایمان والے کامیاب ہو گئے جو اپنی نماز میں خشوع و خضوع اختیار کرتے ہیں۔",
                TranslationEnglish = "Certainly will the believers have succeeded: They who are during their prayer humbly submissive.",
                Reference = "Surah Al-Muminun (23:1-2)",
                SourceAttribution = QuranAttribution
            },
            // 12. Hadith - Sahih al-Bukhari 645
            new()
            {
                DayOfYear = 12,
                Type = "Hadith",
                ArabicText = "صَلاَةُ الْجَمَاعَةِ تَفْضُلُ صَلاَةَ الْفَذِّ بِسَبْعٍ وَعِشْرِينَ دَرَجَةً",
                TranslationUrdu = "باجماعت نماز کا ثواب اکیلے نماز پڑھنے پر ستائیس درجے فضیلت رکھتا ہے۔",
                TranslationEnglish = "The reward of the congregational prayer is twenty-seven times greater than that of the prayer offered alone.",
                Reference = "Sahih al-Bukhari 645 (Book 10, Hadith 40)",
                SourceAttribution = BukhariAttribution
            },
            // 13. Quran - Surah Al-Baqarah (2:153)
            new()
            {
                DayOfYear = 13,
                Type = "Quran",
                ArabicText = "يَا أَيُّهَا الَّذِينَ آمَنُوا اسْتَعِينُوا بِالصَّبْرِ وَالصَّلَاةِ ۚ إِنَّ اللَّهَ مَعَ الصَّابِرِينَ",
                TranslationUrdu = "اے ایمان والو! صبر اور نماز کے ذریعے مدد چاہا کرو، بے شک اللہ صبر کرنے والوں کے ساتھ ہے۔",
                TranslationEnglish = "O you who have believed, seek help through patience and prayer. Indeed, Allah is with the patient.",
                Reference = "Surah Al-Baqarah (2:153)",
                SourceAttribution = QuranAttribution
            },
            // 14. Hadith - Sahih Muslim 482
            new()
            {
                DayOfYear = 14,
                Type = "Hadith",
                ArabicText = "أَقْرَبُ مَا يَكُونُ الْعَبْدُ مِنْ رَبِّهِ وَهُوَ سَاجِدٌ فَأَكْثِرُوا الدُّعَاءَ",
                TranslationUrdu = "بندہ اپنے رب کے سب سے زیادہ قریب اس وقت ہوتا ہے جب وہ سجدے میں ہو، پس سجدے میں کثرت سے دعا کیا کرو۔",
                TranslationEnglish = "The nearest a servant comes to his Lord is when he is prostrating himself, so make abundant supplication (in it).",
                Reference = "Sahih Muslim 482 (Book 4, Hadith 245)",
                SourceAttribution = MuslimAttribution
            },
            // 15. Quran - Surah Al-Isra (17:78)
            new()
            {
                DayOfYear = 15,
                Type = "Quran",
                ArabicText = "أَقِمِ الصَّلَاةَ لِدُلُوكِ الشَّمْسِ إِلَىٰ غَسَقِ اللَّيْلِ وَقُرْآنَ الْفَجْرِ ۖ إِنَّ قُرْآنَ الْفَجْرِ كَانَ مَشْهُودًا",
                TranslationUrdu = "نماز قائم کیجئے زوالِ آفتاب سے لے کر رات کے اندھیرے تک اور فجر کا قرآن پڑھنا بھی، بے شک فجر کے وقت قرآن کا پڑھنا حاضری کا وقت ہوتا ہے۔",
                TranslationEnglish = "Establish prayer at the decline of the sun until the darkness of the night and [also] the Quran of dawn. Indeed, the recitation of dawn is ever witnessed.",
                Reference = "Surah Al-Isra (17:78)",
                SourceAttribution = QuranAttribution
            },
            // 16. Hadith - Sahih al-Bukhari 528
            new()
            {
                DayOfYear = 16,
                Type = "Hadith",
                ArabicText = "أَرَأَيْتُمْ لَوْ أَنَّ نَهَرًا بِبَابِ أَحَدِكُمْ يَغْتَسِلُ فِيهِ كُلَّ يَوْمٍ خَمْسًا، هَلْ يَبْقَى مِنْ دَرَنِهِ شَىْءٌ؟ قَالُوا لاَ يَبْقَى مِنْ دَرَنِهِ شَىْءٌ‏. قَالَ: «فَذَلِكَ مَثَلُ الصَّلَوَاتِ الْخَمْسِ يَمْحُو اللَّهُ بِهِمَا الْخَطَايَا»",
                TranslationUrdu = "نبی کریم ﷺ نے فرمایا: بتاؤ اگر تم میں سے کسی کے دروازے پر نہر ہو جس میں وہ روزانہ پانچ بار نہاتا ہو تو کیا اس کے جسم پر کوئی میل باقی رہے گا؟ صحابہ نے عرض کیا: کچھ نہیں۔ آپ ﷺ نے فرمایا: یہی مثال پانچوں نمازوں کی ہے، اللہ ان کے ذریعے گناہوں کو مٹا دیتا ہے۔",
                TranslationEnglish = "The Prophet (ﷺ) said: 'If there was a river at the door of anyone of you and he took a bath in it five times a day would you notice any dirt on him?' They said, 'None.' The Prophet (ﷺ) added: 'That is the example of the five prayers with which Allah blots out evil deeds.'",
                Reference = "Sahih al-Bukhari 528 (Book 9, Hadith 6)",
                SourceAttribution = BukhariAttribution
            },
            // 17. Quran - Surah Maryam (19:59)
            new()
            {
                DayOfYear = 17,
                Type = "Quran",
                ArabicText = "فَخَلَفَ مِن بَعْدِهِمْ خَلْفٌ أَضَاعُوا الصَّلَاةَ وَاتَّبَعُوا الشَّهَوَاتِ ۖ فَسَوْفَ يَلْقَوْنَ غَيًّا",
                TranslationUrdu = "پھر ان کے بعد ایسے ناخلف لوگ جانشین ہوئے جنہوں نے نماز کو ضائع کر دیا اور خواہشاتِ نفسانی کی پیروی کی، سو وہ عنقریب گمراہی کا انجام دیکھیں گے۔",
                TranslationEnglish = "But there came after them successors who neglected prayer and pursued desires; so they are going to meet evil.",
                Reference = "Surah Maryam (19:59)",
                SourceAttribution = QuranAttribution
            },
            // 18. Hadith - Sahih Muslim 657
            new()
            {
                DayOfYear = 18,
                Type = "Hadith",
                ArabicText = "مَنْ صَلَّى الْعِشَاءَ فِي جَمَاعَةٍ فَكَأَنَّمَا قَامَ نِصْفَ اللَّيْلِ وَمَنْ صَلَّى الصُّبْحَ فِي جَمَاعَةٍ فَكَأَنَّمَا صَلَّى اللَّيْلَ كُلَّهُ",
                TranslationUrdu = "جس نے عشاء کی نماز باجماعت پڑھی گویا اس نے آدھی رات قیام کیا، اور جس نے صبح (فجر) کی نماز باجماعت پڑھی گویا اس نے پوری رات نماز پڑھی۔",
                TranslationEnglish = "He who observed the 'Isha' prayer in congregation, it was as if he had prayed up to midnight, and he who also observed the dawn prayer in congregation, it was as if he had prayed the whole night.",
                Reference = "Sahih Muslim 657 (Book 5, Hadith 326)",
                SourceAttribution = MuslimAttribution
            },
            // 19. Quran - Surah Al-Jumu'ah (62:9)
            new()
            {
                DayOfYear = 19,
                Type = "Quran",
                ArabicText = "يَا أَيُّهَا الَّذِينَ آمَنُوا إِذَا نُودِيَ لِلصَّلَاةِ مِن يَوْمِ الْجُمُعَةِ فَاسْعَوْا إِلَىٰ ذِكْرِ اللَّهِ وَذَرُوا الْبَيْعَ",
                TranslationUrdu = "اے ایمان والو! جب جمعہ کے دن نماز کے لیے اذان دی جائے تو اللہ کے ذکر کی طرف لپکو اور خرید و فروخت چھوڑ دو۔",
                TranslationEnglish = "O you who have believed, when [the adhan] is called for the prayer on the day of Jumu'ah [Friday], then proceed to the remembrance of Allah and leave trade.",
                Reference = "Surah Al-Jumu'ah (62:9)",
                SourceAttribution = QuranAttribution
            },
            // 20. Hadith - Sahih al-Bukhari 533
            new()
            {
                DayOfYear = 20,
                Type = "Hadith",
                ArabicText = "إِذَا اشْتَدَّ الْحَرُّ فَأَبْرِدُوا بِالصَّلاَةِ، فَإِنَّ شِدَّةَ الْحَرِّ مِنْ فَيْحِ جَهَنَّمَ",
                TranslationUrdu = "جب گرمی شدید ہو تو ظہر کی نماز کو ٹھنڈے وقت میں پڑھو، کیونکہ گرمی کی شدت جہنم کے جوش سے ہے۔",
                TranslationEnglish = "When it is very hot, delay the prayer until it becomes cooler, for the severity of heat is from the raging of Hell.",
                Reference = "Sahih al-Bukhari 533 (Book 9, Hadith 11)",
                SourceAttribution = BukhariAttribution
            }
        };
    }
}
