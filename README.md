# Pillager

[![License](https://img.shields.io/github/license/qwqdanchun/Pillager.svg)](LICENSE)
![GitHub last commit](https://img.shields.io/github/last-commit/qwqdanchun/Pillager)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/qwqdanchun/Pillager)
[![Downloads](https://img.shields.io/github/downloads/qwqdanchun/Pillager/total.svg)](https://github.com/qwqdanchun/Pillager/releases)
![Issues](https://img.shields.io/github/issues/qwqdanchun/Pillager)

<img src=".\Pillager.png"/>

[中文说明](./README_ZH.md)

## Introduction

Pillager is a tool for exporting and decrypting useful data from target computer.

## Support

#### Browser

| Browser Name  | BookMarks | Cookies | Passwords | Historys | Local Storage | Extension Settings |
| :------------ | :-------: | :-----: | :-------: | :------: | :-----------: | :----------------: |
| IE            |    ✅    |   ❌   |    ✅    |    ✅    |      ❌      |         ❌         |
| Edge          |    ✅    |   ✅   |    ✅    |    ✅    |      ✅      |         ✅         |
| Chrome        |    ✅    |   ✅   |    ✅    |    ✅    |      ✅      |         ✅         |
| Chrome Beta   |    ✅    |   ✅   |    ✅    |    ✅    |      ✅      |         ✅         |
| Chrome SxS    |    ✅    |   ✅   |    ✅    |    ✅    |      ✅      |         ✅         |
| Chromium      |    ✅    |   ✅   |    ✅    |    ✅    |      ✅      |         ✅         |
| Brave-Browser |    ✅    |   ✅   |    ✅    |    ✅    |      🚧      |         🚧         |
| QQBrowser     |    ✅    |   ✅   |    ✅    |    ✅    |      ✅      |         ✅         |
| SogouExplorer |    ✅    |   ✅   |    ✅    |    ✅    |      🚧      |         🚧         |
| 360Chrome     |    ❌    |   ✅   |    ✅    |    ❌    |      ✅      |         ✅         |
| 360ChromeX    |    ❌    |   ✅   |    ✅    |    ❌    |      ✅      |         ✅         |
| Vivaldi       |    🚧    |   🚧   |    🚧    |    🚧    |      🚧      |         🚧         |
| CocCoc        |    🚧    |   🚧   |    🚧    |    🚧    |      🚧      |         🚧         |
| Torch         |    🚧    |   🚧   |    🚧    |    🚧    |      🚧      |         🚧         |
| Kometa        |    🚧    |   🚧   |    🚧    |    🚧    |      🚧      |         🚧         |
| Orbitum       |    🚧    |   🚧   |    🚧    |    🚧    |      🚧      |         🚧         |
| CentBrowser   |    🚧    |   🚧   |    🚧    |    🚧    |      🚧      |         🚧         |
| 7Star         |    🚧    |   🚧   |    🚧    |    🚧    |      🚧      |         🚧         |
| Sputnik       |    🚧    |   🚧   |    🚧    |    🚧    |      🚧      |         🚧         |
| Epic Privacy  |    🚧    |   🚧   |    🚧    |    🚧    |      🚧      |         🚧         |
| Uran          |    🚧    |   🚧   |    🚧    |    🚧    |      🚧      |         🚧         |
| Yandex        |    🚧    |   🚧   |    🚧    |    🚧    |      🚧      |         🚧         |
| Opera         |    🚧    |   🚧   |    🚧    |    🚧    |      🚧      |         🚧         |
| Opera GX      |    🚧    |   🚧   |    🚧    |    🚧    |      🚧      |         🚧         |
| FireFox       |    ✅    |   ✅   |    ✅    |    ✅    |      ❌      |         ✅         |

✅ Support,🚧 Haven't Tested,❌ Not Support

#### Software

* Acount Takeover
  * Telegram
  * Skype
  * Enigma
  * DingTalk
  * Line
  * Discord
  * MailMaster
  * Foxmail
  * FileZilla
  * Teams
* Password Recovery
  * MobaXterm
  * Xmanager
  * RDCMan
  * FinalShell
  * Navicat
  * SQLyog
  * SecureCRT
  * Outlook
  * MailBird
  * WinSCP
  * DBeaver
  * CoreFTP
  * Snowflake
  * HeidiSQL
* Personal Infomation
  * QQ
  * VSCode
  * Netease CloudMusic
  * Win10Ms_Pinyin

Will add more ......

#### System

* Wifi
* ScreenShot
* InstalledApp
* ClipBoard
* FileList
* RecentFile
* SystemInfo
* TaskList

## Usage

This project uses Github Action to auto build and upload the [Release](https://github.com/qwqdanchun/Pillager/releases)

* [Pillager.exe](https://github.com/qwqdanchun/Pillager/releases/download/AutoBuild/Pillager.exe) is exe for .Net Framework v3.5
* [Pillager.bin](https://github.com/qwqdanchun/Pillager/releases/download/AutoBuild/Pillager.bin) is shellcode built with Donut
* [cs-plugin.zip](https://github.com/qwqdanchun/Pillager/releases/download/AutoBuild/cs-plugin.zip) is plugin for CobaltStrike

Pillager.exe is just for testing. It will be detect as malware by most Anti-Virus softwares.

Run the shellcode in your way, and find the result at `%Temp%\Pillager.zip`.

## Feature

* Shellcode file size is less than 100kb
* Using self version of Donut，shellcode is suitable for both .Net Framework v3.5/v4.x

## Contributors

<a href="https://github.com/qwqdanchun/Pillager/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=qwqdanchun/Pillager" />
</a>

## 404 StarLink Project

![](https://github.com/knownsec/404StarLink-Project/raw/master/logo.png)

Pillager has joined [404星链计划](https://github.com/knownsec/404StarLink)
