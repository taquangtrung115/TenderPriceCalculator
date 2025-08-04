-- ============================================
-- 🎯 Tender Rule Engine – Cấu hình DB PostgreSQL
-- ============================================

-- ============================================
-- 📘 BẢNG 1: Hành động xử lý khi match rule
-- ============================================
CREATE TABLE TenderRuleActions (
  Id UUID PRIMARY KEY,
  ActionCode VARCHAR(50) NOT NULL,         -- Mã xử lý (KEEP_INPUT_PRICE, SEQUENTIAL_REDUCE,...)
  HandlerName TEXT NOT NULL,               -- Tên hàm xử lý trong backend
  Description TEXT,                        -- Mô tả chi tiết
  InputPriceSource VARCHAR(10)             -- Chỉ dùng cho hành động giữ giá (KH, MIN, NY, TD)
);

CREATE UNIQUE INDEX idx_action_code ON TenderRuleActions(ActionCode);


-- ============================================
-- 📘 BẢNG 2: Cây logic rule các trường hợp tính giá
-- ============================================
CREATE TABLE TenderRules (
  Id UUID PRIMARY KEY,
  ParentId UUID REFERENCES TenderRules(Id),       -- Rule cha (nếu có)
  CaseCode VARCHAR(50) NOT NULL,                  -- Mã rule theo tài liệu (TH2, TH2.1.1,...)
  RuleName TEXT NOT NULL,                         -- Tên mô tả rule
  Level INT NOT NULL,                             -- Cấp trong cây (1, 2, 3,...)
  MatchCondition TEXT,                            -- Điều kiện áp dụng
  ActionId UUID REFERENCES TenderRuleActions(Id), -- Hành động xử lý
  AppliesToTypes TEXT[],                          -- Áp dụng cho loại hàng nào
  IsActive BOOLEAN DEFAULT TRUE                   -- Có đang sử dụng không
);

CREATE INDEX idx_tenderrules_parent ON TenderRules(ParentId);
CREATE INDEX idx_tenderrules_action ON TenderRules(ActionId);
CREATE UNIQUE INDEX idx_tenderrules_casecode ON TenderRules(CaseCode);


-- ============================================
-- 📘 BẢNG 3: Cấu hình làm tròn
-- ============================================
CREATE TABLE TenderRoundingConfig (
  Id UUID PRIMARY KEY,
  RuleId UUID REFERENCES TenderRules(Id),      -- Liên kết đến rule cụ thể
  RoundTo INT NOT NULL,                        -- Làm tròn đến số nào (vd: 1000)
  Mode VARCHAR(10) NOT NULL,                   -- Kiểu làm tròn (NEAREST, UP, DOWN)
  ExcelFormula TEXT                            -- Công thức tương đương trong Excel (ví dụ: =ROUND(A2,-3))
);

CREATE INDEX idx_rounding_ruleid ON TenderRoundingConfig(RuleId);


INSERT INTO TenderRuleActions (Id, ActionCode, HandlerName, Description, InputPriceSource) VALUES
  ('ac01'::uuid, 'KEEP_INPUT_PRICE', 'ApplyKeepInputPrice', 'Giữ nguyên giá đã chọn (KH/MIN/NY/TĐ)', 'KH'),
  ('ac02'::uuid, 'SEQUENTIAL_REDUCE', 'ApplySequentialReduction', 'Giảm giá lần lượt từng mặt hàng đến khi thỏa', NULL),
  ('ac03'::uuid, 'SHOW_USER_CHOICE', 'AskUserToSelectInputPrice', 'Yêu cầu người dùng chọn giá KH/MIN theo từng item', NULL),
  ('ac04'::uuid, 'DISABLE_REDUCTION', 'DisableReduction', 'Không áp dụng quy tắc giảm giá - giữ nguyên', NULL);


  INSERT INTO TenderRoundingConfig (Id, RuleId, RoundTo, Mode, ExcelFormula) VALUES
  ('r01'::uuid, 'f23532ea-193e-4c63-91bb-8892385a02df'::uuid, 1000, 'NEAREST', '=ROUND(A2, -3)'),
  ('r02'::uuid, 'some-uuid-th3.1'::uuid, 1000, 'NEAREST', '=MROUND(A2, 1000)');
