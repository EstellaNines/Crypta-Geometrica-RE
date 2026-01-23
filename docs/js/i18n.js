const I18N = {
  en: {
    nav_home: "Home",
    nav_visualizers: "Visualizers",
    vis_air_column: "Air Column Sampling",
    vis_cellular: "Cellular Automata",
    vis_sparse: "Sparse Placement",
    vis_random_walk: "Random Walk",
    panel_analysis: "Algorithm Analysis",
    loading: "Loading...",
    error_loading: "Error loading content.",
  },
  zh: {
    nav_home: "首页",
    nav_visualizers: "算法演示",
    vis_air_column: "空气柱采样 (Air Column)",
    vis_cellular: "元胞自动机 (Cellular Automata)",
    vis_sparse: "稀疏分布 (Sparse Placement)",
    vis_random_walk: "随机游走 (Random Walk)",
    panel_analysis: "算法详细分析",
    loading: "加载中...",
    error_loading: "加载内容失败。",
  },
  fi: {
    nav_home: "Koti",
    nav_visualizers: "Visualisoinnit",
    vis_air_column: "Ilmasarakkeen Näytteenotto",
    vis_cellular: "Soluautomaatti",
    vis_sparse: "Haja-asettelu",
    vis_random_walk: "Satunnaiskävely",
    panel_analysis: "Algoritmin Analyysi",
    loading: "Ladataan...",
    error_loading: "Virhe ladattaessa sisältöä.",
  },
  sv: {
    nav_home: "Hem",
    nav_visualizers: "Visualiseringar",
    vis_air_column: "Luftkolonnsprovtagning",
    vis_cellular: "Cellautomater",
    vis_sparse: "Gles Placering",
    vis_random_walk: "Slumpvandring",
    panel_analysis: "Algoritmanalys",
    loading: "Laddar...",
    error_loading: "Fel vid laddning av innehåll.",
  },
  da: {
    nav_home: "Hjem",
    nav_visualizers: "Visualiseringer",
    vis_air_column: "Luftsøjleudtagning",
    vis_cellular: "Cellulære Automater",
    vis_sparse: "Sparsom Placering",
    vis_random_walk: "Tilfældig Gang",
    panel_analysis: "Algoritmeanalyse",
    loading: "Indlæser...",
    error_loading: "Fejl ved indlæsning af indhold.",
  },
};

const VISUALIZERS = {
  air_column: {
    id: "air_column_sampling_visualizer.html",
    titleKey: "vis_air_column",
  },
  cellular: {
    id: "cellular_automata_visualizer.html",
    titleKey: "vis_cellular",
  },
  sparse: {
    id: "sparse_placement_visualizer.html",
    titleKey: "vis_sparse",
  },
  random_walk: {
    id: "random_walk_visualizer.html",
    titleKey: "vis_random_walk",
  },
};

/**
 * Helper to get text for current language
 * @param {string} key
 * @param {string} lang
 */
function t(key, lang) {
  const dict = I18N[lang] || I18N["en"];
  return dict[key] || key;
}
