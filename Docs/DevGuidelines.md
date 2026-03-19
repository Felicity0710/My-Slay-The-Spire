# 项目约定

## 数据与文本编码规则

1. 所有文本配置文件，包括 JSON、CSV、TSV、TXT、YAML，都必须使用 UTF-8 读写。
2. 脚本写文件时，统一使用 UTF-8 without BOM。
3. 读写文本文件时，不要依赖 shell 默认编码。
4. 不要使用会强制把这些文件保存成 ANSI、GBK、GB2312 等旧编码的编辑器。

## 脚本约定

- 涉及大量数据修改前，优先使用 `Tools/Json` 下面的辅助脚本。
- `Update-CardArt.ps1`：
  - 为 `Data/cards.json` 增加或规范化 `artPath` 字段
  - 输出时强制使用 UTF-8
- `Repair-DescriptionZh.ps1`：
  - 尝试修复 `descriptionZh` 的乱码问题
  - 重写前会先备份原文件
- `JsonEncodingGuard.ps1`：
  - 检查目标文件的编码是否正常，以及 JSON 是否能正确解析

## 推荐工作流

1. 只使用 `Tools/Json` 下的脚本去编辑或批量生成数据
2. 执行：
   - `pwsh Tools/Json/JsonEncodingGuard.ps1 -TargetJson Data/cards.json`
3. 如果检查通过，再正常运行项目
4. 如果解析失败，再执行：
   - `pwsh Tools/Json/Repair-DescriptionZh.ps1 -InputPath Data/cards.json`
   - 这个脚本会保留备份

## 备注

- 如果某个 `descriptionZh` 已经被错误重编码，单靠算法可能无法完全恢复。
- 如果某个值看起来已经无法修复，优先从 git 历史或已知正确的备份里恢复对应内容，再重新应用脚本。
