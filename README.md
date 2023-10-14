# Pillager

<img src=".\Pillager.png"/>

## 介绍

这是一个敏感信息提取工具，将会长期维护，如果有任何问题或建议，欢迎发issues

在整理工具的过程中，发现目前的信息提取工具，普遍存在各种问题，最常见的如体积过大，缺少维护，于是自己在现有工具的基础上进行整理，得到了这款工具

目前支持：

| Browser       | BookMarks | Cookies | Passwords | Historys | Local Storage | Local Extension Settings |
| :------------ | :-------: | :-----: | :-------: | :------: | :------: | :------: |
| IE            |    ✅    |   ❌   |    ✅    |    ✅    |    ❌    |    ❌    |
| Edge          |    ✅    |   ✅   |    ✅    |    ✅    |    ✅    |    ✅    |
| Chrome        |    ✅    |   ✅   |    ✅    |    ✅    |    ✅    |    ✅    |
| Chrome Beta   |    ✅    |   ✅   |    ✅    |    ✅    |    ✅    |    ✅    |
| Chromium      |    ✅    |   ✅   |    ✅    |    ✅    |    ✅    |    ✅    |
| Brave-Browser |    ✅    |   ✅   |    ✅    |    ✅    |    ✅    |    ✅    |
| QQBrowser     |    ✅    |   ✅   |    ✅    |    ✅    |    ✅    |    ✅    |
| SogouExplorer |    ✅    |   ✅   |    ✅    |    ✅    |    ✅    |    ✅    |
| Vivaldi       |    ✅    |   ✅   |    ✅    |    ✅    |    ✅    |    ✅    |
| CocCoc        |    ✅    |   ✅   |    ✅    |    ✅    |    ✅    |    ✅    |
| FireFox       |    ✅    |   ✅   |    ✅    |    ✅    |    ❌    |    ❌    |

| IM       | Support            |
| -------- | ------------------ |
| QQ       | ClientKey（Email） |
| Telegram | tdata              |

后续将会陆续添加支持的软件

## 优点

体积小，长期维护，shellcode兼容.Net Framework 2.x/3.x/4.x , shellcode兼容x86/x64，执行后文件输出至 `%Temp%\Pillager.zip`

## 编译

Release有Github Action自动编译的exe及shellcode，可以直接使用

为了方便使用，Release附带了cs插件版本，使用Pillager命令即可执行
