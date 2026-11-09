# Xice的体验优化

适用于 **Probably Stolen** 的体验优化模组，改善日常经营中的重复操作与物品信息展示。各项功能可在游戏内独立配置。

[使用与配置](#使用与配置) · [从源码构建](#从源码构建) · [问题反馈](https://github.com/xice-1201/XicesBetterExperience/issues)

## 兼容性

| 项目 | 说明 |
| --- | --- |
| 当前模组版本 | 0.3.8 |
| 支持的游戏内部版本 | `0.46D` |
| 加载方式 | 游戏内置 Mod 系统 |
| 界面语言 | 简体中文、英文 |
| Mod ID | `xice.betterexperience` |

模组会检查游戏内部版本字符串。游戏更新后，请确认兼容性再安装对应版本。

## 安装

将构建好的 `XicesBetterExperience` 文件夹放入游戏安装目录下的 `Mods` 文件夹，目录结构应为：

```text
Probably Stolen/
└── Mods/
    └── XicesBetterExperience/
        ├── manifest.xml
        ├── XicesBetterExperience.dll
        └── Localization/
            ├── zh.csv
            └── en.csv
```

启动游戏，在 Mod 菜单中启用 **Xice的体验优化**，随后重启游戏完成加载。

更新时请先退出游戏，再用新版本覆盖同名文件夹。禁用模组可通过游戏内 Mod 菜单完成；卸载时，退出游戏后移除该文件夹。

本仓库提供源码。GitHub 的源码压缩包不包含可直接加载的 DLL，请按下文构建后安装。

## 使用与配置

在游戏的 Mod 菜单中打开本模组的配置界面。设置按用途分为两个标签页：

| 标签页 | 用途 |
| --- | --- |
| 通常 | 调整鉴定、文档阅读和日常经营中的辅助操作 |
| 图标 | 调整物品名称、状态标记及告警信息的展示 |

各配置项的具体说明可通过旁边的信息提示查看。修改后点击 **应用**；点击 **重置** 可恢复界面中的默认值，再应用保存。设置由游戏原生配置系统保存，跨存档共用。

勾选项默认开启。鉴定自动化默认处于 **专家** 档，可通过滑动条调整所需证书等级或关闭。

### 使用说明

- 鉴定自动化需要满足对应证书和原版服务条件，未满足时保留手动鉴定流程。
- 市场箭头反映物品所有类型的当前市场变动之和；上涨显示红色上箭头，下跌显示绿色下箭头，相互抵消时隐藏。
- 文档翻页记忆保留在当前游戏进程中，退出游戏后重置。

## 从源码构建

### 环境要求

- Windows 与 PowerShell。
- 已安装的 Probably Stolen，包含 `Probably Stolen_Data/Managed` 下的游戏程序集。
- Unity Editor `2022.3.61f1c1`，构建脚本使用其中的 .NET 运行时和 Roslyn 编译器。

在仓库根目录执行，并将路径替换为本机安装位置：

```powershell
.\build.ps1 `
    -GamePath 'D:\SteamLibrary\steamapps\common\Probably Stolen' `
    -UnityEditorPath 'D:\Unity\2022.3.61f1c1\Editor'
```

构建产物位于 `dist/XicesBetterExperience/`，需手动复制到游戏的 `Mods` 目录。游戏程序集仅作为编译引用，不随模组分发。

### 验证

运行回归测试：

```powershell
.\tests\Run-Regression.ps1 `
    -UnityEditorPath 'D:\Unity\2022.3.61f1c1\Editor'
```

回归测试使用游戏接口替身检查配置与功能逻辑；构建使用实际游戏程序集校验 API。涉及界面布局、Harmony 加载和存档交互的改动，还需要在游戏内验证。

### 目录结构

```text
src/            模组实现与界面补丁
Localization/   配置界面的本地化文本
tests/          回归测试与运行脚本
manifest.xml    模组清单与兼容性声明
build.ps1       构建脚本
```

## 问题反馈

请通过 [GitHub Issues](https://github.com/xice-1201/XicesBetterExperience/issues) 提交问题，附上游戏版本、模组版本、相关配置、复现步骤，以及预期与实际表现。界面问题请附截图；加载失败请附完整拒绝信息，运行异常请附相关游戏日志。
