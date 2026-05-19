# OneNote Widget AddIn

OneNote 的 Markdown 增强插件，支持 Markdown 编辑、预览、语法高亮和导出功能。

# 功能特性

## Markdown 编辑器
- 基于 [Monaco Editor](https://github.com/microsoft/monaco-editor)（VS Code 编辑器引擎）
- 语法高亮、行号、括号匹配、自动缩进
- 工具栏快捷操作
  - 加粗、斜体、删除线
  - 标题（H1-H3）
  - 链接、代码块、行内代码
  - 有序列表、无序列表、引用
  - 分割线、表格
- 键盘快捷键
  - `Ctrl+S` 保存到 OneNote
  - `Ctrl+B` 加粗 / `Ctrl+I` 斜体 / `Ctrl+K` 链接
  - `Ctrl+1/2/3` 标题级别
- 三种视图模式
  - 查看模式（仅预览）
  - 分栏模式（编辑器 + 实时预览）
  - 编辑模式（仅编辑器）
- 实时预览同步（防抖 500ms）
- 修改内容写回 OneNote 页面
- 冲突检测（编辑期间页面被外部修改时提示确认）

## Markdown 查看器
- 基于 [Markdig](https://github.com/lunet-io/markdig) —— 微软推荐的 Markdown 解析器
- 支持 CommonMark 和 GitHub 风格 Markdown
- 语法高亮
  - 支持 100+ 种编程语言
- 深色/浅色配色方案和代码高亮主题
  - 自动检测系统主题
  - 多种代码高亮主题可选
- [Mermaid](https://github.com/mermaid-js/mermaid) 图表支持
  - 流程图、时序图、甘特图等
- 捕获 OneNote 页面中的图片并在 Markdown 查看器中显示
- 预览窗口刷新（按 `F5`）
- 自动刷新预览窗口
  - 默认每 1 秒自动刷新
  - 可在设置窗口中将间隔设为较大值来禁用自动刷新（例如设为 999999999）

## Markdown 速查表
- CommonMark 语法
- 扩展 Markdown 语法
- 图表
  - Mermaid 图表

## 导出增强
- 支持导出笔记本/分区组/分区/页面
- 支持单文件导出和分层文件导出

## 演示
**语法高亮**
```
    ```csharp
    namespace Demo
    {
        public class MyDemo
        {
            public static void Main(string[] args)
            {
                Console.WriteLine("Hello World!");
            }
        }
    }
    ```
```
*Markdown 预览效果：*
```csharp
    namespace Demo
    {
        public class MyDemo
        {
            public static void Main(string[] args)
            {
                Console.WriteLine("Hello World!");
            }
        }
    }
```
**Mermaid 饼图**
```
    ```mermaid
    pie
      title Key elements in Product X
      "Calcium" : 40
      "Potassium" : 46
      "Magnesium" : 9
      "Iron" :  5

    ```
```
*Markdown 预览效果：*
<svg id="mermaid-1642657277801" width="100%" xmlns="http://www.w3.org/2000/svg" height="450" style="max-width: 1200px;" viewBox="0 0 1200 450"><style>#mermaid-1642657277801 {font-family:"trebuchet ms",verdana,arial,sans-serif;font-size:16px;fill:#ccc;}#mermaid-1642657277801 .error-icon{fill:#a44141;}#mermaid-1642657277801 .error-text{fill:#ddd;stroke:#ddd;}#mermaid-1642657277801 .edge-thickness-normal{stroke-width:2px;}#mermaid-1642657277801 .edge-thickness-thick{stroke-width:3.5px;}#mermaid-1642657277801 .edge-pattern-solid{stroke-dasharray:0;}#mermaid-1642657277801 .edge-pattern-dashed{stroke-dasharray:3;}#mermaid-1642657277801 .edge-pattern-dotted{stroke-dasharray:2;}#mermaid-1642657277801 .marker{fill:lightgrey;stroke:lightgrey;}#mermaid-1642657277801 .marker.cross{stroke:lightgrey;}#mermaid-1642657277801 svg{font-family:"trebuchet ms",verdana,arial,sans-serif;font-size:16px;}#mermaid-1642657277801 .pieCircle{stroke:black;stroke-width:2px;opacity:0.7;}#mermaid-1642657277801 .pieTitleText{text-anchor:middle;font-size:25px;fill:hsl(28.5714285714, 17.3553719008%, 86.2745098039%);font-family:"trebuchet ms",verdana,arial,sans-serif;}#mermaid-1642657277801 .slice{font-family:"trebuchet ms",verdana,arial,sans-serif;fill:#ccc;font-size:17px;}#mermaid-1642657277801 .legend text{fill:hsl(28.5714285714, 17.3553719008%, 86.2745098039%);font-family:"trebuchet ms",verdana,arial,sans-serif;font-size:17px;}#mermaid-1642657277801 :root{--mermaid-font-family:"trebuchet ms",verdana,arial,sans-serif;}}</style><g></g><g transform="translate(600,225)"><path d="M46.00762912549813,179.18788480879675A185,185,0,0,1,-142.54494991352107,-117.92343810350752L0,0Z" fill="#0b0000" class="pieCircle"></path><path d="M1.1327982892113017e-14,-185A185,185,0,0,1,46.00762912549813,179.18788480879675L0,0Z" fill="#4d1037" class="pieCircle"></path><path d="M-142.54494991352107,-117.92343810350752A185,185,0,0,1,-57.1681439593653,-175.9454555146034L0,0Z" fill="#3f5258" class="pieCircle"></path><path d="M-57.1681439593653,-175.9454555146034A185,185,0,0,1,-3.398394867633905e-14,-185L0,0Z" fill="#4f2f1b" class="pieCircle"></path><text transform="translate(-78.10033310893637,49.56397853555722)" class="slice" style="text-anchor: middle;">40%</text><text transform="translate(91.7706098715892,-11.593324104698139)" class="slice" style="text-anchor: middle;">46%</text><text transform="translate(-51.99271245132211,-76.50495312039695)" class="slice" style="text-anchor: middle;">9%</text><text transform="translate(-14.470188016221371,-91.36117150505024)" class="slice" style="text-anchor: middle;">5%</text><text x="0" y="-200" class="pieTitleText">Key&nbsp;elements&nbsp;in&nbsp;Product&nbsp;X</text><g class="legend" transform="translate(216,-44)"><rect width="18" height="18" style="fill: rgb(11, 0, 0); stroke: rgb(11, 0, 0);"></rect><text x="22" y="14">Calcium</text></g><g class="legend" transform="translate(216,-22)"><rect width="18" height="18" style="fill: rgb(77, 16, 55); stroke: rgb(77, 16, 55);"></rect><text x="22" y="14">Potassium</text></g><g class="legend" transform="translate(216,0)"><rect width="18" height="18" style="fill: rgb(63, 82, 88); stroke: rgb(63, 82, 88);"></rect><text x="22" y="14">Magnesium</text></g><g class="legend" transform="translate(216,22)"><rect width="18" height="18" style="fill: rgb(79, 47, 27); stroke: rgb(79, 47, 27);"></rect><text x="22" y="14">Iron</text></g></g></svg>

[更多 Mermaid 图表示例](https://mermaid-js.github.io/mermaid/#/examples)

### 预览窗口截图
![](preview_snapshot.png)

### 速查表截图
![](cheatsheet_snapshot.png)

## 许可证
Apache 2.0
