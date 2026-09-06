# Steam 创意工坊宣传素材

[打开完整简介](Description.zh-CN.bbcode.txt)，复制文件正文到创意工坊简介编辑框即可。简介使用 Steam BBCode，六处图片均为本仓库的公开 Raw 直链。

素材存放于仓库外层的 `Workshop/Media`，不随内层可加载模组打包。封面直接复制自模组 `About/Preview.png`，未重新绘制。

| 文件 | 简介位置 | 处理方式 |
| --- | --- | --- |
| `hive-lord-cover.png` | 顶部封面 | 原封面原样复制 |
| `hive-lord-showcase.png` | 任务背景 | 展示图缩至1000像素宽，保留比例 |
| `spore-spewer-destruction.gif` | 孢子源清理 | 完整10.17秒素材，1.25倍速，480像素宽 |
| `hive-lord-battle.gif` | 霸王虫战斗 | 原素材24–38秒片段，2倍速，376像素宽 |
| `hive-lord-rewards.png` | 猎杀奖励 | 原奖励截图原样复制 |
| `hive-lord-summoning.gif` | 友方召唤 | 原素材16–32秒片段，2倍速，400像素宽 |

GIF 使用8帧/秒、32色调色板和无限循环，全部素材以每份小于1,000,000字节为压缩目标。演示片段经过加速，不能据此判断实际攻击速度。截图文字保留原图，未重绘或补写。源文件保留在本机 `E:\gifrimw`。

不同 Steam 上传入口有各自的限制；这里采用保守的文件大小目标，简介本身通过 GitHub 直链嵌入。公开链接需要仓库保持公开，且文件路径存在。尚未在 Steam 编辑器中发布或验收。

需要重新处理素材时运行 `Build-Media.ps1`，可通过 `SourceDirectory` 和 `FFmpeg` 参数指定本机素材与工具位置。脚本不会改写原始素材。
