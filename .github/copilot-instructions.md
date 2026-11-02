# Copilot Instructions

## 项目指南
- 用户项目使用 .NET 10 和 CommunityToolkit.Mvvm；偏好将 RecognitionViewModel 拆分为子 ViewModel，并探讨用 Messenger 解耦父子 ViewModel 的状态通知。
- 用户希望把选择文件栏及其相关逻辑迁移到 RecognitionBanner.xaml 和 BannerViewModel；IsBusy 不应迁移为 BannerViewModel 自有字段，而应通过 Messenger 监控父 RecognitionViewModel.IsBusy 的变化。