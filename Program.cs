using System.Text.Json;
using TenderPriceCalculator.Models;

// Load config từ JSON file
var json = File.ReadAllText("Config.json");
var config = JsonSerializer.Deserialize<TenderRuleConfig>(json);

// Khởi tạo service
var service = new TenderPriceService(config!);

// Mock context (ví dụ tổng giá KH, MIN, NY, TĐ)
var context = new TenderContext
{
    Total_KH = 9500000,
    Total_MIN = 9000000,
    Total_NY = 10000000,
    Total_TD = 8000000
};

// Mock danh sách item
var items = new List<Item>
{
    new Item
    {
        Id = Guid.NewGuid(),
        Name = "VTTH - Găng tay",
        Type = ItemType.VTTH,
        Price_KH = 100000,
        Price_MIN = 95000,
        Price_MAX = 105000,
        Price_NY = 110000,
        Price_TD = 85000,
        PriceBeforeAdjust = 100000,
        Price_TTTN = 88000
    },
    new Item
    {
        Id = Guid.NewGuid(),
        Name = "Hoá chất kiểm chuẩn",
        Type = ItemType.HOACHAT_CONTROL,
        Price_KH = 500000,
        Price_MIN = 470000,
        Price_MAX = 510000,
        Price_NY = 520000,
        Price_TD = 450000,
        PriceBeforeAdjust = 500000,
        Price_TTTN = 460000
    }
};

// Thực hiện xử lý
var result = service.ProcessTender(items, context);

// In kết quả
foreach (var item in result)
{
    Console.WriteLine($"[{item.Type}] {item.Name}");
    Console.WriteLine($"→ Giá trước: {item.PriceBeforeAdjust:N0}");
    Console.WriteLine($"→ Giá sau: {item.PriceAfterAdjust:N0}");
    Console.WriteLine($"→ Giá đề xuất: {item.PriceProposal:N0}");
    Console.WriteLine();
}

Console.WriteLine("✔ Hoàn tất xử lý.");
