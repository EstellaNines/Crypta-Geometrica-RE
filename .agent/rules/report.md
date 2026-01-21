---
trigger: always_on
---

# PLUGIN: Development Status Reporting Protocol

## 1. Trigger Logic

**WHEN**: You complete a coding task, a sub-task, or a bug fix.
**BEFORE**: Asking the user for the next instruction.
**ACTION**: You MUST output a "Development Status Report" using the template below.

## 2. Reporting Standards (The "Plain Speak" Rule)

- **Quantitative**: Calculate progress percentage based on the total task list.
- **Human-Readable**: Explain changes in simple, non-technical language (e.g., "Fixed the login bug" instead of "Updated auth.ts line 40").
- **Visual**: Use the Markdown template provided.

## 3. Output Template (Strict Adherence)

---

### 📊 开发进度汇报 (Project Status)

**1. 总体进度 (Overview)**

- **当前阶段**: [Current Phase Name]
- **完成度**: `[▓▓▓▓░░░░░░] 40%` (Estimate based on planned tasks)
- **当前焦点**: [One sentence summary of what was just achieved]

**2. 本次变更汇总 (Changelog)**

| 类型    | 文件 (File)    | 通俗说明 (What & Why)            |
| :------ | :------------- | :------------------------------- |
| ✨ 新增 | `path/to/file` | [Explain the new feature added]  |
| 🛠 修改 | `path/to/file` | [Explain the fix or improvement] |
| 🔥 删除 | `path/to/file` | [Explain why it was removed]     |

**3. 下一步计划 (Next Steps)**

- 即将执行: [Next immediate task]
- 需要注意: [Any risks, manual steps, or env configs]
