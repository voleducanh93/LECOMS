using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Common.Helper
{
    public static class DateRangeHelper
    {
        /// <summary>
        /// Resolve khoảng thời gian cho SellerDashboard V5.
        /// 
        /// Ưu tiên:
        /// - Nếu view = "custom" -> dùng from/to (bắt buộc có đủ).
        /// - Nếu không, dùng 'date' (nếu null thì dùng hôm nay) và:
        ///   + view = day     -> từ 00:00 đến 23:59:59 của ngày đó
        ///   + view = week    -> tuần chứa ngày đó (Mon-Sun)
        ///   + view = month   -> tháng chứa ngày đó
        ///   + view = quarter -> quý chứa ngày đó
        ///   + view = year    -> năm chứa ngày đó
        /// </summary>
        public static (DateTime from, DateTime to, DateTime baseDate, string normalizedView)
            ResolveSellerDashboardRangeV5(
                string? view,
                DateTime? date,
                DateTime? from,
                DateTime? to)
        {
            // Chuẩn hoá view
            var v = (view ?? "day").Trim().ToLowerInvariant();

            // Nếu custom -> phải có from/to
            if (v == "custom")
            {
                if (!from.HasValue || !to.HasValue)
                    throw new ArgumentException("For view 'custom', both 'from' and 'to' are required.");

                var f = from.Value;
                var t = to.Value;

                if (f > t)
                    throw new ArgumentException("'from' must be less than or equal to 'to'.");

                return (f, t, f, "custom");
            }

            // Base date: nếu FE không truyền thì lấy hôm nay (UTC)
            var baseDate = (date ?? DateTime.UtcNow).Date;

            DateTime fromDate;
            DateTime toDate;

            switch (v)
            {
                case "day":
                    fromDate = baseDate;
                    toDate = baseDate.AddDays(1).AddTicks(-1);
                    break;

                case "week":
                    // Tuần bắt đầu từ Monday
                    int diff = ((int)baseDate.DayOfWeek + 6) % 7; // Monday = 0
                    var weekStart = baseDate.AddDays(-diff);
                    fromDate = weekStart;
                    toDate = weekStart.AddDays(7).AddTicks(-1);
                    break;

                case "month":
                    var monthStart = new DateTime(baseDate.Year, baseDate.Month, 1);
                    fromDate = monthStart;
                    toDate = monthStart.AddMonths(1).AddTicks(-1);
                    break;

                case "quarter":
                    int quarterIndex = (baseDate.Month - 1) / 3; // 0..3
                    int qStartMonth = quarterIndex * 3 + 1;      // 1,4,7,10
                    var quarterStart = new DateTime(baseDate.Year, qStartMonth, 1);
                    fromDate = quarterStart;
                    toDate = quarterStart.AddMonths(3).AddTicks(-1);
                    break;

                case "year":
                    var yearStart = new DateTime(baseDate.Year, 1, 1);
                    fromDate = yearStart;
                    toDate = yearStart.AddYears(1).AddTicks(-1);
                    break;

                default:
                    // fallback = day
                    fromDate = baseDate;
                    toDate = baseDate.AddDays(1).AddTicks(-1);
                    v = "day";
                    break;
            }

            return (fromDate, toDate, baseDate, v);
        }
    }
}
