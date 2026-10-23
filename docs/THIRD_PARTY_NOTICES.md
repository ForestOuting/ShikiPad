# 第三方组件说明

本文件说明 ShikiPad 5.5.1 SendInput 版的系统依赖、研究数据和第三方组件关系，不替代相应许可证正文。

## Windows 与 .NET

- 手柄输入调用 Windows 自带的 HID、SetupAPI 和文件接口。
- 键盘和鼠标输出调用 Windows `user32.dll` 提供的 `SendInput`；这些系统组件不随包分发。
- 本版为依赖框架发布，需要另外安装 x64 `.NET 8 Desktop Runtime`，发布包不捆绑该运行时。

## HidHide

- 项目：[nefarius/HidHide](https://github.com/nefarius/HidHide)
- HidHide 是用户单独安装、用于隐藏原始手柄的驱动工具，不包含在 ShikiPad 发布包中。
- 本版保留现有 HidHide 配置方式；安装、使用或另行分发 HidHide 时应遵守其官方许可。

## Interception 原版

本版不加载、不捆绑 Interception，也不包含其驱动安装/卸载入口。GitHub `main` 分支及原版备份仍使用 Interception；该版本的第三方许可说明随原版保留。

- 原项目：[oblitum/Interception](https://github.com/oblitum/Interception)
- 作者：Francisco Lopes（oblitum）
- 原版所含文件：`interception.dll`、`driver/install-interception.exe`
- 原项目许可说明：[Interception license](https://github.com/oblitum/Interception#license)

更换 ShikiPad 程序不会卸载系统已有的 Interception 驱动；本版不执行系统驱动管理操作。

## 布局研究与测试数据

程序运行不加载下面的软件或语料。布局说明、统计报告及测试词串由这些公开数据派生，进行了筛选、全拼转换、频率归一化、片段计数和动作路径评估。

- **wordfreq 3.1.1 — Copyright 2022 Robyn Speer**：[项目](https://github.com/rspeer/wordfreq)、[原 NOTICE](https://github.com/rspeer/wordfreq/blob/master/NOTICE.md)。代码采用 Apache-2.0；词频数据可按 [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) 再分发。本项目随附的 wordfreq 派生频率/片段数据保留该数据许可及署名。
- wordfreq 上游包括 [Google Books Ngram Viewer](https://books.google.com/ngrams)、[Leeds Internet Corpus](http://corpus.leeds.ac.uk/list.html)、[Wikipedia](https://www.wikipedia.org/)、[ParaCrawl](https://paracrawl.eu/)、[OPUS/OpenSubtitles](https://opus.nlpl.eu/OpenSubtitles.php) 与 [OpenSubtitles](https://www.opensubtitles.org/)。感谢 **Marc Brysbaert 等 SUBTLEX 作者**；[SUBTLEX 是可自由获取的数据](https://crr.ugent.be/programs-data/subtitle-frequencies)。完整来源和条件以 wordfreq 原 NOTICE 为准。
- [pypinyin](https://github.com/mozillazg/python-pinyin) 0.55.0 与 [pypinyin-dict](https://github.com/mozillazg/pypinyin-dict) 0.9.0：程序采用 MIT；中文词组注音使用其 CC-CEDICT 数据，词典内容遵循各自上游许可。
- [ManyThings/Tatoeba 中英句对](https://www.manythings.org/anki/)：本次使用保存日期为 2026-02-13 的资料。源句采用 CC BY 2.0 France，原作者/句子 ID 随原始文件保留。发布包包含汇总动作统计，不包含该外部原句库。

研究来源与哈希见 `docs/LAYOUT_EVALUATION.json`；原始研究文件保存在本地 `C:\ShikiPad-layout-study`。这些研究依赖不构成程序的运行时依赖。
