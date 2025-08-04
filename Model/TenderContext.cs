using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TenderPriceCalculator.Model;

public class TenderContext
{
    public decimal Total_KH { get; set; }
    public decimal Total_MIN { get; set; }
    public decimal Total_TĐ { get; set; }
    public decimal Total_NY { get; set; }

    // Lựa chọn từ user cho bước 1: "KH" | "MIN" | "NY"
    public string UserChoice { get; set; } = "MIN";

    // Tỷ lệ điều chỉnh nếu TH3
    public decimal? AdjustmentRate { get; set; } // VD: 0.97m nghĩa là giảm 3%
}
