# 可视化门户项目交接汇总 (Handover Summary)

**生成时间**: 2026-01-23
**项目**: Crypta Geometrica RE - Visualization Portal
**位置**: `Assets/docs/`

---

## 1. 项目概况 (Overview)
本项目旨在将散落在 `Assets/Html` 中的独立算法可视化工具，集成到一个统一的、现代化的 Web 门户中。该门户支持 GitHub Pages 托管，并具备多语言支持、响应式布局和分屏分析功能。

## 2. 文件结构 (File Structure)

所有 Web 相关资源均位于 `Assets/docs/` 目录下：

```text
Assets/docs/
├── index.html              # [入口] 主页面，包含导航栏和内容容器
├── css/
│   └── style.css           # [样式] 核心样式表 (Glassmorphism, Dark Mode)
├── js/
│   ├── i18n.js             # [逻辑] 多语言字典 (EN, ZH, FI, SV, DA)
│   └── main.js             # [逻辑] 核心脚本 (路由, Markdown渲染, DOM操作)
├── libs/                   # [库] 第三方依赖 (如 marked.js 本地备份，目前使用CDN)
├── visualizers/            # [资源] 原始可视化 HTML 文件 (Iframe 目标)
│   ├── air_column_sampling_visualizer.html
│   ├── cellular_automata_visualizer.html
│   ├── sparse_placement_visualizer.html
│   └── random_walk_visualizer.html
└── content/                # [内容] 用于动态渲染的文档 (复制自 Assets/MainDocs)
    ├── README_Main_EN.md
    ├── README_Main_ZH.md
    ├── README_Main_FI.md
    ├── README_Main_SV.md
    └── README_Main_DA.md
```

## 3. 核心功能实现 (Key Features)

### 3.1 导航与布局
- **顶部导航栏 (Top Nav)**: 固定顶部，包含 Logo、语言切换器、首页链接和可视化工具下拉菜单。
- **响应式设计**: 使用 CSS Flexbox 实现，适配移动端和桌面端。
- **视觉风格**: 采用“毛玻璃拟态 (Glassmorphism)”设计，深色背景 (`#121212`) 搭配半透明磨砂效果。

### 3.2 多语言支持 (i18n)
- **支持语言**: 英语 (EN), 中文 (ZH), 芬兰语 (FI), 瑞典语 (SV), 丹麦语 (DA)。
- **实现机制**: `js/i18n.js` 维护一个静态字典对象。切换语言时，JS 会自动更新带有 `data-i18n` 属性的 DOM 元素的文本内容，并尝试重新加载对应语言的 Markdown 文档。

### 3.3 动态内容渲染
- **首页**: 使用 `fetch` API 读取 `content/README_Main_{LANG}.md` 文件，并通过 `marked.js` 库实时解析为 HTML 插入页面。
- **可视化页**: 采用 **分屏设计 (Split View)**。
    - **左侧**: 使用 `<iframe>` 加载 `visualizers/` 下的 HTML 工具。
    - **右侧**: 可折叠的“算法分析面板”，用于展示该算法的技术文档（目前为占位符，支持扩展）。

## 4. 维护与扩展 (Maintenance)

### 添加新的可视化工具
1. 将 HTML 文件放入 `docs/visualizers/`。
2. 在 `docs/js/i18n.js` 的 `VISUALIZERS` 对象中注册新工具的 ID 和翻译键。
3. 在 `docs/js/i18n.js` 的 `I18N` 字典中添加对应的翻译文本。
4. 在 `docs/index.html` 的下拉菜单中添加新的 `<a>`标签链接。

### 更新文档
- `docs/content/` 下的 README 文件是静态副本。如果 `Assets/MainDocs` 中的源文件更新，需要手动或通过脚本同步复制到这里。

## 5. 部署指南 (Deployment)

本项目已配置为通过 **GitHub Pages** 运行。

1. **提交更改**: 确保所有 `Assets/docs` 下的文件已推送到远程仓库。
2. **GitHub 设置**:
    - 进入仓库 Settings -> Pages。
    - Source: `Deploy from a branch`.
    - Branch: `main` (或 master)，Folder 选择 `/docs`。
3. **访问地址**: `https://estellanines.github.io/Crypta-Geometrica-RE/` (或自定义域名)。

## 6. 注意事项
- **本地调试**: 由于浏览器 CORS 跨域策略，直接双击 `index.html` 打开可能无法加载 `.md` 文件。请使用 VS Code 的 **Live Server** 插件或在已部署的 GitHub Pages 上查看。
