# Copilot Instructions

## 项目指南
- 用户项目使用 .NET 10 和 CommunityToolkit.Mvvm；偏好将 RecognitionViewModel 拆分为子 ViewModel，并探讨用 Messenger 解耦父子 ViewModel 的状态通知。
- 用户希望把选择文件栏及其相关逻辑迁移到 RecognitionBanner.xaml 和 BannerViewModel；IsBusy 不应迁移为 BannerViewModel 自有字段，而应通过 Messenger 监控父 RecognitionViewModel.IsBusy 的变化。
- 用户偏好在 CommunityToolkit.Mvvm 中使用 [NotifyPropertyChangedRecipients] 通过 PropertyChangedMessage 广播状态属性变化。
- 用户偏好通过强类型 record 消息包装 Messenger payload，以避免接收端检查通用 PropertyChangedMessage 的 Sender 和 PropertyName。