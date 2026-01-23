/**
 * 核心逻辑脚本 (Main Logic Script)
 * 负责导航、视图切换和内容加载
 */

const App = {
    state: {
        currentView: 'welcome',
        currentAlgo: null,
        language: 'zh-CN'
    },

    paths: {
        readme: 'content/README_Main_ZH.md',
        visualizers: {
            'random_walk': 'visualizers/random_walk_visualizer.html',
            'air_column': 'visualizers/air_column_sampling_visualizer.html',
            'cellular': 'visualizers/cellular_automata_visualizer.html',
            'sparse': 'visualizers/sparse_placement_visualizer.html'
        }
    },

    dom: {
        views: {
            welcome: document.getElementById('view-welcome'),
            visualizer: document.getElementById('view-visualizer')
        },
        readmeContainer: document.getElementById('readme-content'),
        iframe: document.getElementById('viz-frame'),
        navItems: document.querySelectorAll('.nav-item'), // Sidebar items
        topNavItems: document.querySelectorAll('.nav-item-top'), // Topbar items
        algoDocContent: document.getElementById('algo-doc-content')
    },

    init() {
        console.log('App Initializing...');
        this.bindEvents();
        this.loadReadme();
        
        // 默认激活
        this.switchView('welcome');
    },

    bindEvents() {
        // 处理所有导航点击 (Sidebar + Topbar)
        const handleNavClick = (item) => {
            const targetView = item.dataset.target;
            const algoId = item.dataset.algo;

            if (targetView === 'welcome') {
                this.switchView('welcome');
                this.updateNavState(null); // Clear algo selection
            } else if (targetView === 'visualizer' && algoId) {
                this.loadVisualizer(algoId);
            }
        };

        this.dom.navItems.forEach(item => item.addEventListener('click', () => handleNavClick(item)));
        this.dom.topNavItems.forEach(item => item.addEventListener('click', () => handleNavClick(item)));
    },

    updateNavState(activeAlgo) {
        // 更新 Sidebar 状态
        this.dom.navItems.forEach(item => {
            if (item.dataset.algo === activeAlgo) {
                item.classList.add('active');
            } else if (activeAlgo === null && item.dataset.target === 'welcome') {
                item.classList.add('active');
            } else {
                item.classList.remove('active');
            }
        });

        // 更新 Topbar 状态
        this.dom.topNavItems.forEach(item => {
            if (activeAlgo) {
                 if (item.dataset.algo === activeAlgo) item.classList.add('active');
                 else item.classList.remove('active');
            } else {
                 if (item.dataset.target === 'welcome') item.classList.add('active');
                 else item.classList.remove('active');
            }
        });
    },

    switchView(viewName) {
        this.state.currentView = viewName;

        Object.values(this.dom.views).forEach(el => {
            el.classList.remove('active');
            el.style.display = 'none';
        });

        const target = this.dom.views[viewName];
        if (target) {
            target.style.display = 'block';
            setTimeout(() => target.classList.add('active'), 10);
        }
    },

    async loadReadme() {
        try {
            const response = await fetch(this.paths.readme);
            if (!response.ok) throw new Error('Failed to load README');
            
            const text = await response.text();
            this.dom.readmeContainer.innerHTML = marked.parse(text);
        } catch (error) {
            console.error(error);
            this.dom.readmeContainer.innerHTML = `<h3>加载文档失败</h3><p>${error.message}</p>`;
        }
    },

    loadVisualizer(algoId) {
        const url = this.paths.visualizers[algoId];
        if (!url) return;

        this.state.currentAlgo = algoId;
        this.switchView('visualizer');
        
        // 更新 Iframe
        this.dom.iframe.src = url;

        // 更新状态高亮
        this.updateNavState(algoId);

        // 更新文档
        this.updateAlgoDoc(algoId);
    },

    updateAlgoDoc(algoId) {
        const content = this.getAlgoContent(algoId);
        this.dom.algoDocContent.innerHTML = content;
    },

    getAlgoContent(id) {
        const data = {
            'random_walk': {
                title: '约束醉汉游走 (Constrained Drunkard Walk)',
                desc: '一种优化的随机游走算法，用于生成适合横版平台游戏（Platformer）的房间拓扑结构。',
                details: `
                    <h4>核心机制</h4>
                    <ul>
                        <li><strong>方向加权</strong>: 并非均匀随机，而是倾向于向下(40%)和向两侧(30%)移动，极少向上(10%)，模拟地牢深度。</li>
                        <li><strong>智能回溯 (Smart Backtracking)</strong>: 当走进死胡同（周围无空位）时，算法会自动随机跳转到已访问的、且周围有空位的节点继续生成，保证房间数量达标。</li>
                    </ul>
                    <div class="code-block" style="background:#0F172A; padding:0.5rem; border-radius:4px; margin-top:0.5rem; font-family:monospace; font-size:0.8em; color:#38BDF8;">
                        Bias: Down=0.4, Side=0.3, Up=0.1
                    </div>
                `
            },
            'air_column': {
                title: '空气柱步进采样 (Air Column Sampling)',
                desc: '模拟重力感知的垂直空间检测算法，用于在空旷区域智能生成跳跃平台。',
                details: `
                    <h4>核心机制</h4>
                    <ul>
                        <li><strong>Bottom-Up 扫描</strong>: 对每一列像素从下往上扫描，统计连续的"空气"高度。</li>
                        <li><strong>触发阈值</strong>: 当空气高度 >= <code>SafeHeight</code> (跳跃高度 * 2 - 安全边距) 时，尝试生成平台。</li>
                        <li><strong>自适应宽度</strong>: 平台生成后会向左右延伸，直到遇到墙壁或达到最大宽度，并进行AABB碰撞检测防止重叠。</li>
                    </ul>
                `
            },
            'cellular': {
                title: '细胞自动机 (Cellular Automata)',
                desc: '利用 B4/S5 规则演化生成自然形态的洞穴地形，也就是著名的 "Game of Life" 变体。',
                details: `
                    <h4>演化规则</h4>
                    <ul>
                        <li><strong>初始噪点</strong>: 随机填充约 45% 的墙壁。</li>
                        <li><strong>出生 (Birth)</strong>: 如果死细胞周围有 >= 5 个墙壁，变活。</li>
                        <li><strong>存活 (Survival)</strong>: 如果活细胞周围有 >= 4 个墙壁，保持存活。</li>
                        <li><strong>平滑处理</strong>: 迭代 5-8 次后，地形变得平滑且具有有机的洞穴感。</li>
                    </ul>
                `
            },
            'sparse': {
                title: '泊松盘稀疏放置 (Poisson Disk Sampling)',
                desc: '在特定区域内高效生成互不重叠的物体分布，常用于敌人、陷阱或宝箱的生成。',
                details: `
                    <h4>算法优势</h4>
                    <ul>
                        <li><strong>Bridson 算法</strong>: 相比纯随机 (Uniform Random) 导致的团聚或重叠，泊松盘采样保证任意两点间距 >= r。</li>
                        <li><strong>自然分布</strong>: 比网格抖动 (Jittered Grid) 更自然，无明显人工痕迹。</li>
                        <li><strong>应用</strong>: 保证玩家不会遇到两个重叠的敌人，或者陷阱不会密集到无法通过。</li>
                    </ul>
                `
            }
        };

        const item = data[id];
        if (!item) return `<p>未找到文档</p>`;

        return `
            <h2>${item.title}</h2>
            <hr style="border-color:var(--border-glass); margin:1rem 0;">
            <p style="font-size:1.1em; color:var(--txt-heading); line-height:1.6;">${item.desc}</p>
            <br>
            <div class="glass-card" style="padding:1.5rem; border:1px dashed var(--border-highlight); background:rgba(15,23,42,0.3);">
                ${item.details}
            </div>
        `;
    }
};

document.addEventListener('DOMContentLoaded', () => {
    App.init();
});
