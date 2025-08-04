// =====================================
// 📦 TenderPriceService.cs – Full Rule Engine
// =====================================
using System;
using System.Collections.Generic;
using System.Linq;
using TenderPriceCalculator.Models;

public class TenderPriceService
{
    private readonly TenderRuleConfig _config;

    public TenderPriceService(TenderRuleConfig config)
    {
        _config = config;
    }

    public List<Item> ProcessTender(List<Item> items, TenderContext context)
    {
        var result = items.Select(item => item.Clone()).ToList();

        foreach (var item in result)
        {
            ApplyRulesRecursively(item, context, _config.TenderRulesTree);
        }

        ApplyRounding(result, context);

        return result;
    }

    private void ApplyRulesRecursively(Item item, TenderContext context, TenderRuleNode node)
    {
        if (!node.IsActive) return;

        if (!string.IsNullOrEmpty(node.MatchCondition) && !EvaluateCondition(item, context, node.MatchCondition))
            return;

        if (node.AppliesToTypes != null && node.AppliesToTypes.Any() && !node.AppliesToTypes.Contains(item.Type.ToString()))
            return;

        if (node.ActionId != null)
        {
            var action = _config.TenderRuleActions.FirstOrDefault(a => a.Id == node.ActionId);
            if (action != null)
                ApplyAction(item, action);
        }

        foreach (var child in node.Children)
        {
            ApplyRulesRecursively(item, context, child);
        }
    }

    private bool EvaluateCondition(Item item, TenderContext context, string condition)
    {
        try
        {
            return condition switch
            {
                "context.Total_KH >= context.Total_MIN" => context.Total_KH >= context.Total_MIN,
                "context.Total_MIN <= context.Total_KH && context.Total_KH <= context.Total_NY" =>
                    context.Total_MIN <= context.Total_KH && context.Total_KH <= context.Total_NY,
                "item.Price_MIN < item.Price_MAX" => item.Price_MIN < item.Price_MAX,
                "item.Price_MIN >= item.Price_MAX" => item.Price_MIN >= item.Price_MAX,
                "item.Price_KH < item.Price_MIN" => item.Price_KH < item.Price_MIN,
                "item.Price_NY < item.Price_KH" => item.Price_NY < item.Price_KH,
                "item.Price_MIN <= item.Price_KH && item.Price_KH <= item.Price_MAX" =>
                    item.Price_MIN <= item.Price_KH && item.Price_KH <= item.Price_MAX,
                "item.Price_MAX < item.Price_KH && item.Price_KH <= item.Price_NY" =>
                    item.Price_MAX < item.Price_KH && item.Price_KH <= item.Price_NY,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private void ApplyAction(Item item, TenderRuleAction action)
    {
        switch (action.ActionCode)
        {
            case "KEEP_INPUT_PRICE":
                item.PriceAfterAdjust = GetPriceBySource(item, action.InputPriceSource);
                item.PriceProposal = item.PriceAfterAdjust;
                break;

            case "SEQUENTIAL_REDUCE":
                var step = GetReductionStep(item.Type);
                ReduceItemToThreshold(item, step);
                break;

            case "SHOW_USER_CHOICE":
                Console.WriteLine($"Chọn giá cho {item.Name} (KH: {item.Price_KH}, MIN: {item.Price_MIN}): ");
                var input = Console.ReadLine();
                item.PriceAfterAdjust = input == "MIN" ? item.Price_MIN : item.Price_KH;
                item.PriceProposal = item.PriceAfterAdjust;
                break;

            case "DISABLE_REDUCTION":
                item.PriceAfterAdjust = item.PriceBeforeAdjust;
                item.PriceProposal = item.PriceBeforeAdjust;
                break;
        }
    }

    private void ApplyRounding(List<Item> items, TenderContext context)
    {
        foreach (var item in items)
        {
            var rule = _config.TenderRoundingConfig.FirstOrDefault(r => r.RuleId == item.MatchedRuleId);
            if (rule != null)
            {
                item.PriceProposal = RoundPrice(item.PriceProposal, rule);
            }
        }
    }

    private decimal RoundPrice(decimal price, TenderRoundingConfig rule)
    {
        return rule.Mode switch
        {
            "NEAREST" => Math.Round(price / rule.RoundTo) * rule.RoundTo,
            "UP" => Math.Ceiling(price / rule.RoundTo) * rule.RoundTo,
            "DOWN" => Math.Floor(price / rule.RoundTo) * rule.RoundTo,
            _ => price
        };
    }

    private decimal GetPriceBySource(Item item, string source)
    {
        return source switch
        {
            "KH" => item.Price_KH,
            "MIN" => item.Price_MIN,
            "NY" => item.Price_NY,
            "TD" => item.Price_TD,
            _ => item.Price_KH
        };
    }

    private void ReduceItemToThreshold(Item item, decimal step)
    {
        decimal current = item.PriceBeforeAdjust;
        while (current * (1 - step) >= item.Price_TTTN)
        {
            current *= (1 - step);
        }
        item.PriceAfterAdjust = current;
        item.PriceProposal = current;
    }

    private decimal GetReductionStep(ItemType type)
    {
        return type switch
        {
            ItemType.VATTU => 0.01m,
            ItemType.VTTH => 0.015m,
            ItemType.HOACHAT_CONTROL => 0.02m,
            ItemType.HOACHAT_CALIB => 0.02m,
            ItemType.HOACHAT_CHINH => 0.025m,
            _ => 0.01m,
        };
    }
}

// =====================================
// 📘 Models tương ứng bảng cấu hình
// =====================================
public class TenderRuleConfig
{
    public TenderRuleNode TenderRulesTree { get; set; }
    public List<TenderRuleAction> TenderRuleActions { get; set; }
    public List<TenderRoundingConfig> TenderRoundingConfig { get; set; }
}

public class TenderRule
{
    public Guid Id { get; set; }
    public string CaseCode { get; set; }
    public string RuleName { get; set; }
    public int Level { get; set; }
    public string MatchCondition { get; set; }
    public Guid? ActionId { get; set; }
    public string[] AppliesToTypes { get; set; }
    public bool IsActive { get; set; }
    public List<TenderRule> Children { get; set; } = new();
}

public class TenderRuleAction
{
    public Guid Id { get; set; }
    public string ActionCode { get; set; }
    public string HandlerName { get; set; }
    public string Description { get; set; }
    public string InputPriceSource { get; set; }
}

public class TenderRoundingConfig
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public int RoundTo { get; set; }
    public string Mode { get; set; }
    public string ExcelFormula { get; set; }
}

public class TenderContext
{
    public decimal Total_KH { get; set; }
    public decimal Total_MIN { get; set; }
    public decimal Total_NY { get; set; }
    public decimal Total_TD { get; set; }
}
public class TenderRuleNode
{
    public Guid Id { get; set; } // UUID định danh rule
    public string CaseCode { get; set; } = string.Empty; // Mã TH (TH2.1.1,...)
    public string RuleName { get; set; } = string.Empty; // Mô tả ngắn gọn
    public int Level { get; set; } // Cấp trong cây
    public string? MatchCondition { get; set; } // Điều kiện để áp dụng rule
    public Guid? ActionId { get; set; } // ID action áp dụng
    public List<string>? AppliesToTypes { get; set; } // Loại hàng áp dụng (VATTU, VTTH,...)
    public bool IsActive { get; set; } = true; // Rule có được áp dụng không
    public Guid? ParentId { get; set; } // ID của rule cha
    public List<TenderRuleNode> Children { get; set; } = new(); // Danh sách rule con
}
