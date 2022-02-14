using System;
using System.Text;
using System.Globalization;

namespace Hourado
{
    class ConvertDateTime
    {
        public static string m2sh(DateTime dt)
        {
            try
            {
                PersianCalendar pc = new PersianCalendar();
                StringBuilder sb = new StringBuilder();

                sb.Append(pc.GetDayOfMonth(dt).ToString("00"));
                sb.Append("/");
                sb.Append(pc.GetMonth(dt).ToString("00"));
                sb.Append("/");
                sb.Append(pc.GetYear(dt).ToString("0000"));
                sb.Append(" ");
                sb.Append(pc.GetHour(dt).ToString("00"));
                sb.Append(":");
                sb.Append(pc.GetMinute(dt).ToString("00"));
                sb.Append(":");
                sb.Append(pc.GetSecond(dt).ToString("00"));

                return sb.ToString();
            }
            catch (Exception)
            {

            }

            return null;
        }

        public static DateTime sh2m(String s)
        {
            try
            {
                int day = Convert.ToInt32(s.Substring(0, 2));
                int month = Convert.ToInt32(s.Substring(5, 2));
                int year = Convert.ToInt32(s.Substring(10, 4));

                DateTime now = DateTime.Now;

                DateTime georgianDateTime = new DateTime(year, month, day, now.Hour, now.Minute, now.Second, new PersianCalendar());

                return georgianDateTime;
            }
            catch (Exception)
            {

            }

            return default(DateTime);
        }
    }
}