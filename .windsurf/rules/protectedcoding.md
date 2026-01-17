---
trigger: always_on
---

# SECTION 3.5: PRE-CODING PROPOSAL PROTOCOL (The "Measure Twice" Rule)

## 1. Trigger Condition
**WHEN**: Before writing ANY code that modifies existing files or creates new logical modules in **Phase 5**.
**ACTION**: You MUST PAUSE, output the "Modification Proposal" table, and WAIT for user approval.

## 2. Proposal Content Requirements
You must analyze and report:
* **Target**: File path and specific line ranges.
* **Scope**: Minimal (1-5 lines), Moderate (Function level), Major (Architecture level).
* **Rationale**: Why is this change necessary? (Link to Requirement/Bug).
* **Feasibility**: Can this be done with current context? (High/Medium/Low).
* **Success Probability**: Estimated chance of fixing the issue without regression (0-100%).

## 3. Proposal Output Template
Use this exact Markdown structure:

---
### 🛡️ 修改方案提案 (Modification Proposal)

| 维度 (Dimension) | 详细信息 (Details) |
| :--- | :--- |
| 📍 **修改位置** | `src/utils/auth.py` (Line 45-52) & `src/api/login.py` (Line 12) |
| 🔧 **修改程度** | **Moderate** (修改了 Token 验证逻辑) |
| 💡 **修改原因** | 解决 Token 过期时间未正确解析导致的 401 错误 |
| ⚖️ **可行性判断** | **High** (无需引入新依赖，逻辑清晰) |
| 🎯 **成功率预估** | **95%** |

**请审核方案：输入 'Y' 继续，或输入修改意见。**
---

## 4. Strict Enforcement
* **DO NOT** write the code block immediately after the proposal.
* **STOP** generation.
* **WAIT** for user input (Y/N/Comments).