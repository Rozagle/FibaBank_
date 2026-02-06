using System;

namespace FibaPlus_Bank.Helpers
{
    public static class NumberToText
    {
        public static string Convert(decimal amount, string currencyCode = "TL")
        {
            var lira = (long)amount;
            var kurus = (long)((amount - lira) * 100);

            return $"YALNIZ {NumberToTextTR(lira)} {currencyCode} {NumberToTextTR(kurus)} KURUŞTUR.".ToUpper();
        }

        private static string NumberToTextTR(long number)
        {
            if (number == 0) return "SIFIR";

            string[] birler = { "", "BİR", "İKİ", "ÜÇ", "DÖRT", "BEŞ", "ALTI", "YEDİ", "SEKİZ", "DOKUZ" };
            string[] onlar = { "", "ON", "YİRMİ", "OTUZ", "KIRK", "ELLİ", "ALTMIŞ", "YETMİŞ", "SEKSEN", "DOKSAN" };
            string[] binler = { "", "BİN", "MİLYON", "MİLYAR", "TRİLYON" };

            if (number < 10) return birler[number];

            string text = "";
            int i = 0;

            while (number > 0)
            {
                long mod = number % 1000;
                if (mod > 0)
                {
                    string block = "";
                    if (mod / 100 > 0) block += (mod / 100 == 1 ? "" : birler[mod / 100]) + "YÜZ";
                    block += onlar[(mod % 100) / 10];
                    block += birler[mod % 10];

                    if (i == 1 && mod == 1) block = ""; 

                    text = block + binler[i] + text;
                }
                i++;
                number /= 1000;
            }
            return text;
        }
    }
}