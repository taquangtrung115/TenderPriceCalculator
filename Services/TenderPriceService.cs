// TenderPriceService.cs
using TenderPriceCalculator.Models;

namespace TenderPriceCalculator.Services;

public class TenderPriceService
{
    /// <summary>
    /// Hàm chính xử lý logic chọn phù hợp rule (TH2.x / TH3.x)
    /// Gọi từng bước xử lý theo loại hàng hoặc theo yêu cầu rule
    /// </summary>
    public List<Item> ProcessTender(List<Item> items, TenderContext context)
    {
        // Clone danh sách item để tránh thay đổi gốc
        var result = items.Select(item => item.Clone()).ToList();

        // Phân chia xử lý theo logic từng rule
        if (context.SelectedRuleCode == "TH2.1.1")
        {
            ApplyKeepOriginalPrice(result); // giữ nguyên giá
        }
        else if (context.SelectedRuleCode == "TH2.2.1")
        {
            ApplyReductionByItemType(result); // giảm theo từng loại hàng
        }
        else if (context.SelectedRuleCode == "TH3")
        {
            ApplyGeneralReduction(result, 0.05m); // giảm toàn bộ 5% ví dụ
        }
        else
        {
            // Mặc định: giảm theo loại hàng
            ApplyReductionByItemType(result);
        }

        return result;
    }

    /// <summary>
    /// TH2.1.1 – Không giảm, giữ nguyên giá sau điều chỉnh = giá trước điều chỉnh
    /// </summary>
    public void ApplyKeepOriginalPrice(List<Item> items)
    {
        foreach (var item in items)
        {
            item.PriceAfterAdjust = item.PriceBeforeAdjust;
            item.PriceProposal = item.PriceBeforeAdjust;
        }
    }

    /// <summary>
    /// TH2.2.1 hoặc mặc định – Giảm giá theo từng loại hàng hóa
    /// </summary>
    public List<ReductionLog> ApplyReductionByItemType(List<Item> items)
    {
        var logs = new List<ReductionLog>();

        foreach (var group in items.GroupBy(i => i.Type))
        {
            var step = GetReductionStep(group.Key);
            foreach (var item in group)
            {
                var log = new ReductionLog
                {
                    ItemName = item.Name,
                    Type = item.Type,
                    StepPercent = step,
                    OriginalPrice = item.PriceBeforeAdjust,
                    ThresholdPrice = item.Price_TTTN,
                    PriceSteps = new List<decimal>()
                };

                // Thực hiện giảm tuần tự tới khi đạt TTTN hoặc hết bước
                decimal current = item.PriceBeforeAdjust;
                while (current * (1 - step) >= item.Price_TTTN)
                {
                    current *= (1 - step);
                    log.PriceSteps.Add(current);
                }

                item.PriceAfterAdjust = current;
                item.PriceProposal = current;
                logs.Add(log);
            }
        }

        return logs;
    }

    /// <summary>
    /// TH3 – Giảm toàn bộ theo phần trăm (ví dụ 5%)
    /// </summary>
    public void ApplyGeneralReduction(List<Item> items, decimal percent)
    {
        foreach (var item in items)
        {
            var newPrice = item.PriceBeforeAdjust * (1 - percent);
            item.PriceAfterAdjust = newPrice;
            item.PriceProposal = newPrice;
        }
    }

    /// <summary>
    /// Trả về bước giảm theo loại hàng (tuỳ chỉnh được nếu có yêu cầu khác nhau)
    /// </summary>
    private decimal GetReductionStep(ItemType type)
    {
        return type switch
        {
            ItemType.VATTU => 0.01m,               // 1%
            ItemType.VTTH => 0.015m,              // 1.5%
            ItemType.HOACHAT_CONTROL => 0.02m,
            ItemType.HOACHAT_CALIB => 0.02m,
            ItemType.HOACHAT_CHINH => 0.025m,
            _ => 0.01m,
        };
    }
}
