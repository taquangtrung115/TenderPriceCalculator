// Program.cs - tách từng bước như API
using TenderPriceCalculator.Models;
using TenderPriceCalculator.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// === STEP 1: KHỞI TẠO DỮ LIỆU ===
var items = InitItems();
var context = InitContext(items);

Console.WriteLine("========== BƯỚC 1: KHỞI TẠO ==========");
Console.WriteLine($"Tổng KH: {context.Total_KH:N0}, Tổng MIN: {context.Total_MIN:N0}, Tổng TĐ: {context.Total_TD:N0}, Tổng NY: {context.Total_NY:N0}\n");

// === STEP 2: CHỌN RULE CẤP 1 ===
var rootRules = GetApplicableRootRules(context);
if (!rootRules.Any())
{
    Console.WriteLine("❌ Không thỏa rule cấp 1 nào.");
    return;
}
Console.WriteLine("========== BƯỚC 2: CHỌN RULE CẤP 1 ==========");
foreach (var rule in rootRules)
    Console.WriteLine($"→ {rule.CaseCode}: {rule.RuleName}");

var selectedRoot = rootRules.First(); // mặc định chọn rule đầu tiên
context.SelectedRuleCode = selectedRoot.CaseCode;
Console.WriteLine($"✅ Đã chọn: {context.SelectedRuleCode} - {selectedRoot.RuleName}\n");

// === STEP 3: CHỌN RULE CON (nếu có) ===
var subRules = GetApplicableChildRules(selectedRoot.CaseCode, context);
if (subRules.Any())
{
    Console.WriteLine("========== BƯỚC 3: CHỌN RULE CON ==========");
    foreach (var sub in subRules)
        Console.WriteLine($"→ {sub.CaseCode}: {sub.RuleName}");

    var selectedSub = subRules.First(); // mặc định chọn rule con đầu tiên
    context.SelectedRuleCode = selectedSub.CaseCode;
    Console.WriteLine($"✅ Đã chọn: {context.SelectedRuleCode} - {selectedSub.RuleName}\n");
}

// === STEP 4: CHỌN GIÁ GỐC ===
ApplyUserChoiceOrDefault(context.SelectedRuleCode, items, context);

// === STEP 5: XỬ LÝ GIÁ DỰ THẦU ===
Console.WriteLine("========== BƯỚC 5: XỬ LÝ GIÁ DỰ THẦU ==========");
var service = new TenderPriceService();
var result = service.ProcessTender(items, context);
PrintResult(result);

// === STEP 6: IN LOG GIẢM GIÁ (nếu có) ===
if (context.SelectedRuleCode != "TH2.1.1")
    PrintLogs(service.ApplyReductionByItemType(result));


// ===================== HÀM HỖ TRỢ =====================

List<Item> InitItems() => new()
{
    new() { Name = "VT A", Type = ItemType.VATTU, Price_MIN = 900000, Price_MAX = 880000, Price_TTTN = 870000, Price_NY = 950000, Price_KH = 920000, Quantity = 5 },
    new() { Name = "VT B", Type = ItemType.VATTU, Price_MIN = 850000, Price_MAX = 830000, Price_TTTN = 810000, Price_NY = 890000, Price_KH = 860000, Quantity = 4 },
    new() { Name = "TT A", Type = ItemType.VTTH, Price_MIN = 760000, Price_MAX = 780000, Price_TTTN = 770000, Price_NY = 800000, Price_KH = 780000, Quantity = 6 },
    new() { Name = "HC C", Type = ItemType.HOACHAT_CONTROL, Price_MIN = 1200000, Price_MAX = 1180000, Price_TTTN = 1170000, Price_NY = 1250000, Price_KH = 1220000, Quantity = 3 },
    new() { Name = "HC Main", Type = ItemType.HOACHAT_CHINH, Price_MIN = 1400000, Price_MAX = 1350000, Price_TTTN = 1300000, Price_NY = 1450000, Price_KH = 1420000, Quantity = 2 }
};

TenderContext InitContext(List<Item> items) => new()
{
    Total_MIN = items.Sum(i => i.Price_MIN * i.Quantity),
    Total_KH = items.Sum(i => i.Price_KH * i.Quantity),
    Total_TD = items.Sum(i => i.Price_MIN * i.Quantity) * 0.95m,
    Total_NY = items.Sum(i => i.Price_NY * i.Quantity)
};

List<TenderRuleConfig> GetApplicableRootRules(TenderContext context) => ConfigData.TenderRuleConfigs
    .Where(r => r.Level == 1 && r.MatchCondition?.Invoke(context) == true)
    .ToList();

List<TenderRuleConfig> GetApplicableChildRules(string parentCode, TenderContext context) => ConfigData.TenderRuleConfigs
    .Where(r => r.ParentCode == parentCode && r.MatchCondition?.Invoke(context) == true)
    .OrderBy(r => r.Level)
    .ToList();

void ApplyUserChoiceOrDefault(string ruleCode, List<Item> items, TenderContext context)
{
    if (ruleCode == "TH2.1" || ruleCode == "TH2.1.1")
    {
        Console.WriteLine("👉 Rule yêu cầu chọn MIN, tự động áp dụng");
        context.UserChoice = "MIN";
    }
    else
    {
        Console.WriteLine("👉 Hãy chọn giá gốc để tính giá dự thầu:");
        Console.WriteLine("1. Giá KẾ HOẠCH (KH)");
        Console.WriteLine("2. Giá MIN");
        Console.Write("Nhập lựa chọn (1 hoặc 2): ");
        var key = Console.ReadLine();
        context.UserChoice = key == "1" ? "KH" : "MIN";
        Console.WriteLine($"Bạn đã chọn: {context.UserChoice}\n");
    }

    foreach (var item in items)
        item.PriceBeforeAdjust = context.UserChoice == "KH" ? item.Price_KH : item.Price_MIN;
}

void PrintResult(List<Item> result)
{
    Console.WriteLine("✅ KẾT QUẢ:");
    foreach (var item in result)
        Console.WriteLine($"{item.Name} ({item.Type}): Gốc = {item.PriceBeforeAdjust:N0}, Giảm = {item.PriceAfterAdjust:N0}, Đề xuất = {item.PriceProposal:N0}");
}

void PrintLogs(List<ReductionLog> logs)
{
    Console.WriteLine("\n🧾 LOG GIẢM GIÁ:");
    foreach (var log in logs)
    {
        Console.WriteLine($"Mặt hàng: {log.ItemName} ({log.Type})");
        Console.WriteLine($"  Gốc: {log.OriginalPrice:N0}, Ngưỡng: {log.ThresholdPrice:N0}, Bước: {log.StepPercent:P}");
        foreach (var step in log.PriceSteps)
            Console.WriteLine($"    → {step:N0}");
    }
}
