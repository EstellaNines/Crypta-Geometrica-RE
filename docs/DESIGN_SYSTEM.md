# 设计系统: Crypta Geometrica Portal

## 核心理念
- **风格**: 金融科技未来主义 (Fintech Futurism) + Flutter Material
- **主题**: 仅暗黑模式 (Dark Mode Only)
- **视觉**: 毛玻璃 (Glassmorphism), 霓虹点缀 (Neon Accents), 数据密集度 (Data Density)

## 1. 配色方案 (Color Palette)

### 背景色 (Backgrounds)
- **基础 (Base)**: `#0F172A` (Slate 900) - 主背景
- **表面 (Surface)**: `#1E293B` (Slate 800) - 卡片背景 (后备)
- **玻璃 (Glass)**: `rgba(255, 255, 255, 0.03)` - 玻璃面板

### 品牌色 (Brand Colors)
- **主色 (Crypto Blue)**: `#38BDF8` (Sky 400) -> 渐变至 `#818CF8`
- **辅色 (Growth Green)**: `#34D399` (Emerald 400)
- **强调色 (Alert Pink)**: `#FB7185` (Rose 400)

### 文本色 (Text)
- **标题 (Headings)**: `#F8FAFC` (Slate 50)
- **正文 (Body)**: `#94A3B8` (Slate 400)
- **柔和 (Muted)**: `#475569` (Slate 600)

## 2. 排版 (Typography)

### 字体系列 (Font Family)
- **非衬线 (Sans)**: `Inter`, system-ui, sans-serif
- **等宽 (Mono)**: `JetBrains Mono`, `Fira Code`, monospace

### 字号 (Scale)
- **H1**: 2.25rem (36px), Bold, Tracking-tight
- **H2**: 1.875rem (30px), Semibold
- **H3**: 1.5rem (24px), Medium
- **Body**: 1rem (16px), Regular, Leading-relaxed

## 3. UI 组件与 CSS 类 (UI Components)

### 玻璃卡片 (Glass Card)
```css
.glass-card {
    background: rgba(255, 255, 255, 0.03);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    border: 1px solid rgba(255, 255, 255, 0.05);
    border-radius: 1rem; /* 16px */
    box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
}
```

### 霓虹按钮 (Neon Button)
```css
.btn-primary {
    background: linear-gradient(135deg, #38BDF8 0%, #818CF8 100%);
    color: #FFFFFF;
    font-weight: 500;
    padding: 0.5rem 1rem;
    border-radius: 0.5rem;
    transition: all 0.2s ease;
    box-shadow: 0 0 10px rgba(56, 189, 248, 0.3);
}
.btn-primary:hover {
    box-shadow: 0 0 20px rgba(56, 189, 248, 0.5);
    transform: translateY(-1px);
}
```

### 导航 (Navigation)
- **顶栏 (Top Bar)**: 吸顶, 玻璃效果, 左侧 Logo, 右侧导航项。
- **侧边栏 (Sidebar)**: 固定左侧 (桌面端) 或 抽屉式 (移动端), 玻璃效果。

## 4. 布局 (Layout)

- **仪表盘网格 (Dashboard Grid)**:
  - 顶栏高度: 64px
  - 侧边栏宽度: 260px
  - 内容区域高度: `calc(100vh - 64px)`
