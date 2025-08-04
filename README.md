# PixivCS

[![NuGet Version](https://img.shields.io/nuget/v/Natsukage.PixivCS.svg)](https://www.nuget.org/packages/Natsukage.PixivCS) [![NuGet Downloads](https://img.shields.io/nuget/dt/Natsukage.PixivCS.svg)](https://www.nuget.org/packages/Natsukage.PixivCS) [![.NET Version](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) [![License](https://img.shields.io/github/license/Natsukage/PixivCS.svg)](https://github.com/Natsukage/PixivCS/blob/master/LICENSE)

PixivCS 是一个现代化的 C# 版本 Pixiv API 库，基于 .NET 8.0 平台开发。

> **致谢：** 本项目是 [pixivpy](https://github.com/upbit/pixivpy) 的 C# 实现，接口设计与数据结构均基于原项目。感谢 upbit 及其贡献者们为开源社区提供的卓越工作。

## 安装

通过 NuGet 安装：

```bash
dotnet add package Natsukage.PixivCS
```

## 快速开始

```csharp
using PixivCS.Api;
using PixivCS.Network;

// 创建 API 实例
var api = new PixivAppApi();

// 认证
var authResult = await api.AuthAsync("your_refresh_token");

// 获取插画详情
var illustDetail = await api.GetIllustDetailAsync("133368512");
Console.WriteLine($"标题: {illustDetail.Illust?.Title}");

// 搜索插画
var searchResult = await api.SearchIllustAsync("初音ミク");
foreach (var illust in searchResult.Illusts ?? [])
{
    Console.WriteLine($"ID: {illust.Id}, 标题: {illust.Title}");
}
```

### 获取 Refresh Token

由于密码登录已不再支持，请使用 `refresh_token` 进行认证。获取 `refresh_token` 的方法：

- [@ZipFile Pixiv OAuth Flow](https://gist.github.com/ZipFile/c9ebedb224406f4f11845ab700124362)
- [gppt: get-pixivpy-token](https://github.com/eggplants/get-pixivpy-token) （推荐，基于 Selenium，易于使用）
- [OAuth with Selenium/ChromeDriver](https://gist.github.com/upbit/6edda27cb1644e94183291109b8a5fde)

## 连接方式

支持三种连接方式：

### 普通连接
```csharp
var config = new ConnectionConfig { Mode = ConnectionMode.Normal };
var api = new PixivAppApi(config);
```

### 免代理直连
```csharp
var config = new ConnectionConfig { Mode = ConnectionMode.DirectBypass };
var api = new PixivAppApi(config);
```

**注意：** 在大陆网络环境下，直连 Pixiv 的 IP 地址速度通常较慢。特别是在进行图片下载时，原图下载可能需要很长时间，请设置足够长的超时时间。如果您有优质代理服务，建议优先使用代理方式连接。

### 代理连接
```csharp
var config = new ConnectionConfig 
{ 
    Mode = ConnectionMode.Proxy,
    ProxyUrl = "http://127.0.0.1:7890"
};
var api = new PixivAppApi(config);
```

## 示例程序

查看 `PixivCS.Tests` 项目获取完整的使用示例，该项目可以直接编译运行。

## 文档

详细文档请参考项目 [Wiki](https://github.com/Natsukage/PixivCS/wiki)。

## 许可证

MIT