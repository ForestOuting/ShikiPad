# 第三方组件说明

本文件说明 ShikiPad 5.2.1 SendInput 版的系统依赖和第三方组件关系，不替代相应许可证正文。

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
