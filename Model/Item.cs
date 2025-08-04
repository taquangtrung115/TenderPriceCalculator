// ==================== TenderPricingConfig ====================
// ✅ Full Implementation: Configurable Tender Pricing Logic with Hierarchical Rules

using System;
using System.Collections.Generic;
using System.Linq;

namespace TenderPriceCalculator.Models
{
    public enum ItemType
    {
        VATTU,
        VTTH,
        HOACHAT_CONTROL,
        HOACHAT_CALIB,
        HOACHAT_CHINH
    }

    public class Item
    {
        public Guid Id { get; set; }
        public Guid MatchedRuleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ItemType Type { get; set; }
        public decimal Price_MIN { get; set; }
        public decimal Price_MAX { get; set; }
        public decimal Price_TTTN { get; set; }
        public decimal Price_NY { get; set; }
        public decimal Price_TD { get; set; }
        public decimal Price_KH { get; set; }
        public decimal PriceBeforeAdjust { get; set; }
        public decimal PriceAfterAdjust { get; set; }
        public decimal PriceProposal { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalValue => Price_KH * Quantity;

        public bool ShouldReducePrice(decimal baseValue) => baseValue >= Math.Max(Price_MAX, Price_TTTN);
        public Item Clone()
        {
            return new Item
            {
                Name = this.Name,
                Type = this.Type,
                Quantity = this.Quantity,
                Price_KH = this.Price_KH,
                Price_MIN = this.Price_MIN,
                Price_MAX = this.Price_MAX,
                Price_TTTN = this.Price_TTTN,
                Price_TD = this.Price_TD,
                Price_NY = this.Price_NY,
                PriceBeforeAdjust = this.PriceBeforeAdjust,
                PriceAfterAdjust = this.PriceAfterAdjust,
                PriceProposal = this.PriceProposal
            };
        }
    }

    public class TenderContext
    {
        public decimal Total_KH { get; set; }
        public decimal Total_MIN { get; set; }
        public decimal Total_TD { get; set; }
        public decimal Total_NY { get; set; }
        public string UserChoice { get; set; } = "MIN";
        public string SelectedRuleCode { get; set; }
        public decimal? AdjustmentRate { get; set; } = null;
    }

    public class TenderRuleConfig
    {
        public string CaseCode { get; set; } = "";  // VD: TH2, TH2.1, TH2.2.1
        public string RuleName { get; set; } = "";
        public int Level { get; set; } = 1;          // Cấp độ: 1, 2, 3...
        public string? ParentCode { get; set; }      // Cha của rule này (nếu có)
        public Func<TenderContext, bool>? MatchCondition { get; set; } // Điều kiện áp dụng rule
        public bool AllowUserChoicePrice { get; set; } = false;
        public bool AllowAutoReduction { get; set; } = true;
        public bool ApplyItemTypeReduction { get; set; } = true;
        public bool RoundAfterAdjust { get; set; } = true;
    }

    public class ItemReductionRule
    {
        public ItemType ItemType { get; set; }
        public decimal ThresholdPercent { get; set; }
        public decimal StepPercent { get; set; }
        public bool SortByTotalValueFirst { get; set; } = true;
        public bool SortByLowCustomerCountFirst { get; set; } = true;
        public bool ReduceAllOrSingle { get; set; } = false;
    }

    public class CeilingConfig
    {
        public decimal VATRate { get; set; } = 0.08m;
        public decimal CeilingStep { get; set; } = 1000;
        public decimal FinalFactor { get; set; } = 1.05m;
    }

    public class ReductionLog
    {
        public string ItemName { get; set; } = string.Empty;
        public ItemType Type { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal ThresholdPrice { get; set; }
        public decimal StepPercent { get; set; }
        public List<decimal> PriceSteps { get; set; } = new();
    }

    public static class ConfigData
    {
        public static List<TenderRuleConfig> TenderRuleConfigs = new()
        {
            new TenderRuleConfig {
                CaseCode = "TH2", RuleName = "Tổng TĐ < Tổng MIN", Level = 1,
                MatchCondition = ctx => ctx.Total_TD < ctx.Total_MIN
            },
            new TenderRuleConfig {
                CaseCode = "TH2.1", RuleName = "Tổng MIN <= KH <= NY", Level = 2, ParentCode = "TH2",
                MatchCondition = ctx => ctx.Total_MIN <= ctx.Total_KH && ctx.Total_KH <= ctx.Total_NY
            },
            new TenderRuleConfig {
                CaseCode = "TH2.2", RuleName = "TĐ <= KH < MIN", Level = 2, ParentCode = "TH2",
                MatchCondition = ctx => ctx.Total_TD <= ctx.Total_KH && ctx.Total_KH < ctx.Total_MIN,
                AllowUserChoicePrice = true
            },
            new TenderRuleConfig {
                CaseCode = "TH2.3", RuleName = "KH < TĐ", Level = 2, ParentCode = "TH2",
                MatchCondition = ctx => ctx.Total_KH < ctx.Total_TD,
                AllowUserChoicePrice = true
            },
            new TenderRuleConfig {
                CaseCode = "TH2.4", RuleName = "NY < KH", Level = 2, ParentCode = "TH2",
                MatchCondition = ctx => ctx.Total_NY < ctx.Total_KH
            },
            new TenderRuleConfig {
                CaseCode = "TH3", RuleName = "Tổng TĐ > Tổng NY", Level = 1,
                MatchCondition = ctx => ctx.Total_TD > ctx.Total_NY,
                AllowUserChoicePrice = true,
                AllowAutoReduction = false
            }
        };

        public static List<ItemReductionRule> ItemReductionRules = new()
        {
            new ItemReductionRule { ItemType = ItemType.VATTU, ThresholdPercent = 0.80m, StepPercent = 0.01m },
            new ItemReductionRule { ItemType = ItemType.VTTH, ThresholdPercent = 1.00m, StepPercent = 0.0005m },
            new ItemReductionRule { ItemType = ItemType.HOACHAT_CONTROL, ThresholdPercent = 1.00m, StepPercent = 0.0005m },
            new ItemReductionRule { ItemType = ItemType.HOACHAT_CALIB, ThresholdPercent = 1.00m, StepPercent = 0.0005m },
            new ItemReductionRule { ItemType = ItemType.HOACHAT_CHINH, ThresholdPercent = 1.00m, StepPercent = 0.0005m }
        };

        public static CeilingConfig GlobalCeiling = new()
        {
            VATRate = 0.08m,
            CeilingStep = 1000,
            FinalFactor = 1.05m
        };
    }
}